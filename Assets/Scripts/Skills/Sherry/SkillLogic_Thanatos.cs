using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Thanatos", menuName = "SkillLogic/Player/Thanatos")]
public class SkillLogic_Thanatos : SkillLogicBase
{
    [Header("디버프/버프 데이터")]
    public StatusEffectData defDownDebuff;
    public StatusEffectData defUpBuff;

    [Header("레벨별 방어력 감소율")]
    public float[] defDownRates = { -0.10f, -0.15f, -0.20f };

    [Header("진화 B (Burn My Dread) 설정")]
    [Tooltip("기본 데미지 배율 (예: 0.5 = 50%로 감소)")]
    public float pathB_BaseMult = 0.5f;
    [Tooltip("게이지 100%일 때 추가되는 최대 증폭량 (예: 1.5면 최대 2.0배)")]
    public float pathB_MaxBonus = 1.5f;

    // Path C rule.
    public override int GetHitCount(SkillData skill)
    {
        if (skill.currentEvolution == SkillEvolution.PathC) return 8;
        return base.GetHitCount(skill);
    }

    public override float GetDamageMultiplier(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        if (skill.currentEvolution == SkillEvolution.PathB)
        {
            // Path B rule.
            float targetGauge = BreakManager.Instance.GetBreakGauge(!isPlayerAttacking);
            float bonus = (targetGauge / 100f) * pathB_MaxBonus;
            return pathB_BaseMult + bonus;
        }
        else if (skill.currentEvolution == SkillEvolution.PathC)
        {
            // Path C rule.
            return 1.0f / 8.0f;
        }
        return 1.0f;
    }

    public override float GetBreakMultiplier(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        if (skill.currentEvolution == SkillEvolution.PathC)
        {
            // Path C rule.
            return 1.0f / 8.0f;
        }
        return 1.0f;
    }

    // Path C rule.
    public override float GetBaseAccuracy(SkillData skill)
    {
        if (skill.currentEvolution == SkillEvolution.PathC)
        {
            return 80f;
        }
        return base.GetBaseAccuracy(skill);
    }

    public override void ApplyEffectOnHit(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking, bool isHit)
    {
        // Buff/debuff rule.
        if (!isHit)
        {
            DevLog.Log("[스킬 효과] 타나토스가 빗나가 방어력 감소 효과가 적용되지 않았습니다.");
            return;
        }

        if (isPlayerAttacking && defDownDebuff != null)
        {
            int index = Mathf.Clamp(skill.skillLevel - 1, 0, defDownRates.Length - 1);
            float rate = defDownRates[index];

            // Buff/debuff rule.
            // Path C rule.
            BuffManager.Instance.AddEffect(false, defDownDebuff, rate, 3);
            DevLog.Log($"[스킬 효과] 타나토스 적중! 적의 방어력이 3턴간 {Mathf.Abs(rate * 100)}% 감소합니다.");

            // Path A rule.
            if (skill.currentEvolution == SkillEvolution.PathA && defUpBuff != null)
            {
                float actualReductionValue = enemy.defense * Mathf.Abs(rate);

                BuffManager.Instance.AddEffect(true, defUpBuff, actualReductionValue, 3);

                DevLog.Log($"[진화 효과] 밤을 물들여라! 적의 방어력을 {actualReductionValue:F1}만큼 흡수하여 내 방어력이 상승합니다.");
            }
        }
    }
}