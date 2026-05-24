using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_ReverseDream", menuName = "SkillLogic/Player/ReverseDream")]
public class SkillLogic_ReverseDream : SkillLogicBase
{
    [Header("기본: 최대 데미지 증폭치 (별과 당신보다 높은 2.0)")]
    public float maxDamageBonus = 2.0f;

    [Header("진화 A: 비비드 바이스 (흡혈률 대폭 상승 및 피해감소)")]
    public float[] pathA_LifestealRates = { 0.50f, 0.75f, 1.00f };
    public StatusEffectData pathA_DamageReductionBuff;

    [Header("진화 B: 돌려줘 (버프 강탈)")]
    // Buff/debuff rule.
    public float[] baseLifestealRates = { 0.20f, 0.30f, 0.40f };

    [Header("진화 C: 말보다 더 (다단 히트 & 심연의 출혈)")]
    public int[] pathC_HitCounts = { 8, 10, 12 };
    public StatusEffectData pathC_BleedDebuff;
    public float pathC_BleedRatePerStack = 1f;

    // Path C rule.
    public override int GetHitCount(SkillData skill)
    {
        if (skill.currentEvolution == SkillEvolution.PathC)
        {
            int index = Mathf.Clamp(skill.skillLevel - 1, 0, pathC_HitCounts.Length - 1);
            return pathC_HitCounts[index];
        }
        return base.GetHitCount(skill);
    }

    // Path C rule.
    public override float GetDamageMultiplier(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        if (!isPlayerAttacking) return 1.0f;

        float bonus = CombatMath.GetMissingHPMultiplier(pStats.maxHp, pStats.currentHp, maxDamageBonus);

        if (skill.currentEvolution == SkillEvolution.PathC)
        {
            int index = Mathf.Clamp(skill.skillLevel - 1, 0, pathC_HitCounts.Length - 1);
            return bonus / pathC_HitCounts[index];
        }
        return bonus;
    }

    // Path C rule.
    public override float GetBreakMultiplier(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        if (skill.currentEvolution == SkillEvolution.PathC)
        {
            int index = Mathf.Clamp(skill.skillLevel - 1, 0, pathC_HitCounts.Length - 1);
            // Multi-hit rule.
            return 1.0f / pathC_HitCounts[index];
        }

        // Path A rule.
        return 1.0f;
    }

    public override float GetSkillBonusLifesteal(SkillData skill)
    {
        int index = Mathf.Clamp(skill.skillLevel - 1, 0, baseLifestealRates.Length - 1);
        return (skill.currentEvolution == SkillEvolution.PathA) ? pathA_LifestealRates[index] : baseLifestealRates[index];
    }

    public override void ApplyEffectOnHit(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking, bool isHit)
    {
        if (!isHit || !isPlayerAttacking) return;

        int executionCount = (skill.currentEvolution == SkillEvolution.PathC) ? CombatManager.Instance.currentState.lastSuccessfulHits : 1;

        // Path C rule.
        for (int i = 0; i < executionCount; i++)
        {
            if (skill.currentEvolution == SkillEvolution.PathC && pathC_BleedDebuff != null)
            {
                BuffManager.Instance.AddEffect(false, pathC_BleedDebuff, pathC_BleedRatePerStack, 3);
            }
        }

        // Path A rule.
        if (skill.currentEvolution == SkillEvolution.PathA && pathA_DamageReductionBuff != null)
        {
            int totalExcessHeal = CombatManager.Instance.currentState.totalExcessHealThisSkill;
            if (totalExcessHeal > 0)
            {
                float reductionValue = Mathf.Clamp((float)totalExcessHeal / pStats.maxHp, 0.05f, 0.50f);
                BuffManager.Instance.AddEffect(true, pathA_DamageReductionBuff, reductionValue, 3);
                DevLog.Log($"[비비드 바이스] 초과 회복량 {totalExcessHeal} 달성 -> 피해 감소 {reductionValue * 100}% 버프 획득!");
            }
        }

        // Path B rule.
        if (skill.currentEvolution == SkillEvolution.PathB)
        {
            var enemyEffects = BuffManager.Instance.GetEffects(false);
            var targetBuff = enemyEffects.Find(e => e.effectData.category == EffectCategory.Buff);

            if (targetBuff != null)
            {
                BuffManager.Instance.AddEffect(true, targetBuff.effectData, targetBuff.value, targetBuff.turnsLeft);
                enemyEffects.Remove(targetBuff);
                if (CombatUIManager.Instance != null) CombatUIManager.Instance.RefreshBuffUI();
                DevLog.Log($"[진화 B] 거꾸로 된 꿈! 적의 버프를 훔쳤습니다.");
            }
        }
    }
}