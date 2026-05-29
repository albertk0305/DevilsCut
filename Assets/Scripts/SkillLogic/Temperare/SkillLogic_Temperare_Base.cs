using UnityEngine;

public class SkillLogic_Temperare_Base : SkillLogicBase
{
    protected EnemyAI_Temperare GetTemperareAI(EnemyData enemy)
    {
        return enemy != null ? enemy.aiBrain as EnemyAI_Temperare : null;
    }
}
