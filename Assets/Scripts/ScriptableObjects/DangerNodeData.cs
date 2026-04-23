using UnityEngine;

[CreateAssetMenu(fileName = "NewDanger", menuName = "GameData/Node/Danger")]
public class DangerNodeData : ExplorationNodeData
{
    [Header("위험 전용 정보")]
    public int enemyLevel;          // 등장할 적의 레벨
    public EnemyData enemyToSpawn; // [추가] 이 칸을 밟으면 등장할 적의 데이터
}