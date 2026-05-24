using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Michael_IronMaiden", menuName = "SkillLogic/Michael/IronMaiden")]
public class SkillLogic_Michael_IronMaiden : SkillLogic_Michael_Base
{
    public override float GetSkillBonusLifesteal(SkillData skill)
    {
        // Lifesteal rule.
        // Lifesteal rule.
        return 0.40f;
    }

    public override void ApplyEffectOnHit(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking, bool isHit)
    {
        if (!isHit) return;

        if (TurnManager.Instance != null)
        {
            foreach (var entity in TurnManager.Instance.turnQueue)
            {
                if (entity.type == EntityType.Player)
                {
                    entity.actionGauge -= 40f;
                    DevLog.Log("[철처녀] 셰리의 행동 게이지가 40 차감되어 턴이 밀려납니다!");
                    break;
                }
            }
        }
    }
}