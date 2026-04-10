using UnityEngine;

[CreateAssetMenu(fileName = "NewDanger", menuName = "GameData/Node/Danger")]
public class DangerNodeData : ExplorationNodeData
{
    [Header("위험 전용 정보")]
    public int enemyLevel;          // 등장할 적의 레벨
    // 나중에 몬스터 프리팹이나 드롭 아이템 데이터를 여기에 추가!
}