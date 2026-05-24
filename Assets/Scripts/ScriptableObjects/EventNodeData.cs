using UnityEngine;

[CreateAssetMenu(fileName = "NewEvent", menuName = "GameData/Node/Event")]
public class EventNodeData : ExplorationNodeData
{
    [Header("이벤트 전용 정보")]
    [TextArea]
    public string eventDescription;
}