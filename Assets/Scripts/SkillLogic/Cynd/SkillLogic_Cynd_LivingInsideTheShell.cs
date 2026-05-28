using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Cynd_LivingInsideTheShell", menuName = "SkillLogic/Cynd/LivingInsideTheShell")]
public class SkillLogic_Cynd_LivingInsideTheShell : SkillLogic_Cynd_Base
{
    public override void ApplyEffect(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        if (isPlayerAttacking) return;

        EnemyAI_Cynd cyndAI = GetCyndAI(enemy);
        if (cyndAI != null)
        {
            cyndAI.EnterOverheat(enemy);
        }
    }
}
