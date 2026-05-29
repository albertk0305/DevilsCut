using UnityEngine;

public class SkillLogic_Diligere_Base : SkillLogicBase
{
    protected int GetDiligereStrength(EnemyData enemy)
    {
        if (StatManager.Instance != null)
            return StatManager.Instance.GetEffectiveStat(false, TargetStat.Strength);

        return enemy != null ? enemy.strength : 0;
    }
}
