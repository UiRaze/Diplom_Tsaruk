using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class MapNode : MonoBehaviour
{
    [SerializeField] private MapNodeData nodeData;
    [SerializeField] private Image nodeImage;
    [SerializeField] private Button nodeButton;
    [SerializeField] public UnityEvent onNodeSelected;

    [SerializeField] private bool isCurrentPosition;
    [SerializeField] private bool isSelected;
    [SerializeField] private bool isVisited = false;
    [SerializeField] private bool isRevealed = true;
    [SerializeField] private string nodeId;
    [SerializeField] private int nodeLevel;
    [SerializeField] private bool hasRuntimeNodeType;
    [SerializeField] private NodeType runtimeNodeType = NodeType.NormalBattle;
    [SerializeField] private bool colorByTypeIfNoIcon = true;

    [Header("Colors")]
    [SerializeField] private Color defaultColor = Color.white;
    [SerializeField] private Color currentColor = Color.yellow;
    [SerializeField] private Color selectedColor = Color.green;
    [SerializeField] private Color visitedColor = Color.gray;

    public MapNodeData NodeData => nodeData;
    public string NodeId => nodeId;
    public int NodeLevel => nodeLevel;
    public bool IsCurrentPosition => isCurrentPosition;
    public bool IsVisited => isVisited;
    public bool IsRevealed => isRevealed;
    public NodeType EffectiveNodeType => hasRuntimeNodeType
        ? runtimeNodeType
        : (nodeData != null ? nodeData.NodeType : NodeType.NormalBattle);

    private void Start()
    {
        if (nodeData != null && nodeImage != null)
        {
            nodeImage.sprite = nodeData.NodeIcon;
        }

        if (nodeButton != null)
        {
            nodeButton.onClick.RemoveListener(OnNodeClicked);
            nodeButton.onClick.AddListener(OnNodeClicked);
        }

        UpdateVisualState();
    }

    public void OnNodeClicked()
    {
        onNodeSelected?.Invoke();
    }

    public void SetNodeData(MapNodeData data, string id = null, NodeType? typeOverride = null)
    {
        nodeData = data;
        if (!string.IsNullOrEmpty(id))
        {
            nodeId = id;
        }

        if (typeOverride.HasValue)
        {
            hasRuntimeNodeType = true;
            runtimeNodeType = typeOverride.Value;
        }
        else
        {
            hasRuntimeNodeType = false;
        }

        EnsureNodeClickSubscription();

        if (nodeImage != null && nodeData != null)
        {
            nodeImage.sprite = nodeData.NodeIcon;
        }

        UpdateVisualState();
    }

    public void SetNodeData(MapNodeData data)
    {
        SetNodeData(data, null, null);
    }

    public void SetRuntimeNodeType(NodeType nodeType)
    {
        hasRuntimeNodeType = true;
        runtimeNodeType = nodeType;
        UpdateVisualState();
    }

    public void SetNodeLevel(int level)
    {
        nodeLevel = Mathf.Max(0, level);
    }

    public void SetAsCurrentPosition(bool isCurrent)
    {
        isCurrentPosition = isCurrent;
        if (isCurrent)
        {
            isRevealed = true;
        }

        UpdateVisualState();
    }

    public void SetAsSelected(bool selected)
    {
        isSelected = selected;
        UpdateVisualState();
    }

    public void SetAsVisited(bool visited)
    {
        isVisited = visited;
        if (visited)
        {
            isRevealed = true;
        }

        UpdateVisualState();
    }

    public void MarkVisited()
    {
        SetAsVisited(true);
    }

    public void SetRevealed(bool revealed)
    {
        isRevealed = revealed;
        UpdateVisualState();
    }

    private void UpdateVisualState()
    {
        if (nodeImage == null)
        {
            return;
        }

        Color baseColor = GetBaseTypeColor();

        if (isCurrentPosition)
        {
            nodeImage.color = Color.Lerp(baseColor, currentColor, 0.55f);
        }
        else if (isSelected)
        {
            nodeImage.color = Color.Lerp(baseColor, selectedColor, 0.55f);
        }
        else if (isVisited)
        {
            nodeImage.color = Color.Lerp(baseColor, visitedColor, 0.45f);
        }
        else
        {
            nodeImage.color = baseColor;
        }

        if (!isRevealed && !isCurrentPosition && !isVisited)
        {
            Color hiddenColor = nodeImage.color;
            hiddenColor.a = 0.12f;
            nodeImage.color = hiddenColor;
        }

        if (nodeButton != null)
        {
            nodeButton.interactable = isRevealed || isCurrentPosition || isVisited;
        }
    }

    private void EnsureNodeClickSubscription()
    {
        if (nodeButton == null)
        {
            return;
        }

        nodeButton.onClick.RemoveListener(OnNodeClicked);
        nodeButton.onClick.AddListener(OnNodeClicked);
    }

    private Color GetBaseTypeColor()
    {
        if (!colorByTypeIfNoIcon)
        {
            return defaultColor;
        }

        NodeType type = EffectiveNodeType;
        switch (type)
        {
            case NodeType.NormalBattle:
                return new Color(0.88f, 0.26f, 0.26f, 1f);
            case NodeType.Elite:
                return new Color(0.75f, 0.19f, 0.7f, 1f);
            case NodeType.Boss:
                return new Color(0.5f, 0.1f, 0.1f, 1f);
            case NodeType.Rest:
                return new Color(0.2f, 0.75f, 0.35f, 1f);
            case NodeType.Shop:
                return new Color(0.95f, 0.75f, 0.2f, 1f);
            case NodeType.RandomEvent:
                return new Color(0.35f, 0.7f, 0.95f, 1f);
            case NodeType.AlchemyPot:
                return new Color(0.48f, 0.9f, 0.82f, 1f);
            case NodeType.TrialGate:
                return new Color(0.95f, 0.44f, 0.22f, 1f);
            case NodeType.GamblingBazaar:
                return new Color(0.96f, 0.6f, 0.25f, 1f);
            case NodeType.FortuneWheel:
                return new Color(0.98f, 0.86f, 0.32f, 1f);
            case NodeType.Crossroads:
                return new Color(0.76f, 0.53f, 0.3f, 1f);
            case NodeType.CardWorkshop:
                return new Color(0.2f, 0.82f, 0.86f, 1f);
            case NodeType.StorageVault:
                return new Color(0.6f, 0.6f, 0.74f, 1f);
            case NodeType.Chronicler:
                return new Color(0.72f, 0.53f, 0.9f, 1f);
            case NodeType.SeerEye:
                return new Color(0.43f, 0.87f, 0.96f, 1f);
            case NodeType.MemoryCandle:
                return new Color(1f, 0.9f, 0.65f, 1f);
            case NodeType.MysticalAltar:
                return new Color(0.63f, 0.38f, 0.87f, 1f);
            case NodeType.Chest:
                return new Color(0.82f, 0.54f, 0.18f, 1f);
            default:
                return defaultColor;
        }
    }
}
