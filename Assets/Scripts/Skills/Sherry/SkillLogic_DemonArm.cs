using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_DemonArm", menuName = "SkillLogic/Player/DemonArm")]
public class SkillLogic_DemonArm : SkillLogicBase
{
    [Header("진화 A (Come the Light)")]
    public float pathA_CritMultiplier = 2.5f;

    [Header("진화 B (Stars)")]
    [Tooltip("크리 100% 초과분 1%당 상승할 추가 데미지 비율 (1.0 = 1%)")]
    public float pathB_OverflowConversionRate = 1.0f;

    // Path C rule.
    private int currentEmptyChamberIndex = -1;

    // Path C rule.
    public override int GetHitCount(SkillData skill)
    {
        if (skill.currentEvolution == SkillEvolution.PathC)
        {
            return currentEmptyChamberIndex + 1;
        }
        return base.GetHitCount(skill);
    }

    // Path C rule.
    public override void PaySkillCost(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        if (skill.currentEvolution == SkillEvolution.PathC && isPlayerAttacking)
        {
            currentEmptyChamberIndex = Random.Range(0, 6);
            DevLog.Log($"[러시안 룰렛] 철컥! {currentEmptyChamberIndex + 1}번째 총알이 비어있습니다...");
        }
    }

    // Path C rule.
    public override bool AlwaysMisses(SkillData skill, int hitIndex)
    {
        if (skill.currentEvolution == SkillEvolution.PathC)
        {
            return hitIndex == currentEmptyChamberIndex;
        }
        return false;
    }

    // Path A rule.
    public override float GetCritDamageMultiplier(SkillData skill)
    {
        if (skill.currentEvolution == SkillEvolution.PathA) return pathA_CritMultiplier;
        return base.GetCritDamageMultiplier(skill);
    }

    // Path B rule.
    public override float GetDamageMultiplier(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        float multiplier = 1.0f;

        if (skill.currentEvolution == SkillEvolution.PathB && isPlayerAttacking)
        {
            // Path B rule.
            float finalCritRate = CombatMath.GetFinalCritRate(skill.GetCurrentBonusCritRate(), pStats.luck);
            if (finalCritRate > 100f)
            {
                float overflow = finalCritRate - 100f;
                float bonus = (overflow * pathB_OverflowConversionRate) / 100f;
                multiplier += bonus;
                DevLog.Log($"[별 부스러기] 크리 확률 {overflow:F1}% 초과! 데미지가 {bonus * 100:F1}% 증폭됩니다!");
            }
        }
        else if (skill.currentEvolution == SkillEvolution.PathC)
        {
            // Path C rule.
            multiplier = 1.0f / 6.0f;
        }

        return multiplier;
    }
}