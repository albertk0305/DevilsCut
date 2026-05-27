using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Raguel_OverheatRecover", menuName = "SkillLogic/Raguel/OverheatRecover")]
public class SkillLogic_Raguel_OverheatRecover : SkillLogic_Raguel_Base
{
    public override bool AlwaysHits(SkillData skill) => true;

    public override float GetDamageMultiplier(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        return 0f;
    }

    public override void ApplyEffect(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        if (isPlayerAttacking) return;

        EnemyAI_Raguel raguelAI = GetRaguelAI(enemy);
        if (raguelAI != null)
        {
            raguelAI.RecoverOverheat(enemy);
        }
    }
}
