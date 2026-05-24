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
            // Path A rule.
            // HP cost/recovery rule.
            float maxHpDamage = enemy.maxHp * pathA_MaxHpRates[index];
            float myBaseDamage = pStats.strength * skill.GetCurrentDamageMultiplier();

            if (myBaseDamage > 0)
            {
                multiplier += (maxHpDamage / myBaseDamage);
            }
        }
        else if (skill.currentEvolution == SkillEvolution.PathC && isPlayerAttacking)
        {
            // Path C rule.
            int debuffCount = 0;

            // Buff/debuff rule.
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
        // Lifesteal rule.
        if (!isHit) return;

        // Path B rule.
        if (skill.currentEvolution == SkillEvolution.PathB && isPlayerAttacking)
        {
            int index = Mathf.Clamp(skill.skillLevel - 1, 0, pathB_LifestealRates.Length - 1);
            float lifestealRate = pathB_LifestealRates[index];

            // Damage scaling rule.
            float skillMult = skill.GetCurrentDamageMultiplier() * GetDamageMultiplier(skill, pStats, enemy, isPlayerAttacking);
            int defenderDef = StatManager.Instance.GetEffectiveStat(false, TargetStat.Defense);

            float penRatio = GetArmorPenetrationRatio(skill, skill.skillLevel);
            // Damage scaling rule.
            float drPercent = CombatMath.GetDamageReduction(defenderDef) * (1f - penRatio);

            float expectedDamage = (pStats.strength * skillMult) * (1f - drPercent);

            // HP cost/recovery rule.
            int healAmount = Mathf.RoundToInt(expectedDamage * lifestealRate);
            pStats.currentHp = Mathf.Clamp(pStats.currentHp + healAmount, 0, pStats.maxHp);

            if (CombatUIManager.Instance != null)
            {
                CombatUIManager.Instance.playerStatusUI.UpdateHP(pStats.currentHp, pStats.maxHp);
                CombatUIManager.Instance.SpawnDamageText($"<color=#00FF00>+{healAmount}</color>", false, true);
            }

            DevLog.Log($"[진화 B] 오니가 되어라 발동! {healAmount} 체력 흡수 완료.");
        }
    }
}