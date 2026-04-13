using UnityEngine;

[CreateAssetMenu(fileName = "NewSupporter", menuName = "GameData/Supporter")]
public class SupporterData : ScriptableObject
{
    public string supporterID;
    public string supporterName;

    [Header("이미지")]
    public Sprite mainImage; // 메인 화면에 뜰 큰 이미지
    public Sprite iconImage; // 하단 목록에 뜰 작은 아이콘
    public Sprite worriedImage;
    public Sprite readyImage;
    public Sprite sdImage;

    [Header("스킬 설명")]
    [TextArea] public string passiveSkillDesc;
    [TextArea] public string startSkillDesc;
    [TextArea] public string battleSkillDesc;

    [Header("대사")]
    public string selectMessage; // 목록에서 클릭했을 때 나오는 대사
    public string joinMessage;   // Join 눌렀을 때 나오는 대사
}