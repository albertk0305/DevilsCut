using UnityEngine;

public class SkillLogic_Cynd_Base : SkillLogicBase
{
    protected EnemyAI_Cynd GetCyndAI(EnemyData enemy)
    {
        return enemy != null ? enemy.aiBrain as EnemyAI_Cynd : null;
    }
}
