using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Uriel_CurtainCall", menuName = "SkillLogic/Uriel/Curtain Call")]
public class SkillLogic_Uriel_CurtainCall : SkillLogic_Uriel_Base
{
    public override bool TryOverrideBaseHitCalculation(
        SkillData skill,
        int attackerStrength,
        int attackerDefense,
        out float calculatedDamage,
        out float breakPower)
    {
        calculatedDamage = attackerStrength * 10f + attackerDefense * 20f;
        breakPower = 30f;
        return true;
    }

    public override void ApplyEffect(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        if (isPlayerAttacking) return;

        GetUrielAI(enemy)?.SpendCurtainCallStacks();
    }
}
