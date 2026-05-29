using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Hibiki_Masshiro", menuName = "SkillLogic/Hibiki/Masshiro")]
public class SkillLogic_Hibiki_Masshiro : SkillLogicBase
{
    public override float GetArmorPenetrationRatio(SkillData skill, int skillLevel)
    {
        return 0.20f;
    }
}
