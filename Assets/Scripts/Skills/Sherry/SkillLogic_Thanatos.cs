using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Thanatos", menuName = "SkillLogic/Player/Thanatos")]
public class SkillLogic_Thanatos : SkillLogicBase
{
    [Header("디버프/버프 데이터")]
    public StatusEffectData defDownDebuff; // 적 방어력 감소
    public StatusEffectData defUpBuff;     // [진화 A] 내 방어력 흡수(증가)

    [Header("레벨별 방어력 감소율")]
    public float[] defDownRates = { -0.10f, -0.15f, -0.20f };

    [Header("진화 B (Burn My Dread) 설정")]
    [Tooltip("기본 데미지 배율 (예: 0.5 = 50%로 감소)")]
    public float pathB_BaseMult = 0.5f;
    [Tooltip("게이지 100%일 때 추가되는 최대 증폭량 (예: 1.5면 최대 2.0배)")]
    public float pathB_MaxBonus = 1.5f;

    // [진화 C] 타격 횟수를 8타로 변경합니다.
    public override int GetHitCount(SkillData skill)
    {
        if (skill.currentEvolution == SkillEvolution.PathC) return 8;
        return base.GetHitCount(skill);
    }

    public override float GetDamageMultiplier(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        if (skill.currentEvolution == SkillEvolution.PathB)
        {
            // [진화 B] 적의 현재 그로기 게이지 비례 폭딜
            float targetGauge = BreakManager.Instance.GetBreakGauge(!isPlayerAttacking);
            float bonus = (targetGauge / 100f) * pathB_MaxBonus;
            return pathB_BaseMult + bonus;
        }
        else if (skill.currentEvolution == SkillEvolution.PathC)
        {
            // [진화 C] 8타로 나뉘므로 데미지 계수를 8등분 합니다.
            return 1.0f / 8.0f;
        }
        return 1.0f; // 일반 상태 또는 진화 A
    }

    public override float GetBreakMultiplier(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        if (skill.currentEvolution == SkillEvolution.PathC)
        {
            // [진화 C] 8타로 나뉘므로 브레이크 수치도 8등분 합니다.
            return 1.0f / 8.0f;
        }
        return 1.0f;
    }

    // 진화 C (보름달 아래의 삶)일 때 다단히트 페널티로 명중률 80% 적용
    public override float GetBaseAccuracy(SkillData skill)
    {
        if (skill.currentEvolution == SkillEvolution.PathC)
        {
            return 80f; // 진화 C (8타 다단히트)일 때 강제로 명중률 80 반환
        }
        return base.GetBaseAccuracy(skill);
    }

    // [수정됨] 적중(isHit) 시에만 적용되도록 오버라이드
    public override void ApplyEffectOnHit(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking, bool isHit)
    {
        // 1. 공격이 완전히 빗나갔다면 디버프를 걸지 않고 종료합니다.
        if (!isHit)
        {
            DevLog.Log("[스킬 효과] 타나토스가 빗나가 방어력 감소 효과가 적용되지 않았습니다.");
            return;
        }

        if (isPlayerAttacking && defDownDebuff != null)
        {
            int index = Mathf.Clamp(skill.skillLevel - 1, 0, defDownRates.Length - 1);
            float rate = defDownRates[index];

            // 2. 적에게 방어력 감소 디버프 3턴 부여
            // (진화 C의 다단 히트여도 여기서 1번만 실행되므로 절대 중첩되지 않습니다!)
            BuffManager.Instance.AddEffect(false, defDownDebuff, rate, 3);
            DevLog.Log($"[스킬 효과] 타나토스 적중! 적의 방어력이 3턴간 {Mathf.Abs(rate * 100)}% 감소합니다.");

            // 3. [진화 A] 밤을 물들여라 - 깎아낸 수치만큼 내 방어력 상승
            if (skill.currentEvolution == SkillEvolution.PathA && defUpBuff != null)
            {
                // [핵심] 적의 '베이스 방어력'에서 실제로 깎여나간 절대 수치를 계산합니다.
                // 예: 적 방어력 500 * 0.20 = 100
                float actualReductionValue = enemy.defense * Mathf.Abs(rate);

                // 계산된 '100'이라는 수치를 내 방어력에 상수로 더해줍니다.
                // (주의: 유니티 인스펙터에서 defUpBuff의 Modifier Type을 'Flat'으로 설정해야 합니다!)
                BuffManager.Instance.AddEffect(true, defUpBuff, actualReductionValue, 3);

                DevLog.Log($"[진화 효과] 밤을 물들여라! 적의 방어력을 {actualReductionValue:F1}만큼 흡수하여 내 방어력이 상승합니다.");
            }
        }
    }
}