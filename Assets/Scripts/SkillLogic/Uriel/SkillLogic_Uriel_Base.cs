using UnityEngine;

public class SkillLogic_Uriel_Base : SkillLogicBase
{
    protected EnemyAI_Uriel GetUrielAI(EnemyData enemy)
    {
        return enemy != null ? enemy.aiBrain as EnemyAI_Uriel : null;
    }

    protected void AddEndurance(EnemyData enemy, int amount = 1)
    {
        GetUrielAI(enemy)?.AddEnduranceStack(amount);
    }

    protected int GetUrielEffectiveDefense(EnemyData enemy)
    {
        if (StatManager.Instance != null)
            return StatManager.Instance.GetEffectiveStat(false, TargetStat.Defense);

        return enemy != null ? enemy.defense : 0;
    }
}
