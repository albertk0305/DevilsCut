using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Pati_PlasticLove", menuName = "SkillLogic/Pati/Plastic Love")]
public class SkillLogic_Pati_PlasticLove : SkillLogicBase
{
    public override bool TryOverrideBaseHitCalculation(
        SkillData skill,
        int attackerStrength,
        int attackerDefense,
        out float calculatedDamage,
        out float breakPower)
    {
        calculatedDamage = attackerStrength * 1f + attackerDefense * 1f;
        breakPower = 2f;
        return true;
    }
}
