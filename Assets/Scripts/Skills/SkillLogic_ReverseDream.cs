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

    public override void ApplyEffectOnHit(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking, bool isHit)
    {
        if (!isHit || !isPlayerAttacking) return;

        int index = Mathf.Clamp(skill.skillLevel - 1, 0, baseLifestealRates.Length - 1);
        float currentLifestealRate = (skill.currentEvolution == SkillEvolution.PathA) ? pathA_LifestealRates[index] : baseLifestealRates[index];

        // [수정됨] 진화 C일 경우, 실제 적중한 횟수(lastSuccessfulHits)만큼 효과를 반복 적용합니다.
        int executionCount = (skill.currentEvolution == SkillEvolution.PathC) ? CombatManager.Instance.currentState.lastSuccessfulHits : 1;

        for (int i = 0; i < executionCount; i++)
        {
            // 1. 타격당 예상 데미지 및 흡혈량 산출 (진화 C는 계수가 이미 타수만큼 나눠져 있음)
            float skillMultiplier = skill.GetCurrentDamageMultiplier() * GetDamageMultiplier(skill, pStats, enemy, isPlayerAttacking);
            int defenderDef = StatManager.Instance.GetEffectiveStat(false, TargetStat.Defense);
            float expectedDamage = (pStats.strength * skillMultiplier) * (1f - CombatMath.GetDamageReduction(defenderDef));
            int healAmount = Mathf.RoundToInt(expectedDamage * currentLifestealRate);

            // 2. 체력 회복 적용
            int excessHeal = (pStats.currentHp + healAmount) - pStats.maxHp;
            pStats.currentHp = Mathf.Clamp(pStats.currentHp + healAmount, 0, pStats.maxHp);

            if (CombatUIManager.Instance != null && healAmount > 0)
            {
                CombatUIManager.Instance.playerStatusUI.UpdateHP(pStats.currentHp, pStats.maxHp);
                CombatUIManager.Instance.SpawnDamageText($"<color=#00FF00>+{healAmount}</color>", false, true);
            }

            // [진화 A] 비비드 바이스 전용
            if (skill.currentEvolution == SkillEvolution.PathA && excessHeal > 0 && pathA_DamageReductionBuff != null)
            {
                float reductionValue = Mathf.Clamp((float)excessHeal / pStats.maxHp, 0.05f, 0.50f);
                BuffManager.Instance.AddEffect(true, pathA_DamageReductionBuff, reductionValue, 3);
            }

            // [진화 C] 타격마다 출혈 디버프 중첩!
            if (skill.currentEvolution == SkillEvolution.PathC && pathC_BleedDebuff != null)
            {
                BuffManager.Instance.AddEffect(false, pathC_BleedDebuff, pathC_BleedRatePerStack, 3);
            }
        }

        // [진화 B] 버프 강탈 (강탈은 한 번만 수행하도록 루프 밖으로 뺍니다)
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