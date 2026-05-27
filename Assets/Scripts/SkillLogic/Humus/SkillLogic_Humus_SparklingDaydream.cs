using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Humus_SparklingDaydream", menuName = "SkillLogic/Humus/SparklingDaydream")]
public class SkillLogic_Humus_SparklingDaydream : SkillLogic_Humus_Base
{
    [SerializeField] private StatusEffectData speedDebuff;
    [SerializeField] private float speedDebuffValue = -0.20f;
    [SerializeField] private int speedDebuffTurns = 3;

    public override void ApplyEffectOnHit(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking, bool isHit)
    {
        if (!isHit) return;
        if (isPlayerAttacking) return;

        if (speedDebuff == null)
        {
            DevLog.LogWarning("[Sparkling Daydream] speedDebuff가 연결되지 않았습니다.");
            return;
        }

        BuffManager.Instance.AddEffect(true, speedDebuff, speedDebuffValue, speedDebuffTurns);
        DevLog.Log("[Sparkling Daydream] 셰리의 속도가 20% 감소했습니다.");
    }
}
