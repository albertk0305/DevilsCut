using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Arete_RememberTheTime", menuName = "SkillLogic/Arete/Remember the Time")]
public class SkillLogic_Arete_RememberTheTime : SkillLogicBase
{
    public override void ApplyEffect(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        if (isPlayerAttacking) return;
        if (TurnManager.Instance == null) return;

        TurnManager.Instance.SetGauge(EntityType.Enemy, 100f);
    }
}
