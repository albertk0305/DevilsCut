using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Gabriel_Sirius", menuName = "SkillLogic/Gabriel/Sirius")]
public class SkillLogic_Gabriel_Sirius : SkillLogic_Gabriel_Base
{
    public override void ApplyEffectOnHit(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking, bool isHit)
    {
        if (!isHit) return;
        if (isPlayerAttacking) return;

        ApplyGabrielPassiveOnHit(isPlayerAttacking, isHit);
    }

    public override float GetArmorPenetrationRatio(SkillData skill, int skillLevel)
    {
        return 0.25f;
    }
}
