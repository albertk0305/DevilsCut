using UnityEngine;

public enum SkillCategory { Sword = 0, Gun = 1, Martial = 2, Magic = 3, Oni = 4, None = 5 }

[CreateAssetMenu(fileName = "NewSkill", menuName = "GameData/Skill")]
public class SkillData : ScriptableObject
{
    [Header("스킬 고유 로직 (진화 포함)")]
    public SkillLogicBase skillLogic;

    [Header("다국어 번역 키")]
    public string skillNameKey;
    public string skillDescKey;

    [Header("스킬 기본 설정")]
    public SkillCategory category;
    public int skillLevel = 1; // 현재 스킬 레벨 (기본값 1)

    [Header("스킬 연출")]
    public Sprite skillActionImage;

    [Header("스킬 로직 데이터 (레벨별)")]
    [Tooltip("순서대로 Lv1, Lv2, Lv3... 의 피해 계수입니다.")]
    public float[] damageMultipliers = { 1.0f };

    [Tooltip("순서대로 Lv1, Lv2, Lv3... 의 브레이크 피해량입니다.")]
    public float[] breakPowers = { 20f };

    public float baseAccuracy = 80f;
    public float bonusCritRate = 0f;

    [Header("스킬 유형")]
    public bool isUltimate = false;
    public int hitCount = 1;

    public float GetCurrentDamageMultiplier()
    {
        if (damageMultipliers == null || damageMultipliers.Length == 0) return 0f;

        // 인덱스는 0부터 시작하므로 레벨에서 1을 뺍니다. (Lv 1 -> index 0)
        int index = Mathf.Clamp(skillLevel - 1, 0, damageMultipliers.Length - 1);
        return damageMultipliers[index];
    }

    public float GetCurrentBreakPower()
    {
        if (breakPowers == null || breakPowers.Length == 0) return 0f;

        int index = Mathf.Clamp(skillLevel - 1, 0, breakPowers.Length - 1);
        return breakPowers[index];
    }
}