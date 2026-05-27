using UnityEngine;

public class SkillLogic_Humus_Base : SkillLogicBase
{
    public override float GetDamageMultiplier(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        if (isPlayerAttacking) return 1.0f;
        if (enemy == null) return 1.0f;

        enemy.aiBrain?.UpdatePassives(enemy);

        return 1.0f + enemy.damageGivenAmp;
    }
}
