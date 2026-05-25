using UnityEngine;

[CreateAssetMenu(fileName = "NewFacility", menuName = "GameData/Node/Facility")]
public class FacilityData : ExplorationNodeData
{
    [Header("시설 전용: 조력자 정보")]
    public string operatorName;
    public Sprite operatorImage;
    public Sprite operatorSmileImage;

    [Header("시설 전용: 연결 조력자")]
    public SupporterData linkedSupporter;

    [Header("시설 전용: 랭크업 대화")]
    public DialogueData rank0To1Dialogue;
    public DialogueData rank1To2Dialogue;
    public DialogueData rank2To3Dialogue;
}
