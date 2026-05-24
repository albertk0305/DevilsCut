using UnityEngine;

[CreateAssetMenu(fileName = "NewFacility", menuName = "GameData/Node/Facility")]
public class FacilityData : ExplorationNodeData
{
    [Header("시설 전용: 조력자 정보")]
    public string operatorName;
    public Sprite operatorImage;
    public Sprite operatorSmileImage;
}