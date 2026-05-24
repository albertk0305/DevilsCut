using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Crusader", menuName = "SkillLogic/Player/Crusader")]
public class SkillLogic_Crusader : SkillLogicBase
{
    [Header("기본: 레벨별 그로기 추가 데미지 증폭률")]
    public float[] bonusDamageRatesOnBreak = { 0.50f, 0.75f, 1.0f };

    [Header("진화 A (Bloody Stream)")]
    public StatusEffectData defDownEffect;
    public StatusEffectData brDownEffect;
    public float[] pathA_DebuffPerHitRates = { 0.02f, 0.03f, 0.04f };

    [Header("진화 B (Stand Proud)")]
    // Damage scaling rule.
    public float[] pathB_CompoundRates = { 1.10f, 1.15f, 1.20f };

    [Header("진화 C (Last Train Home)")]
    public StatusEffectData timeBombEffect;
    // Damage scaling rule.
    public float[] pathC_DamageMults = { 2.0f, 2.5f, 3.0f };


    public override bool AlwaysHits(SkillData skill)
    {
        if (skill.currentEvolution == SkillEvolution.PathC) return true;
        return base.AlwaysHits(skill);
    }

    // Path C rule.
    public override float GetDamageMultiplier(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        // Path C rule.
        if (skill.currentEvolution == SkillEvolution.PathC) return 0f;

        bool isTargetBroken = BreakManager.Instance.IsBroken(!isPlayerAttacking);
        if (isTargetBroken)
        {
            int index = Mathf.Clamp(skill.skillLevel - 1, 0, bonusDamageRatesOnBreak.Length - 1);
            return 1.0f + bonusDamageRatesOnBreak[index];
        }
        return 1.0f;
    }

    // Path B rule.
    public override float GetDynamicDamageMultiplier(SkillData skill, int consecutiveHits)
    {
        if (skill.currentEvolution == SkillEvolution.PathB)
        {
            int index = Mathf.Clamp(skill.skillLevel - 1, 0, pathB_CompoundRates.Length - 1);
            float compoundRate = pathB_CompoundRates[index];

            return Mathf.Pow(compoundRate, consecutiveHits);
        }
        return 1.0f;
    }

    // Path C rule.
    public override void PaySkillCost(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        if (skill.currentEvolution == SkillEvolution.PathC && isPlayerAttacking)
        {
            // Damage scaling rule.
            int index = Mathf.Clamp(skill.skillLevel - 1, 0, pathC_DamageMults.Length - 1);
            float snapshotMult = pathC_DamageMults[index];

            float skillMult = skill.GetCurrentDamageMultiplier();
            int hits = skill.GetCurrentHitCount();
            if (hits <= 1) hits = 10;

            int def = StatManager.Instance.GetEffectiveStat(false, TargetStat.Defense);
            float dr = CombatMath.GetDamageReduction(def);

            float rawDmg = (pStats.strength * skillMult) * (1f - dr);
            int totalDmg = Mathf.RoundToInt(rawDmg * hits * snapshotMult);

            CombatManager.Instance.currentState.savedBombDamage = totalDmg;
            CombatManager.Instance.currentState.isBombActive = true;

            if (timeBombEffect != null)
            {
                BuffManager.Instance.AddEffect(false, timeBombEffect, totalDmg, 1);
            }

            DevLog.Log($"[진화 C] 라스트 트레인 홈(Lv.{skill.skillLevel}) 장전! 배율 {snapshotMult}x -> {totalDmg} 피해 대기 중.");
        }
    }

    // Path C rule.
    public override int GetHitCount(SkillData skill)
    {
        if (skill.currentEvolution == SkillEvolution.PathC) return 1;
        return base.GetHitCount(skill);
    }

    // Path A rule.
    public override void ApplyEffectOnHit(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking, bool isHit)
    {
        if (!isHit || !isPlayerAttacking) return;

        if (skill.currentEvolution == SkillEvolution.PathA)
        {
            // Buff/debuff rule.
            int index = Mathf.Clamp(skill.skillLevel - 1, 0, pathA_DebuffPerHitRates.Length - 1);
            float debuffPerHit = pathA_DebuffPerHitRates[index];

            int hitCount = CombatManager.Instance.currentState.lastSuccessfulHits;
            float totalDebuff = -(debuffPerHit * hitCount);

            if (defDownEffect != null) BuffManager.Instance.AddEffect(false, defDownEffect, totalDebuff, 3);
            if (brDownEffect != null) BuffManager.Instance.AddEffect(false, brDownEffect, totalDebuff, 3);

            DevLog.Log($"[진화 A] 블러디 스트림(Lv.{skill.skillLevel})! {hitCount}타 적중. 방어력/BR을 {Mathf.Abs(totalDebuff) * 100}% 감소시킵니다.");
        }
    }
}