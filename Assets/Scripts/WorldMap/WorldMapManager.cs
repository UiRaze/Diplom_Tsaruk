using System.Collections.Generic;
using System;
using UnityEngine;

public class WorldMapManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WorldMapGenerator mapGenerator;
    [SerializeField] private WorldMapUI worldMapUI;
    [SerializeField] private EventPopup eventPopup;
    [SerializeField] private ResourceRewardPopup resourceRewardPopup;
    [SerializeField] private ShopUI shopUI;

    [Header("State")]
    [SerializeField] private MapNode currentPlayerPosition;
    [SerializeField] private MapNode selectedNode;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private void Start()
    {
        ResolveReferences();

        if (GameManager.Instance == null)
        {
            LogError("[WorldMapManager] GameManager.Instance = NULL!");
            return;
        }

        string lastNodeId = GameManager.Instance.GetLastNodeId();
        if (!string.IsNullOrEmpty(lastNodeId))
        {
            RestorePlayerPosition(lastNodeId);
        }
        else
        {
            if (mapGenerator != null)
            {
                mapGenerator.GenerateMap();
                SetStartingPosition();
            }
            else
            {
                LogError("[WorldMapManager] mapGenerator = NULL!");
            }
        }

        RefreshMapVisibility();
        worldMapUI?.UpdateStats();
        NodeEventDispatcher.ProcessPendingRewards();
    }

    public void OnNodeSelected(MapNode node)
    {
        GameEventBus.RaiseNodeSelected(node);
        if (!SelectNode(node))
        {
            return;
        }

        EnterNode();
    }

    public bool SelectNode(MapNode targetNode)
    {
        if (!CanMoveToNode(targetNode, true))
        {
            return false;
        }

        if (selectedNode != null)
        {
            selectedNode.SetAsSelected(false);
        }

        selectedNode = targetNode;
        selectedNode.SetAsSelected(true);
        worldMapUI?.OnNodeSelected(selectedNode);
        return true;
    }

    public void EnterNode()
    {
        if (selectedNode == null)
        {
            LogWarning("[WorldMapManager] Узел не выбран!");
            return;
        }

        if (!MoveToNode(selectedNode))
        {
            return;
        }

        worldMapUI?.ClearSelection();
        selectedNode = null;

        if (currentPlayerPosition == null || currentPlayerPosition.NodeData == null)
        {
            return;
        }

        NodeEventDispatcher.ExecuteEvent(currentPlayerPosition);
        worldMapUI?.UpdateStats();
    }

    public bool MoveToNode(MapNode targetNode)
    {
        if (!CanMoveToNode(targetNode, true))
        {
            return false;
        }

        currentPlayerPosition.SetAsCurrentPosition(false);
        currentPlayerPosition.MarkVisited();

        currentPlayerPosition = targetNode;
        currentPlayerPosition.SetAsCurrentPosition(true);
        currentPlayerPosition.MarkVisited();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetLastNode(currentPlayerPosition.NodeId);
            GameManager.Instance.SetCurrentLevelFromNodeLevel(currentPlayerPosition.NodeLevel);
        }

        RefreshMapVisibility();
        return true;
    }

    public bool IsConnectedToCurrentNode(MapNode targetNode)
    {
        if (mapGenerator == null || currentPlayerPosition == null || targetNode == null)
        {
            return false;
        }

        return mapGenerator.IsConnected(currentPlayerPosition, targetNode);
    }

    private bool CanMoveToNode(MapNode targetNode, bool logReason)
    {
        if (targetNode == null || currentPlayerPosition == null)
        {
            if (logReason)
            {
                LogWarning("[WorldMapManager] targetNode или currentPlayerPosition = NULL.");
            }

            return false;
        }

        if (!IsConnectedToCurrentNode(targetNode))
        {
            if (logReason)
            {
                LogWarning("[WorldMapManager] Узел недоступен: нет связи с текущим.");
            }

            return false;
        }

        if (targetNode.NodeLevel != currentPlayerPosition.NodeLevel + 1)
        {
            if (logReason)
            {
                LogWarning("[WorldMapManager] Переход возможен только на следующий уровень.");
            }

            return false;
        }

        return true;
    }

    public string GetCurrentNodeId()
    {
        return currentPlayerPosition != null ? currentPlayerPosition.NodeId : null;
    }

    public void OnBattleFinished()
    {
        if (currentPlayerPosition != null && GameManager.Instance != null)
        {
            GameManager.Instance.SetLastNode(currentPlayerPosition.NodeId);
        }
    }

    public void RefreshMapVisibility()
    {
        if (mapGenerator == null || currentPlayerPosition == null)
        {
            return;
        }

        int extraReveal = GameManager.Instance != null ? GameManager.Instance.SeerRevealDepth : 0;
        int revealUntilLevel = currentPlayerPosition.NodeLevel + 1 + Mathf.Max(0, extraReveal);

        List<MapNode> nodes = mapGenerator.GetAllGeneratedNodes();
        for (int i = 0; i < nodes.Count; i++)
        {
            MapNode node = nodes[i];
            if (node == null)
            {
                continue;
            }

            bool shouldReveal = node.IsVisited
                                || node.IsCurrentPosition
                                || node.NodeLevel <= revealUntilLevel;
            node.SetRevealed(shouldReveal);
        }
    }

    private void RestorePlayerPosition(string nodeId)
    {
        if (mapGenerator == null)
        {
            return;
        }

        mapGenerator.GenerateMap();

        List<MapNode> allNodes = mapGenerator.GetAllGeneratedNodes();
        MapNode restoredNode = allNodes.Find(node => node != null && node.NodeId == nodeId);

        if (restoredNode == null)
        {
            restoredNode = FindNodeByStableKey(allNodes, nodeId);
        }

        if (restoredNode != null)
        {
            currentPlayerPosition = restoredNode;
            currentPlayerPosition.SetAsCurrentPosition(true);
            currentPlayerPosition.MarkVisited();
            GameManager.Instance?.SetLastNode(currentPlayerPosition.NodeId);
            GameManager.Instance?.SetCurrentLevelFromNodeLevel(currentPlayerPosition.NodeLevel);
        }
        else
        {
            SetStartingPosition();
        }
    }

    private void SetStartingPosition()
    {
        if (mapGenerator == null)
        {
            return;
        }

        List<MapNode> firstLevelNodes = mapGenerator.GetFirstLevelNodes();
        if (firstLevelNodes.Count == 0)
        {
            return;
        }

        currentPlayerPosition = firstLevelNodes[0];
        currentPlayerPosition.SetAsCurrentPosition(true);
        currentPlayerPosition.MarkVisited();
        GameManager.Instance?.SetLastNode(currentPlayerPosition.NodeId);
        GameManager.Instance?.SetCurrentLevelFromNodeLevel(currentPlayerPosition.NodeLevel);
    }

    private MapNode FindNodeByStableKey(List<MapNode> nodes, string savedNodeId)
    {
        string stableKey = ExtractStableKey(savedNodeId);
        if (string.IsNullOrEmpty(stableKey))
        {
            return null;
        }

        return nodes.Find(node => node != null
                                  && !string.IsNullOrEmpty(node.NodeId)
                                  && node.NodeId.StartsWith(stableKey + "_", StringComparison.Ordinal));
    }

    private string ExtractStableKey(string nodeId)
    {
        if (string.IsNullOrEmpty(nodeId))
        {
            return null;
        }

        string[] parts = nodeId.Split('_');
        if (parts.Length < 4)
        {
            return null;
        }

        return $"{parts[0]}_{parts[1]}_{parts[2]}";
    }

    private void ResolveReferences()
    {
        if (mapGenerator == null)
        {
            mapGenerator = FindObjectOfType<WorldMapGenerator>();
        }

        if (worldMapUI == null)
        {
            worldMapUI = FindObjectOfType<WorldMapUI>();
        }

        if (eventPopup == null)
        {
            eventPopup = FindObjectOfType<EventPopup>();
        }

        if (resourceRewardPopup == null)
        {
            resourceRewardPopup = FindObjectOfType<ResourceRewardPopup>();
        }

        if (shopUI == null)
        {
            shopUI = FindObjectOfType<ShopUI>();
        }
    }

    private void Log(string message)
    {
        if (debugLogs) Debug.Log(message);
    }

    private void LogWarning(string message)
    {
        if (debugLogs) Debug.LogWarning(message);
    }

    private void LogError(string message)
    {
        if (debugLogs) Debug.LogError(message);
    }
}
