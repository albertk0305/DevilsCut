using UnityEngine;

[CreateAssetMenu(fileName = "NewEvent", menuName = "GameData/Node/Event")]
public class EventNodeData : ExplorationNodeData
{
    [Header("이벤트 전용 정보")]
    [TextArea]
    public string eventDescription; // 이벤트 내용 텍스트
    // 나중에 보상 데이터나 선택지 데이터를 여기에 추가하면 됩니다!
}