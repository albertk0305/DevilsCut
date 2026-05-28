using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Raphael_GoldenTimeRubber", menuName = "SkillLogic/Raphael/GoldenTimeRubber")]
public class SkillLogic_Raphael_GoldenTimeRubber : SkillLogic_Raphael_Base
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
            DevLog.LogWarning("[Golden Time Rubber] speedDebuff가 연결되지 않았습니다.");
            return;
        }

        BuffManager.Instance.AddEffect(true, speedDebuff, speedDebuffValue, speedDebuffTurns);
    }
}
