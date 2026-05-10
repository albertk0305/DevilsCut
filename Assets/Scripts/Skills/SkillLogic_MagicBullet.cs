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
    public StatusEffectData speedDownDebuff; // 속도 감소 디버프 연결
    [Tooltip("레벨별 속도 감소율 (제안: 20%, 25%, 30%)")]
    public float[] pathB_SpeedDownRates = { -0.20f, -0.25f, -0.30f };

    [Header("진화 C (Tiro Finale)")]
    [Tooltip("단타 압축 시 제공되는 방어 무시 비율")]
    public float pathC_ArmorPenetration = 0.30f; // 30% 방관

    // ==========================================
    // 타수 & 명중률 조작 (진화 C)
    // ==========================================
    public override int GetHitCount(SkillData skill)
    {
        if (skill.currentEvolution == SkillEvolution.PathC) return 1; // 단타 압축!
        return base.GetHitCount(skill);
    }

    public override float GetBaseAccuracy(SkillData skill)
    {
        if (skill.currentEvolution == SkillEvolution.PathC) return 90f; // 단타는 90%로 복구
        return 80f; // 다단히트는 80% 페널티 유지
    }

    // ==========================================
    // 실시간 다이내믹 보정 (진화 A)
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
    // 압축 데미지 및 방어 무시 (진화 C)
    // ==========================================
    public override float GetDamageMultiplier(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        if (skill.currentEvolution == SkillEvolution.PathC)
            return skill.GetCurrentHitCount(); // 원래 타수(4,6,8)만큼 데미지 뻥튀기!
        return 1.0f;
    }

    public override float GetBreakMultiplier(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        if (skill.currentEvolution == SkillEvolution.PathC)
            return skill.GetCurrentHitCount(); // 브레이크 수치도 타수만큼 뻥튀기!
        return 1.0f;
    }

    public override float GetArmorPenetrationRatio(SkillData skill, int skillLevel)
    {
        if (skill.currentEvolution == SkillEvolution.PathC) return pathC_ArmorPenetration;
        return base.GetArmorPenetrationRatio(skill, skillLevel);
    }

    // ==========================================
    // 디버프 부여 (진화 B)
    // ==========================================
    public override void ApplyEffectOnHit(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking, bool isHit)
    {
        if (!isHit) return; // 빗나가면 속도 감소 부여 안 함!

        if (skill.currentEvolution == SkillEvolution.PathB && isPlayerAttacking && speedDownDebuff != null)
        {
            int index = Mathf.Clamp(skill.skillLevel - 1, 0, pathB_SpeedDownRates.Length - 1);
            float rate = pathB_SpeedDownRates[index];

            BuffManager.Instance.AddEffect(false, speedDownDebuff, rate, 3);
            DevLog.Log($"[진화 B] 마기아 발동! 적의 속도가 3턴간 {Mathf.Abs(rate * 100)}% 감소합니다.");
        }
    }
}