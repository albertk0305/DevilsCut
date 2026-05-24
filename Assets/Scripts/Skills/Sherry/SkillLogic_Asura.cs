using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Asura", menuName = "SkillLogic/Player/Asura")]
public class SkillLogic_Asura : SkillLogicBase
{
    [Header("기본: 버프 데이터 (공격력/방어력)")]
    public StatusEffectData strengthBuff;
    public StatusEffectData defenseBuff;

    [Header("기본: 레벨별 공/방 증가율 (%)")]
    // Lv.1: 25%, Lv.2: 40%, Lv.3: 60%
    public float[] buffRates = { 0.25f, 0.40f, 0.60f };

    [Header("진화 A: AP(속도) 상승 버프")]
    public StatusEffectData speedBuff;
    public float[] pathA_SpeedRates = { 0.15f, 0.20f, 0.25f }; // 15%, 20%, 25%

    [Header("진화 B: 방어력 희생 -> 힘(Flat) 합산")]
    public StatusEffectData defenseDebuff;
    public StatusEffectData flatStrengthBuff;
    public float[] pathB_DefDebuffRates = { 0.30f, 0.40f, 0.50f };
    public float[] pathB_StrMultipliers = { 2.0f, 2.5f, 3.0f };

    [Header("진화 C: 버프 포기 -> 데미지 스킬화")]
    public float[] pathC_DamageMults = { 50.0f, 70.0f, 90.0f };

    public override bool AlwaysHits(SkillData skill) => true;

    public override int GetHitCount(SkillData skill)
    {
        if (skill.currentEvolution == SkillEvolution.PathC) return 1;
        return base.GetHitCount(skill);
    }

    // Path C rule.
    public override float GetDamageMultiplier(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        if (skill.currentEvolution == SkillEvolution.PathC)
        {
            int index = Mathf.Clamp(skill.skillLevel - 1, 0, buffRates.Length - 1);
            float buffRate = buffRates[index];
            float cMult = pathC_DamageMults[index];

            // Buff/debuff rule.
            float expectedStrIncrease = pStats.strength * buffRate;
            float expectedDefIncrease = pStats.defense * buffRate;
            float totalExpected = expectedStrIncrease + expectedDefIncrease;

            // Damage scaling rule.
            // Damage scaling rule.
            return (totalExpected * cMult) / Mathf.Max(1, pStats.strength);
        }

        // Path A rule.
        return 0f;
    }

    // Buff/debuff rule.
    public override void ApplyEffectOnHit(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking, bool isHit)
    {
        if (!isHit || !isPlayerAttacking) return;

        int index = Mathf.Clamp(skill.skillLevel - 1, 0, buffRates.Length - 1);
        float baseBuffRate = buffRates[index];

        // Path C rule.
        if (skill.currentEvolution == SkillEvolution.PathC)
        {
            DevLog.Log($"[진화 C] 아수라 발동! 공/방 버프를 폭발력으로 전환하여 강력한 피해를 입혔습니다.");
            return;
        }

        // Path A rule.
        if (strengthBuff != null) BuffManager.Instance.AddEffect(isPlayerAttacking, strengthBuff, baseBuffRate, 3);

        if (skill.currentEvolution == SkillEvolution.None || skill.currentEvolution == SkillEvolution.PathA)
        {
            // Buff/debuff rule.
            if (defenseBuff != null) BuffManager.Instance.AddEffect(isPlayerAttacking, defenseBuff, baseBuffRate, 3);

            // Path A rule.
            if (skill.currentEvolution == SkillEvolution.PathA && speedBuff != null)
            {
                BuffManager.Instance.AddEffect(isPlayerAttacking, speedBuff, pathA_SpeedRates[index], 3);
                DevLog.Log($"[진화 A] 아수라! 3턴간 공/방 {baseBuffRate * 100}% 및 AP(속도) {pathA_SpeedRates[index] * 100}% 상승.");
            }
            else
            {
                DevLog.Log($"[기본] 아수라! 3턴간 공/방 {baseBuffRate * 100}% 증가.");
            }
        }
        else if (skill.currentEvolution == SkillEvolution.PathB)
        {
            // Path B rule.
            float defDropRate = pathB_DefDebuffRates[index];
            float strMult = pathB_StrMultipliers[index];

            int lostDef = Mathf.RoundToInt(pStats.defense * defDropRate);
            int bonusFlatStr = Mathf.RoundToInt(lostDef * strMult);

            // Buff/debuff rule.
            if (defenseDebuff != null) BuffManager.Instance.AddEffect(isPlayerAttacking, defenseDebuff, -defDropRate, 3);

            // Buff/debuff rule.
            if (flatStrengthBuff != null) BuffManager.Instance.AddEffect(isPlayerAttacking, flatStrengthBuff, bonusFlatStr, 3);

            DevLog.Log($"[진화 B] 아수라! 방어력이 {defDropRate * 100}%(-{lostDef}) 깎인 대가로, 기본 힘 상승에 더해 {bonusFlatStr}의 고정 힘을 추가로 얻습니다!");
        }
    }
}