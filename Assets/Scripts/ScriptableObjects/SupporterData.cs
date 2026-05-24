using UnityEngine;

[CreateAssetMenu(fileName = "NewSupporter", menuName = "GameData/Supporter")]
public class SupporterData : ScriptableObject
{
    public string supporterID;
    public string supporterName;

    [Header("이미지")]
    public Sprite mainImage;
    public Sprite iconImage;
    public Sprite worriedSDImage;
    public Sprite readySDImage;
    public Sprite sdImage;
    public Sprite CutIn;
    public Sprite startSkillCutIn;
    public Sprite startSkillImage;
    public Sprite battleSkillImage;
    public Sprite worried;
    public Sprite happy;

    [Header("스킬 설명")]
    [TextArea] public string passiveSkillDesc;
    [TextArea] public string startSkillDesc;
    [TextArea] public string battleSkillDesc;

    [Header("대사")]
    public string selectMessage;
    public string joinMessage;

    [Header("스킬 레벨 (개별 설정)")]
    [Range(1, 3)] public int passiveLevel = 1;
    [Range(1, 3)] public int startSkillLevel = 1;
    [Range(1, 3)] public int battleSkillLevel = 1;

    [Header("스킬 로직")]
    public SupporterLogicBase startSkillLogic;
    public SupporterLogicBase battleSkillLogic;
}