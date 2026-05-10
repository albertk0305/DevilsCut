using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_DemonSlaughter", menuName = "SkillLogic/Player/DemonSlaughter")]
public class SkillLogic_DemonSlaughter : SkillLogicBase
{
    [Header("기본: 레벨별 방어 무시 비율")]
    public float[] armorPenetrationRates = { 0.15f, 0.20f, 0.25f };

    [Header("진화 A (Transparent World)")]
    [Tooltip("적 최대 체력 비례 추가 피해량 (3%, 4%, 5%)")]
    public float[] pathA_MaxHpRates = { 0.03f, 0.04f, 0.05f };

    [Header("진화 B (Become a Demon)")]
    [Tooltip("입힌 피해 비례 흡혈률 (10%, 15%, 20%)")]
    public float[] pathB_LifestealRates = { 0.10f, 0.15f, 0.20f };

    [Header("진화 C (Opening Thread)")]
    [Tooltip("적에게 걸린 디버프 1개당 피해 증폭률 (10%, 15%, 20%)")]
    public float[] pathC_BonusPerDebuff = { 0.10f, 0.15f, 0.20f };

    public override float GetArmorPenetrationRatio(SkillData skill, int skillLevel)
    {
        int index = Mathf.Clamp(skillLevel - 1, 0, armorPenetrationRates.Length - 1);
        return armorPenetrationRates[index];
    }

    public override float GetDamageMultiplier(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        int index = Mathf.Clamp(skill.skillLevel - 1, 0, 2);
        float multiplier = 1.0f;

        if (skill.currentEvolution == SkillEvolution.PathA && isPlayerAttacking)
        {
            // [진화 A] 고정 데미지 치환 트릭
            // CombatManager의 private HP를 직접 건드리지 않고, 적의 최대 체력에 비례한 '고정 수치'를 
            // 내 깡딜에 대비한 '배율(Multiplier)'로 환산하여 얹어줍니다! (방어무시 효과도 자동으로 받음)
            float maxHpDamage = enemy.maxHp * pathA_MaxHpRates[index];
            float myBaseDamage = pStats.strength * skill.GetCurrentDamageMultiplier();

            if (myBaseDamage > 0)
            {
                multiplier += (maxHpDamage / myBaseDamage);
            }
        }
        else if (skill.currentEvolution == SkillEvolution.PathC && isPlayerAttacking)
        {
            // [진화 C] 적에게 걸린 디버프 개수 카운트
            int debuffCount = 0;

            // BuffManager에서 적군(false)의 상태 이상 리스트를 가져와서 'Debuff' 카테고리만 셉니다.
            var enemyEffects = BuffManager.Instance.GetEffects(false);
            foreach (var eff in enemyEffects)
            {
                if (eff.effectData.category == EffectCategory.Debuff)
                    debuffCount++;
            }

            multiplier += (debuffCount * pathC_BonusPerDebuff[index]);

            if (debuffCount > 0)
                DevLog.Log($"[진화 C] 빈틈의 실: 디버프 {debuffCount}개 감지! 데미지 {debuffCount * pathC_BonusPerDebuff[index] * 100}% 증폭!");
        }

        return multiplier;
    }

    public override void ApplyEffectOnHit(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking, bool isHit)
    {
        // 타격이 적중했을 때만 흡혈 발동
        if (!isHit) return;

        // [진화 B] 유지력 강화 (흡혈)
        if (skill.currentEvolution == SkillEvolution.PathB && isPlayerAttacking)
        {
            int index = Mathf.Clamp(skill.skillLevel - 1, 0, pathB_LifestealRates.Length - 1);
            float lifestealRate = pathB_LifestealRates[index];

            // 1. 역몽에서 썼던 '예상 데미지 역산' 공식을 사용해 방어 무시가 적용된 찐 데미지를 구합니다.
            float skillMult = skill.GetCurrentDamageMultiplier() * GetDamageMultiplier(skill, pStats, enemy, isPlayerAttacking);
            int defenderDef = StatManager.Instance.GetEffectiveStat(false, TargetStat.Defense);

            float penRatio = GetArmorPenetrationRatio(skill, skill.skillLevel);
            // 방어 무시가 적용된 실제 피해 감소율 계산
            float drPercent = CombatMath.GetDamageReduction(defenderDef) * (1f - penRatio);

            float expectedDamage = (pStats.strength * skillMult) * (1f - drPercent);

            // 2. 체력 흡수 연산
            int healAmount = Mathf.RoundToInt(expectedDamage * lifestealRate);
            pStats.currentHp = Mathf.Clamp(pStats.currentHp + healAmount, 0, pStats.maxHp);

            // 3. UI 연출
            if (CombatUIManager.Instance != null)
            {
                CombatUIManager.Instance.playerStatusUI.UpdateHP(pStats.currentHp, pStats.maxHp);
                CombatUIManager.Instance.SpawnDamageText($"<color=#00FF00>+{healAmount}</color>", false, true);
            }

            DevLog.Log($"[진화 B] 오니가 되어라 발동! {healAmount} 체력 흡수 완료.");
        }
    }
}