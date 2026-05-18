using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Michael_ChainBury", menuName = "SkillLogic/Michael/ChainBury")]
public class SkillLogic_Michael_ChainBury : SkillLogic_Michael_Base
{
    public StatusEffectData speedDebuff; // 인스펙터에서 속도 감소 에셋 연결

    public override void ApplyEffectOnHit(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking, bool isHit)
    {
        if (!isHit) return; // 빗나갔으면 효과 없음

        // 명중 시 속도 감소 디버프 부여 (예: 수치 -0.25f, 3턴)
        if (speedDebuff != null)
        {
            BuffManager.Instance.AddEffect(true, speedDebuff, -0.25f, 3);
            DevLog.Log("[사슬 매장] 셰리의 속도가 감소합니다!");
        }
    }
}