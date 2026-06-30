using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Baito_GateOfSteiner", menuName = "SkillLogic/Baito/Gate of Steiner")]
public class SkillLogic_Baito_GateOfSteiner : SkillLogicBase
{
    [SerializeField] private int fixedDamage = 9999;

    public override bool AlwaysHits(SkillData skill) => true;

    public override bool TreatAsAttackSkill(SkillData skill)
    {
        return true;
    }

    public override bool TryOverrideBaseHitCalculation(
        SkillData skill,
        int attackerStrength,
        int attackerDefense,
        out float calculatedDamage,
        out float breakPower)
    {
        calculatedDamage = fixedDamage;
        breakPower = 0f;
        return true;
    }

    public override float GetArmorPenetrationRatio(SkillData skill, int skillLevel)
    {
        return 1f;
    }

    public override float GetDamageMultiplier(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        return 1f;
    }

    public override float GetBreakMultiplier(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        return 0f;
    }
}
