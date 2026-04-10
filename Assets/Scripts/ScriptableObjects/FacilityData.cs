using UnityEngine;

[CreateAssetMenu(fileName = "NewFacility", menuName = "GameData/Node/Facility")]
public class FacilityData : ExplorationNodeData // <- 여기 변경!
{
    // 부모(ExplorationNodeData)가 이미 ID와 Image를 가지고 있으므로 여기선 지웁니다!

    [Header("시설 전용: 조력자 정보")]
    public Sprite operatorImage;
    public Sprite operatorSmileImage;
}