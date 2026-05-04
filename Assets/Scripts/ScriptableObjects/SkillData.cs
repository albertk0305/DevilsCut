using UnityEngine;

public enum SkillCategory { Sword = 0, Gun = 1, Martial = 2, Magic = 3, Oni = 4 }

public enum SkillID
{
    None,
    Sword_Deflect, Sword_SpaceSlash, Sword_Dance, Sword_Excalibur,
    Gun_AngelArm, Gun_InfiniteBullet, Gun_Snipe, Gun_GateOfBabylon,
    Hit_Compass, Hit_Fajing, Hit_Energy, Hit_Rush,
    Magic_Payment, Magic_Sandevistan, Magic_Restrain, Magic_Infinite,
    Oni_Taunt, Oni_WitchHunt, Oni_Kokorowatari, Oni_Shrine
}

[CreateAssetMenu(fileName = "NewSkill", menuName = "GameData/Skill")]
public class SkillData : ScriptableObject
{
    [Header("다국어 번역 키")]
    public string skillNameKey; // 예: "skill_sword_slash"
    public string skillDescKey; // 예: "desc_sword_slash"

    [Header("스킬 기본 설정")]
    public SkillCategory category;
    public SkillID specificId; // 이 스킬이 정확히 어떤 스킬인지 식별
    public int skillLevel = 1; // 기획서에 있는 Lv.1 ~ Lv.3 구분용

    [Header("스킬 연출")]
    public Sprite skillActionImage; // 스킬 사용 시 주인공 얼굴을 교체할 이미지!

    [Header("스킬 로직 데이터")]
    public float damageMultiplier = 1.0f; // 기본 딜 계수
    public float breakPower = 20f;        // 브레이크 깎는 수치
    public float baseAccuracy = 80f; // [추가] 기본 명중률
}