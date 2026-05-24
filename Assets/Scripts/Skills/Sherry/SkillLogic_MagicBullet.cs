using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_MagicBullet", menuName = "SkillLogic/Player/MagicBullet")]
public class SkillLogic_MagicBullet : SkillLogicBase
{
    [Header("진화 A (Tiro Duet)")]
    [Tooltip("연속 적중 1스택당 오르는 데미지 배율 (0.15 = 15%)")]
    public float pathA_DamageBonusPerHit = 0.15f;
    [Tooltip("연속 적중 1스택당 오르는 크리티컬 확률 (15 = 15%)")]
    public float pathA_CritBonusPerHit = 15f;

    [Header("진화 B (Magia)")]
    public StatusEffectData speedDownDebuff;
    [Tooltip("레벨별 속도 감소율 (제안: 20%, 25%, 30%)")]
    public float[] pathB_SpeedDownRates = { -0.20f, -0.25f, -0.30f };

    [Header("진화 C (Tiro Finale)")]
    [Tooltip("단타 압축 시 제공되는 방어 무시 비율")]
    public float pathC_ArmorPenetration = 0.30f;

    // ==========================================
    // Path C rule.
    // ==========================================
    public override int GetHitCount(SkillData skill)
    {
        if (skill.currentEvolution == SkillEvolution.PathC) return 1;
        return base.GetHitCount(skill);
    }

    public override float GetBaseAccuracy(SkillData skill)
    {
        if (skill.currentEvolution == SkillEvolution.PathC) return 90f;
        return 80f;
    }

    // ==========================================
    // Path A rule.
    // ==========================================
    public override float GetDynamicDamageMultiplier(SkillData skill, int consecutiveHits)
    {
        if (skill.currentEvolution == SkillEvolution.PathA && consecutiveHits > 0)
        {
            return 1.0f + (consecutiveHits * pathA_DamageBonusPerHit);
        }
        return 1.0f;
    }

    public override float GetDynamicCritRateBonus(SkillData skill, int consecutiveHits)
    {
        if (skill.currentEvolution == SkillEvolution.PathA && consecutiveHits > 0)
        {
            return consecutiveHits * pathA_CritBonusPerHit;
        }
        return 0f;
    }

    // ==========================================
    // Path C rule.
    // ==========================================
    public override float GetDamageMultiplier(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        if (skill.currentEvolution == SkillEvolution.PathC)
            return skill.GetCurrentHitCount();
        return 1.0f;
    }

    public override float GetBreakMultiplier(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        if (skill.currentEvolution == SkillEvolution.PathC)
            return skill.GetCurrentHitCount();
        return 1.0f;
    }

    public override float GetArmorPenetrationRatio(SkillData skill, int skillLevel)
    {
        if (skill.currentEvolution == SkillEvolution.PathC) return pathC_ArmorPenetration;
        return base.GetArmorPenetrationRatio(skill, skillLevel);
    }

    // ==========================================
    // Path B rule.
    // ==========================================
    public override void ApplyEffectOnHit(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking, bool isHit)
    {
        if (!isHit) return;

        if (skill.currentEvolution == SkillEvolution.PathB && isPlayerAttacking && speedDownDebuff != null)
        {
            int index = Mathf.Clamp(skill.skillLevel - 1, 0, pathB_SpeedDownRates.Length - 1);
            float rate = pathB_SpeedDownRates[index];

            BuffManager.Instance.AddEffect(false, speedDownDebuff, rate, 3);
            DevLog.Log($"[진화 B] 마기아 발동! 적의 속도가 3턴간 {Mathf.Abs(rate * 100)}% 감소합니다.");
        }
    }
}