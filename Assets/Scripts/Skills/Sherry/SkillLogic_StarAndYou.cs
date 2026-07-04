using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_StarAndYou", menuName = "SkillLogic/Player/StarAndYou")]
public class SkillLogic_StarAndYou : SkillLogicBase
{
    [Header("기본: 최대 데미지 증폭치")]
    public float baseMaxDamageBonus = 1.5f;

    [Header("진화 A: 네가 모르는 이야기")]
    public float pathA_MaxBonusMult = 3.0f;
    public float pathA_CriticalSpikeMult = 3.0f;

    [Header("진화 B: 내 사랑 (잃은 체력 비례 크리 상승)")]
    public float[] pathB_MaxCritRateBonus = { 30f, 40f, 50f };
    // Damage scaling rule.
    public float[] pathB_MaxCritDmgBonus = { 0.5f, 1.0f, 1.5f };

    [Header("진화 C: 언데드 (소모 체력 비율 비례 폭딜)")]
    public float[] pathC_ConsumedHpMults = { 10.0f, 15.0f, 20.0f };

    // Damage scaling rule.
    public override float GetDamageMultiplier(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        if (!isPlayerAttacking) return 1.0f;

        float hpRatio = (float)pStats.currentHp / pStats.maxHp;
        float missingRatio = 1.0f - hpRatio;

        // ---------------------------------------------------------
        // Path A rule.
        // ---------------------------------------------------------
        if (skill.currentEvolution == SkillEvolution.PathA)
        {
            float baseMult = 1.0f + (missingRatio * pathA_MaxBonusMult);
            if (hpRatio <= 0.1f)
            {
                baseMult *= pathA_CriticalSpikeMult;
                DevLog.Log($"[진화 A] 빈사 상태 달성! 별과 당신의 피해량이 {baseMult:F1}배로 폭증합니다!");
            }
            return baseMult;
        }

        // ---------------------------------------------------------
        // Path C rule.
        // ---------------------------------------------------------
        if (skill.currentEvolution == SkillEvolution.PathC)
        {
            // HP cost/recovery rule.
            float consumedRatio = (float)(Mathf.Max(0, pStats.currentHp - 1)) / pStats.maxHp;
            int levelIdx = Mathf.Clamp(skill.skillLevel - 1, 0, 2);

            float undeadMult = 1.0f + (consumedRatio * pathC_ConsumedHpMults[levelIdx]);
            DevLog.Log($"[진화 C] 언데드 딜 산출! 체력 {consumedRatio * 100:F1}% 소모 예정 -> 데미지 {undeadMult:F1}배 폭증!");
            return undeadMult;
        }

        // ---------------------------------------------------------
        // Path B rule.
        // ---------------------------------------------------------
        return 1.0f + (missingRatio * baseMaxDamageBonus);
    }

    // Path B rule.
    public override float GetDynamicCritRateBonus(SkillData skill, int consecutiveHits)
    {
        if (skill.currentEvolution == SkillEvolution.PathB && CombatManager.Instance != null)
        {
            PlayerStats pStats = CombatManager.Instance.GetCurrentPlayerStats();
            float missingRatio = 1.0f - ((float)pStats.currentHp / pStats.maxHp);
            int levelIdx = Mathf.Clamp(skill.skillLevel - 1, 0, 2);

            return missingRatio * pathB_MaxCritRateBonus[levelIdx];
        }
        return 0f;
    }

    // Path B rule.
    public override float GetCritDamageMultiplier(SkillData skill)
    {
        if (skill.currentEvolution == SkillEvolution.PathB && CombatManager.Instance != null)
        {
            PlayerStats pStats = CombatManager.Instance.GetCurrentPlayerStats();
            float missingRatio = 1.0f - ((float)pStats.currentHp / pStats.maxHp);
            int levelIdx = Mathf.Clamp(skill.skillLevel - 1, 0, 2);

            // Damage scaling rule.
            return 1.5f + (missingRatio * pathB_MaxCritDmgBonus[levelIdx]);
        }
        return 1.5f;
    }

    // HP cost/recovery rule.
    public override void PaySkillCost(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        if (isPlayerAttacking)
        {
            int hpCost = 0;

            // ---------------------------------------------------------
            // Path C rule.
            // ---------------------------------------------------------
            if (skill.currentEvolution == SkillEvolution.PathC)
            {
                hpCost = ApplyNonlethalHpCost(pStats, pStats.currentHp - 1);
                DevLog.Log($"[진화 C] 언데드 발동! 생명력을 대가로 화력을 얻습니다. (소모 체력: {hpCost})");
            }
            // ---------------------------------------------------------
            // Path A rule.
            // ---------------------------------------------------------
            else
            {
                int calculatedCost = Mathf.Max(1, Mathf.RoundToInt(pStats.currentHp * 0.2f));
                hpCost = ApplyNonlethalHpCost(pStats, calculatedCost);
                DevLog.Log($"[별과 당신] 체력의 20%({hpCost})를 코스트로 지불했습니다.");
            }

            BattleEventSystem.CallHpChanged(true, pStats.currentHp, pStats.maxHp);
            if (hpCost > 0 && CombatUIManager.Instance != null)
            {
                CombatUIManager.Instance.SpawnDamageText($"-{hpCost}", false, true);
            }
        }
    }
}
