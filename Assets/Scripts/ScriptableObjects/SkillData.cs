using UnityEngine;

public enum SkillCategory { Sword = 0, Gun = 1, Martial = 2, Magic = 3, Oni = 4, None = 5 }
public enum SkillEvolution { None, PathA, PathB, PathC }

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

    [Header("크리티컬 보정 데이터 (레벨별)")]
    [Tooltip("순서대로 Lv1, Lv2, Lv3... 의 추가 크리 확률입니다. (기본 0)")]
    public float[] bonusCritRates = { 0f };

    [Header("명중 보정 데이터 (레벨별)")]
    [Tooltip("순서대로 Lv1, Lv2, Lv3... 의 추가 명중률입니다. (기본 0)")]
    public float[] bonusAccuracyRates = { 0f };

    public float baseAccuracy = 80f;

    [Header("스킬 유형")]
    public bool isUltimate = false;
    [Tooltip("순서대로 Lv1, Lv2, Lv3... 의 타격 횟수입니다.")]
    public int[] hitCounts = { 1 };

    [Header("스킬 진화 (로그라이크)")]
    public SkillEvolution currentEvolution = SkillEvolution.None; // 현재 선택된 진화 상태

    [Tooltip("진화 1(PathA - 인과율 등)의 레벨별 계수")]
    public float[] evolutionA_Multipliers = { 0.5f, 0.75f, 1.0f }; // 반사율: 50%, 75%, 100%

    [Tooltip("진화 B (고드 핸드) 레벨별 계수 (피해 감소율)")]
    public float[] evolutionB_Multipliers = { 0.4f, 0.5f, 0.6f };

    [Tooltip("진화 C (제물의 낙인) 레벨별 딜 계수")]
    public float[] evolutionC_DamageMultipliers = { 15.0f, 20.0f, 25.0f };

    [Tooltip("진화 C (제물의 낙인) 레벨별 브레이크 수치")]
    public float[] evolutionC_BreakPowers = { 25.0f, 30.0f, 35.0f };

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

    public float GetCurrentBonusCritRate()
    {
        if (bonusCritRates == null || bonusCritRates.Length == 0) return 0f;
        int index = Mathf.Clamp(skillLevel - 1, 0, bonusCritRates.Length - 1);
        return bonusCritRates[index];
    }

    public int GetCurrentHitCount()
    {
        if (hitCounts == null || hitCounts.Length == 0) return 1;
        int index = Mathf.Clamp(skillLevel - 1, 0, hitCounts.Length - 1);
        return Mathf.Max(1, hitCounts[index]); // 최소 1타는 보장
    }

    public float GetCurrentBonusAccuracy()
    {
        if (bonusAccuracyRates == null || bonusAccuracyRates.Length == 0) return 0f;
        int index = Mathf.Clamp(skillLevel - 1, 0, bonusAccuracyRates.Length - 1);
        return bonusAccuracyRates[index];
    }
}