using UnityEngine;

[CreateAssetMenu(fileName = "FacilityRankBonusInfo", menuName = "GameData/Facility/Rank Bonus Info")]
public class FacilityRankBonusInfo : ScriptableObject
{
    public string facilityID;
    public Sprite[] rankSprites;
    public string[] rankDescriptions;
}
