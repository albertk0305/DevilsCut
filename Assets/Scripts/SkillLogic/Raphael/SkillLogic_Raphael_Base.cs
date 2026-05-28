using UnityEngine;

public class SkillLogic_Raphael_Base : SkillLogicBase
{
    protected EnemyAI_Raphael GetRaphaelAI(EnemyData enemy)
    {
        return enemy != null ? enemy.aiBrain as EnemyAI_Raphael : null;
    }
}
