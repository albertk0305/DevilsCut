using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Arete_BeatIt", menuName = "SkillLogic/Arete/Beat It")]
public class SkillLogic_Arete_BeatIt : SkillLogicBase
{
    public override float GetArmorPenetrationRatio(SkillData skill, int skillLevel)
    {
        return 1.00f;
    }
}
