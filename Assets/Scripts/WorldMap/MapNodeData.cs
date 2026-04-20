using UnityEngine;

[CreateAssetMenu(fileName = "MapNode_", menuName = "World Map/Node Data")]
public class MapNodeData : ScriptableObject
{
    [SerializeField] private NodeType nodeType;
    [SerializeField] private Sprite nodeIcon;
    [SerializeField] private string nodeName;
    [SerializeField] private float costMultiplier = 1f;
    [SerializeField] private string description;
    [SerializeField] private int riskLevel;
    [SerializeField] private string uniqueConditionId;

    public NodeType NodeType => nodeType;
    public Sprite NodeIcon => nodeIcon;
    public string NodeName => nodeName;
    public float CostMultiplier => costMultiplier;
    public string Description => description;
    public int RiskLevel => riskLevel;
    public string UniqueConditionId => uniqueConditionId;
}
