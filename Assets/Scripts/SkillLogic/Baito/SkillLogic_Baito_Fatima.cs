using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Baito_Fatima", menuName = "SkillLogic/Baito/Fatima")]
public class SkillLogic_Baito_Fatima : SkillLogicBase
{
    public override bool AlwaysCrits(SkillData skill)
    {
        return true;
    }
}
