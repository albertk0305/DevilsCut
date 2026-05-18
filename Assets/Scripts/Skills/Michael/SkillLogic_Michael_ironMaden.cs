using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Michael_IronMaiden", menuName = "SkillLogic/Michael/IronMaiden")]
public class SkillLogic_Michael_IronMaiden : SkillLogic_Michael_Base
{
    public override float GetSkillBonusLifesteal(SkillData skill)
    {
        // ±âº» ±Ã±Ø±â ÈíÇ÷·ü 40% + ¹ÌÄ«¿¤ ÆĞ½Ãºê(ÀÒÀº Ã¼·Â ºñ·Ê) ÈíÇ÷·üÀ» ÇÕ»ê!
        return 0.40f + base.GetSkillBonusLifesteal(skill);
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
                    DevLog.Log("[Ã¶Ã³³à] ¼Î¸®ÀÇ Çàµ¿ °ÔÀÌÁö°¡ 40 Â÷°¨µÇ¾î ÅÏÀÌ ¹Ğ·Á³³´Ï´Ù!");
                    break;
                }
            }
        }
    }
}