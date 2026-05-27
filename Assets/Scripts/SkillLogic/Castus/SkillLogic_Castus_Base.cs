using UnityEngine;

public class SkillLogic_Castus_Base : SkillLogicBase
{
    [SerializeField] private StatusEffectData breakResistanceDebuff;
    [SerializeField] private float breakResistanceDebuffValue = -0.05f;
    [SerializeField] private int breakResistanceDebuffTurns = 3;

    protected void ApplyCastusPassiveOnHit(bool isPlayerAttacking, bool isHit)
    {
        if (!isHit) return;
        if (isPlayerAttacking) return;

        if (breakResistanceDebuff == null)
        {
            DevLog.LogWarning("[Castus Passive] breakResistanceDebuff가 연결되지 않았습니다.");
            return;
        }

        BuffManager.Instance.AddEffect(true, breakResistanceDebuff, breakResistanceDebuffValue, breakResistanceDebuffTurns);
        DevLog.Log("[Castus Passive] 셰리에게 BR 감소가 적용됐습니다.");
    }
}
