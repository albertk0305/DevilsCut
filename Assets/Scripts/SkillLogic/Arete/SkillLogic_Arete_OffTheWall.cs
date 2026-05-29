using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Arete_OffTheWall", menuName = "SkillLogic/Arete/Off the Wall")]
public class SkillLogic_Arete_OffTheWall : SkillLogicBase
{
    public override void ApplyEffectOnHit(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking, bool isHit)
    {
        if (!isHit) return;
        if (isPlayerAttacking) return;
        if (TurnManager.Instance == null) return;

        TurnManager.Instance.ResetGauge(EntityType.Player);
    }
}
