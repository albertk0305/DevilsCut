using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Michael_ChainBury", menuName = "SkillLogic/Michael/ChainBury")]
public class SkillLogic_Michael_ChainBury : SkillLogic_Michael_Base
{
    public StatusEffectData speedDebuff;

    public override void ApplyEffectOnHit(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking, bool isHit)
    {
        if (!isHit) return;

        // Accuracy rule.
        if (speedDebuff != null)
        {
            BuffManager.Instance.AddEffect(true, speedDebuff, -0.25f, 3);
            DevLog.Log("[사슬 매장] 셰리의 속도가 감소합니다!");
        }
    }
}