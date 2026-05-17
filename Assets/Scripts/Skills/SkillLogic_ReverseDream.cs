using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_ReverseDream", menuName = "SkillLogic/Player/ReverseDream")]
public class SkillLogic_ReverseDream : SkillLogicBase
{
    [Header("기본: 최대 데미지 증폭치 (별과 당신보다 높은 2.0)")]
    public float maxDamageBonus = 2.0f; // 최대 3배 증폭

    [Header("진화 A: 비비드 바이스 (흡혈률 대폭 상승 및 피해감소)")]
    public float[] pathA_LifestealRates = { 0.50f, 0.75f, 1.00f }; // 50~100% 흡혈
    public StatusEffectData pathA_DamageReductionBuff;

    [Header("진화 B: 돌려줘 (버프 강탈)")]
    // 버프 강탈은 별도의 데이터 없이 코드 로직으로 처리합니다.
    public float[] baseLifestealRates = { 0.20f, 0.30f, 0.40f };

    [Header("진화 C: 말보다 더 (다단 히트 & 심연의 출혈)")]
    public int[] pathC_HitCounts = { 8, 10, 12 }; // 레벨별 타수
    public StatusEffectData pathC_BleedDebuff;
    public float pathC_BleedRatePerStack = 1f; // 1스택당 힘의 100%

    // [진화 C] 다단 히트 타수 반환
    public override int GetHitCount(SkillData skill)
    {
        if (skill.currentEvolution == SkillEvolution.PathC)
        {
            int index = Mathf.Clamp(skill.skillLevel - 1, 0, pathC_HitCounts.Length - 1);
            return pathC_HitCounts[index];
        }
        return base.GetHitCount(skill);
    }

    // [진화 C] 다단 히트 시 데미지 분할
    public override float GetDamageMultiplier(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        if (!isPlayerAttacking) return 1.0f;

        float bonus = CombatMath.GetMissingHPMultiplier(pStats.maxHp, pStats.currentHp, maxDamageBonus);

        if (skill.currentEvolution == SkillEvolution.PathC)
        {
            int index = Mathf.Clamp(skill.skillLevel - 1, 0, pathC_HitCounts.Length - 1);
            return bonus / pathC_HitCounts[index]; // 타수만큼 계수를 나누어 총합 데미지를 보존합니다!
        }
        return bonus;
    }

    // [진화 C] 다단 히트 시 브레이크(그로기) 수치 분할
    public override float GetBreakMultiplier(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        if (skill.currentEvolution == SkillEvolution.PathC)
        {
            int index = Mathf.Clamp(skill.skillLevel - 1, 0, pathC_HitCounts.Length - 1);
            // 전체 브레이크 수치(1.0f)를 타수만큼 똑같이 나누어줍니다!
            return 1.0f / pathC_HitCounts[index];
        }

        // 기본 및 진화 A, B는 원래 브레이크 수치(1.0배)를 그대로 적용
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

        // [진화 C] 타격마다 출혈 디버프 중첩! (타수만큼 반복)
        for (int i = 0; i < executionCount; i++)
        {
            if (skill.currentEvolution == SkillEvolution.PathC && pathC_BleedDebuff != null)
            {
                BuffManager.Instance.AddEffect(false, pathC_BleedDebuff, pathC_BleedRatePerStack, 3);
            }
        }

        // [진화 A] 비비드 바이스 전용 - CombatManager가 누적해둔 초과 회복량 사용!
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

        // [진화 B] 버프 강탈 (단 1회 수행)
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