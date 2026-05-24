using UnityEngine;

[CreateAssetMenu(fileName = "NewDanger", menuName = "GameData/Node/Danger")]
public class DangerNodeData : ExplorationNodeData
{
    [Header("위험 전용 정보")]
    public int enemyLevel;
    public EnemyData enemyToSpawn;
}