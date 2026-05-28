using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Cynd_Rise", menuName = "SkillLogic/Cynd/Rise")]
public class SkillLogic_Cynd_Rise : SkillLogic_Cynd_Base
{
    [SerializeField] private StatusEffectData speedDebuff;
    [SerializeField] private float speedDebuffValue = -0.25f;
    [SerializeField] private int speedDebuffTurns = 3;

    public override void ApplyEffectOnHit(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking, bool isHit)
    {
        if (!isHit) return;
        if (isPlayerAttacking) return;

        if (speedDebuff == null)
        {
            DevLog.LogWarning("[Rise] speedDebuff가 연결되지 않았습니다.");
            return;
        }

        BuffManager.Instance.AddEffect(true, speedDebuff, speedDebuffValue, speedDebuffTurns);
        DevLog.Log("[Rise] 셰리의 속도가 25% 감소했습니다.");
    }
}
