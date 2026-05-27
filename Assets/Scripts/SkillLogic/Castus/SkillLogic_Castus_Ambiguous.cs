using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Castus_Ambiguous", menuName = "SkillLogic/Castus/Ambiguous")]
public class SkillLogic_Castus_Ambiguous : SkillLogic_Castus_Base
{
    [SerializeField] private StatusEffectData apDebuff;
    [SerializeField] private StatusEffectData speedDebuff;
    [SerializeField] private float apDebuffValue = -0.10f;
    [SerializeField] private float speedDebuffValue = -0.10f;
    [SerializeField] private int debuffTurns = 3;

    public override void ApplyEffectOnHit(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking, bool isHit)
    {
        if (!isHit) return;
        if (isPlayerAttacking) return;

        ApplyCastusPassiveOnHit(isPlayerAttacking, isHit);

        if (apDebuff != null)
        {
            BuffManager.Instance.AddEffect(true, apDebuff, apDebuffValue, debuffTurns);
        }
        else
        {
            DevLog.LogWarning("[Ambiguous] apDebuff가 연결되지 않았습니다.");
        }

        if (speedDebuff != null)
        {
            BuffManager.Instance.AddEffect(true, speedDebuff, speedDebuffValue, debuffTurns);
        }
        else
        {
            DevLog.LogWarning("[Ambiguous] speedDebuff가 연결되지 않았습니다.");
        }
    }
}
