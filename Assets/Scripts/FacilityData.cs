using UnityEngine;

// 이 줄을 넣으면 유니티 에디터에서 우클릭으로 이 데이터 파일을 붕어빵 찍듯 만들어낼 수 있어!
[CreateAssetMenu(fileName = "NewFacility", menuName = "GameData/FacilityData")]
public class FacilityData : ScriptableObject
{
    [Header("기본 정보")]
    public string facilityID;      // 예: "shop", "hospital"
    public string facilityNameKey; // 다국어 번역을 위한 키 (예: "fac_shop")
    public Sprite facilityImage;   // 시설 이미지

    [Header("조력자 정보")]
    public Sprite operatorImage;   // 이 시설의 고정 조력자 이미지 (없을 때 띄울 실루엣 이미지 등)
    public Sprite operatorSmileImage;   // 웃는 표정 (클릭 시)
}