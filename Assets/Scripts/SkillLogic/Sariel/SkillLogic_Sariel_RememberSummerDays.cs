using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Sariel_RememberSummerDays", menuName = "SkillLogic/Sariel/Remember Summer Days")]
public class SkillLogic_Sariel_RememberSummerDays : SkillLogic_Sariel_Base
{
    public override float GetSkillBonusLifesteal(SkillData skill)
    {
        return 1.0f;
    }
}
