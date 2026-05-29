using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Caritas_FlydayChinatown", menuName = "SkillLogic/Caritas/Flyday Chinatown")]
public class SkillLogic_Caritas_FlydayChinatown : SkillLogic_Caritas_Base
{
    public override float GetSkillBonusLifesteal(SkillData skill)
    {
        return 1.0f;
    }
}
