using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ExplorationNodeDatabase", menuName = "GameData/ExplorationNodeDatabase")]
public class ExplorationNodeDatabase : ScriptableObject
{
    public List<ExplorationNodeData> allNodes = new List<ExplorationNodeData>();

    public ExplorationNodeData GetByID(string nodeID)
    {
        if (string.IsNullOrEmpty(nodeID))
        {
            Debug.LogWarning("ExplorationNodeDatabase: empty nodeID.");
            return null;
        }

        ExplorationNodeData found = null;

        foreach (ExplorationNodeData node in allNodes)
        {
            if (node == null || node.nodeID != nodeID)
                continue;

            if (found != null)
                Debug.LogWarning($"ExplorationNodeDatabase: duplicate nodeID '{nodeID}'.");

            found = node;
        }

        return found;
    }
}
