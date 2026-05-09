using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_MagicBullet", menuName = "SkillLogic/Player/MagicBullet")]
public class SkillLogic_MagicBullet : SkillLogicBase
{
    public override int GetHitCount(SkillData skill)
    {
        // [변이 C - 티로 피날레] 다단 히트를 버리고 1타 압축 폭딜로 변형!
        if (skill.currentEvolution == SkillEvolution.PathC)
        {
            return 1;
        }

        // 기본 상태이거나 다른 진화일 때는 정상적인 레벨별 타수(4, 6, 8) 반환
        return skill.GetCurrentHitCount();
    }

    public override void ApplyEffect(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        // [강화 B - 마기아] 적에게 3턴간 속도 감소 디버프를 확정 부여하는 로직이 들어갈 자리입니다.
    }
}