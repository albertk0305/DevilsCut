using UnityEngine;

// Base class only; derived node types define their own CreateAssetMenu entries.
public class ExplorationNodeData : ScriptableObject
{
    [Header("기본 노드 정보")]
    public string nodeID;
    public Sprite nodeImage;
}