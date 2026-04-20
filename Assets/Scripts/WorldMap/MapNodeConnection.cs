using System.Collections.Generic;
using UnityEngine;

public class MapNodeConnection : MonoBehaviour
{
    [SerializeField] private List<MapNode> connectedNodes = new List<MapNode>();

    public List<MapNode> ConnectedNodes => connectedNodes;

    public void AddConnection(MapNode node)
    {
        if (!connectedNodes.Contains(node))
        {
            connectedNodes.Add(node);
        }
    }

    public bool IsConnectedTo(MapNode node)
    {
        return connectedNodes.Contains(node);
    }
}