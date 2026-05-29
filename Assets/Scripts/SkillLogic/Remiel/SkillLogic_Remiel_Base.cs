using UnityEngine;

public class SkillLogic_Remiel_Base : SkillLogicBase
{
    protected int GetRemielStrength(EnemyData enemy)
    {
        if (StatManager.Instance != null)
            return StatManager.Instance.GetEffectiveStat(false, TargetStat.Strength);

        return enemy != null ? enemy.strength : 0;
    }
}
