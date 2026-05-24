using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_OnisTreasure", menuName = "SkillLogic/Player/OnisTreasure")]
public class SkillLogic_OnisTreasure : SkillLogicBase
{
    // ==========================================
    // Path A rule.
    // ==========================================
    public override int GetHitCount(SkillData skill)
    {
        int baseHits = base.GetHitCount(skill);

        if (skill.currentEvolution == SkillEvolution.PathA)
        {
            int currentLuck = StatManager.Instance.GetEffectiveStat(true, TargetStat.Luck);

            int extraHits = currentLuck / 10;

            if (extraHits > 0)
            {
                DevLog.Log($"[진화 A] 컬렉터 EX 발동! 운({currentLuck}) 비례 타수 {extraHits}타 추가! (총 {baseHits + extraHits}타 발사)");
            }
            return baseHits + extraHits;
        }

        return baseHits;
    }

    // ==========================================
    // Path B rule.
    // ==========================================
    public override bool AlwaysHits(SkillData skill)
    {
        if (skill.currentEvolution == SkillEvolution.PathB)
        {
            return true;
        }
        return base.AlwaysHits(skill);
    }

    // ==========================================
    // Path C rule.
    // ==========================================
    public override float GetDynamicCritRateBonus(SkillData skill, int consecutiveHits)
    {
        if (skill.currentEvolution == SkillEvolution.PathC)
        {
            return -skill.GetCurrentBonusCritRate();
        }
        return base.GetDynamicCritRateBonus(skill, consecutiveHits);
    }

    // ==========================================
    // Path C rule.
    // ==========================================
    public override void ApplyEffectOnHit(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking, bool isHit)
    {
        // Buff/debuff rule.
        if (!isHit) return;

        if (skill.currentEvolution == SkillEvolution.PathC)
        {
            // Buff/debuff rule.
            var targetEffects = BuffManager.Instance.GetEffects(!isPlayerAttacking);

            int removedCount = targetEffects.RemoveAll(e => e.effectData.category == EffectCategory.Buff);

            if (removedCount > 0)
            {
                DevLog.Log($"[진화 C] 에누마 엘리시 발동! 적의 이로운 효과 {removedCount}개를 산산조각 냈습니다!");

                // HP cost/recovery rule.
                if (CombatUIManager.Instance != null) CombatUIManager.Instance.RefreshBuffUI();
            }
        }
    }
}