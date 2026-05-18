using UnityEngine;

public class SkillLogic_Michael_Base : SkillLogicBase
{
    [Header("광폭화 상태 확인용 마커")]
    public StatusEffectData phase2MarkerBuff; // 광폭화 시 부여되는 아무 버프나 넣어서 상태를 확인합니다.

    // [패시브 1] 자학적 인과 (잃은 체력 비례 피해 증폭)
    public override float GetDamageMultiplier(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        // 미카엘의 공격일 때만 적용
        if (isPlayerAttacking) return 1.0f;

        float maxHp = enemy.maxHp;
        float currentHp = enemy.currentHp;
        float missingHpRatio = (maxHp - currentHp) / maxHp;

        // 기획 공식: 1.0 + (잃은 체력 비율 * 1.2)
        float passiveMultiplier = 1.0f + (missingHpRatio * 1.2f);

        return passiveMultiplier;
    }

    // [패시브 2] 광폭화 흡혈 (2페이즈 전용 잃은 체력 비례 피흡)
    public override float GetSkillBonusLifesteal(SkillData skill)
    {
        if (CombatManager.Instance == null) return 0f;
        EnemyData enemy = CombatManager.Instance.GetCurrentEnemyData();
        if (enemy == null) return 0f;

        // 적(미카엘)에게 2페이즈 마커 버프가 있는지 확인
        bool isEnraged = false;
        if (BuffManager.Instance != null && phase2MarkerBuff != null)
        {
            var eEffects = BuffManager.Instance.GetEffects(false); // false = 적
            isEnraged = eEffects.Exists(e => e.effectData == phase2MarkerBuff);
        }

        // 광폭화 상태라면 기획 공식 적용
        if (isEnraged)
        {
            float missingHpRatio = (enemy.maxHp - (float)enemy.currentHp) / enemy.maxHp;
            // 기획 공식: 기본 10% + (잃은 체력 비율 * 30%)
            return 0.10f + (missingHpRatio * 0.30f);
        }

        return 0f;
    }
}