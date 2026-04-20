using System.Collections.Generic;
using UnityEngine;

public class WorldMapGenerator : MonoBehaviour
{
    [Header("Node Settings")]
    [SerializeField] private GameObject nodePrefab;
    [SerializeField] private Transform mapContainer;
    [SerializeField] private float horizontalSpacing = 300f;
    [SerializeField] private float verticalSpacing = 200f;
    [SerializeField] private int mapLevels = GameConfig.MAP_LEVELS;
    [SerializeField] private int maxBranchesPerLevel = GameConfig.MAX_BRANCHES;

    [Header("Node Data")]
    [SerializeField] private MapNodeData battleNodeData;
    [SerializeField] private MapNodeData eventNodeData;
    [SerializeField] private MapNodeData merchantNodeData;
    [SerializeField] private MapNodeData restNodeData;
    [SerializeField] private MapNodeData eliteNodeData;
    [SerializeField] private MapNodeData bossNodeData;

    [Header("Line Settings")]
    [SerializeField] private GameObject linePrefab;
    [SerializeField] private Transform linesContainer;

    [Header("References")]
    [SerializeField] private WorldMapManager worldMapManager;

    private readonly List<List<MapNode>> nodes = new List<List<MapNode>>();
    private readonly List<(MapNode from, MapNode to)> connections = new List<(MapNode, MapNode)>();

    public void GenerateMap()
    {
        LoadDefaultNodeDataIfMissing();
        ClearMap();

        Random.State previousState = Random.state;
        int seed = GameManager.Instance != null ? GameManager.Instance.GetOrCreateMapSeed() : System.Environment.TickCount;
        Random.InitState(seed);

        try
        {
            int levels = ResolveMapLevels();
            int maxBranches = ResolveMaxBranchesPerLevel();

            for (int level = 0; level < levels; level++)
            {
                List<MapNode> levelNodes = new List<MapNode>();
                int branches = level == 0 ? 1 : Random.Range(2, maxBranches + 1);

                if (level == levels - 1)
                {
                    branches = 1;
                }

                for (int branch = 0; branch < branches; branch++)
                {
                    GameObject nodeGO = Instantiate(nodePrefab, mapContainer);
                    MapNode node = nodeGO.GetComponent<MapNode>();
                    if (node == null)
                    {
                        Destroy(nodeGO);
                        continue;
                    }

                    float x = (level - (levels - 1) / 2f) * horizontalSpacing;
                    float y = (branch - (branches - 1) / 2f) * verticalSpacing;
                    nodeGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(x, y);

                    NodeType type = PickNodeType(level, levels);
                    MapNodeData nodeData = GetNodeDataForType(type);
                    string uniqueId = $"Node_{level}_{branch}_{type}";

                    node.SetNodeData(nodeData, uniqueId);
                    node.SetNodeLevel(level);

                    if (worldMapManager != null)
                    {
                        node.onNodeSelected.RemoveAllListeners();
                        node.onNodeSelected.AddListener(() => worldMapManager.OnNodeSelected(node));
                    }

                    levelNodes.Add(node);
                }

                nodes.Add(levelNodes);
            }

            CreateConnections();
        }
        finally
        {
            Random.state = previousState;
        }
    }

    private NodeType PickNodeType(int level, int totalLevels)
    {
        if (level == totalLevels - 1)
        {
            return NodeType.BossBattle;
        }

        if (level == 0)
        {
            NodeType[] firstLevelPool = { NodeType.Battle, NodeType.Rest };
            return firstLevelPool[Random.Range(0, firstLevelPool.Length)];
        }

        NodeType[] middlePool =
        {
            NodeType.Battle,
            NodeType.Event,
            NodeType.Merchant,
            NodeType.Rest,
            NodeType.EliteBattle
        };

        return middlePool[Random.Range(0, middlePool.Length)];
    }

