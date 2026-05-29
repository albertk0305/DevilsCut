using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Diligere_VagueHope", menuName = "SkillLogic/Diligere/Vague Hope")]
public class SkillLogic_Diligere_VagueHope : SkillLogic_Diligere_Base
{
    [SerializeField] private StatusEffectData apDebuff;
    [SerializeField] private float apDebuffValue = -0.20f;
    [SerializeField] private int debuffTurns = 3;

    public override void ApplyEffectOnHit(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking, bool isHit)
    {
        if (!isHit) return;
        if (isPlayerAttacking) return;

        if (apDebuff != null)
        {
            BuffManager.Instance.AddEffect(true, apDebuff, apDebuffValue, debuffTurns);
        }
        else
        {
            DevLog.LogWarning("[Vague Hope] apDebuff is not assigned.");
        }
    }
}
