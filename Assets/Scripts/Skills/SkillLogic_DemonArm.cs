using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_DemonArm", menuName = "SkillLogic/Player/DemonArm")]
public class SkillLogic_DemonArm : SkillLogicBase
{
    [Header("진화 A (Come the Light)")]
    public float pathA_CritMultiplier = 2.5f; // 기본 1.5배 -> 2.5배로 폭발적 상승

    [Header("진화 B (Stars)")]
    [Tooltip("크리 100% 초과분 1%당 상승할 추가 데미지 비율 (1.0 = 1%)")]
    public float pathB_OverflowConversionRate = 1.0f; // 밸런싱에 따라 조절

    // 진화 C 전용 내부 상태값 (0~5 사이의 빈 챔버 인덱스)
    private int currentEmptyChamberIndex = -1;

    // [진화 C] 타수 변환 (1타 -> 러시안 룰렛 타수)
    public override int GetHitCount(SkillData skill)
    {
        if (skill.currentEvolution == SkillEvolution.PathC)
        {
            // 빈 챔버(불발탄)가 터지면 그 즉시 쏘기를 멈춰야 합니다.
            // 예: 빈 챔버가 2번 인덱스(3번째 발)라면, 0, 1, 2까지 쏘고 멈추므로 총 3타를 때립니다.
            return currentEmptyChamberIndex + 1;
        }
        return base.GetHitCount(skill);
    }

    // [진화 C] 빈 챔버 굴리기
    public override void PaySkillCost(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        if (skill.currentEvolution == SkillEvolution.PathC && isPlayerAttacking)
        {
            // 스킬 시전 시 6발 중 1발의 빈 챔버(불발탄)를 장전합니다. (0 ~ 5)
            currentEmptyChamberIndex = Random.Range(0, 6);
            DevLog.Log($"[러시안 룰렛] 철컥! {currentEmptyChamberIndex + 1}번째 총알이 비어있습니다...");
        }
    }

    // [진화 C] 강제 불발탄 판정
    public override bool AlwaysMisses(SkillData skill, int hitIndex)
    {
        if (skill.currentEvolution == SkillEvolution.PathC)
        {
            // 지금 쏘는 총알(hitIndex)이 빈 챔버라면 무조건 빗나감(불발) 처리!
            return hitIndex == currentEmptyChamberIndex;
        }
        return false;
    }

    // [진화 A] 크리티컬 데미지 뻥튀기
    public override float GetCritDamageMultiplier(SkillData skill)
    {
        if (skill.currentEvolution == SkillEvolution.PathA) return pathA_CritMultiplier;
        return base.GetCritDamageMultiplier(skill);
    }

    // [진화 B, C] 데미지 계수 보정
    public override float GetDamageMultiplier(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        float multiplier = 1.0f;

        if (skill.currentEvolution == SkillEvolution.PathB && isPlayerAttacking)
        {
            // [진화 B] 크리티컬 오버플로우 연산
            float finalCritRate = CombatMath.GetFinalCritRate(skill.GetCurrentBonusCritRate(), pStats.luck);
            if (finalCritRate > 100f)
            {
                float overflow = finalCritRate - 100f;
                float bonus = (overflow * pathB_OverflowConversionRate) / 100f;
                multiplier += bonus;
                DevLog.Log($"[별 부스러기] 크리 확률 {overflow:F1}% 초과! 데미지가 {bonus * 100:F1}% 증폭됩니다!");
            }
        }
        else if (skill.currentEvolution == SkillEvolution.PathC)
        {
            // [진화 C] 6연발 스킬이므로 기본 데미지를 6등분 합니다.
            multiplier = 1.0f / 6.0f;
        }

        return multiplier;
    }
}