    private MapNodeData GetNodeDataForType(NodeType type)
    {
        switch (type)
        {
            case NodeType.Event:
                return eventNodeData != null ? eventNodeData : battleNodeData;
            case NodeType.Merchant:
                return merchantNodeData != null ? merchantNodeData : battleNodeData;
            case NodeType.Rest:
                return restNodeData != null ? restNodeData : battleNodeData;
            case NodeType.EliteBattle:
                return eliteNodeData != null ? eliteNodeData : battleNodeData;
            case NodeType.BossBattle:
                return bossNodeData != null ? bossNodeData : battleNodeData;
            case NodeType.Battle:
            default:
                return battleNodeData;
        }
    }

    private void CreateConnections()
    {
        connections.Clear();

        for (int level = 0; level < nodes.Count - 1; level++)
        {
            List<MapNode> currentLevel = nodes[level];
            List<MapNode> nextLevel = nodes[level + 1];

            for (int i = 0; i < currentLevel.Count; i++)
            {
                MapNode currentNode = currentLevel[i];
                int linksCount = Random.Range(1, Mathf.Min(3, nextLevel.Count + 1));

                HashSet<MapNode> usedTargets = new HashSet<MapNode>();
                for (int link = 0; link < linksCount; link++)
                {
                    if (nextLevel.Count == 0)
                    {
                        break;
                    }

                    MapNode target = nextLevel[Random.Range(0, nextLevel.Count)];
                    if (usedTargets.Contains(target))
                    {
                        continue;
                    }

                    usedTargets.Add(target);
                    connections.Add((currentNode, target));

                    if (linePrefab != null && linesContainer != null)
                    {
                        GameObject lineGO = Instantiate(linePrefab, linesContainer);
                        MapLineRenderer lineRenderer = lineGO.GetComponent<MapLineRenderer>();
                        if (lineRenderer != null)
                        {
                            lineRenderer.DrawLineBetween(currentNode, target);
                        }
                    }
                }
            }
        }
    }

    private void LoadDefaultNodeDataIfMissing()
    {
        if (battleNodeData == null) battleNodeData = Resources.Load<MapNodeData>("ScriptableObjects/Cards/BattleNode");
        if (eventNodeData == null) eventNodeData = Resources.Load<MapNodeData>("ScriptableObjects/Cards/EventNode");
        if (merchantNodeData == null) merchantNodeData = Resources.Load<MapNodeData>("ScriptableObjects/Cards/MerchantNode");
        if (restNodeData == null) restNodeData = Resources.Load<MapNodeData>("ScriptableObjects/Cards/RestNode");
        if (eliteNodeData == null) eliteNodeData = Resources.Load<MapNodeData>("ScriptableObjects/Cards/EliteNode");
        if (bossNodeData == null) bossNodeData = Resources.Load<MapNodeData>("ScriptableObjects/Cards/BossNode");
    }

    private void ClearMap()
    {
        if (mapContainer != null)
        {
            foreach (Transform child in mapContainer)
            {
                Destroy(child.gameObject);
            }
        }

        if (linesContainer != null)
        {
            foreach (Transform child in linesContainer)
            {
                Destroy(child.gameObject);
            }
        }

        nodes.Clear();
        connections.Clear();
    }

    private int ResolveMapLevels()
    {
        if (GameManager.Instance != null)
        {
            return Mathf.Max(2, GameManager.Instance.MapLevels);
        }

        return Mathf.Max(2, mapLevels);
    }

    private int ResolveMaxBranchesPerLevel()
    {
        if (GameManager.Instance != null)
        {
            return Mathf.Max(2, GameManager.Instance.MaxBranchesPerLevel);
        }

        return Mathf.Max(2, maxBranchesPerLevel);
    }

    public List<MapNode> GetFirstLevelNodes()
    {
        if (nodes.Count > 0)
        {
            return nodes[0];
        }

        return new List<MapNode>();
    }

    public bool IsConnected(MapNode from, MapNode to)
    {
        if (from == null || to == null)
        {
            return false;
        }

        if (to.NodeLevel != from.NodeLevel + 1)
        {
            return false;
        }

        return connections.Exists(connection => connection.from == from && connection.to == to);
    }
}
