using UnityEngine;

public class SkillLogic_Raguel_Base : SkillLogicBase
{
    protected EnemyAI_Raguel GetRaguelAI(EnemyData enemy)
    {
        return enemy != null ? enemy.aiBrain as EnemyAI_Raguel : null;
    }

    public override float GetDamageMultiplier(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        return 1.0f;
    }
}
