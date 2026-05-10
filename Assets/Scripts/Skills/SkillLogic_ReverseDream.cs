using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_ReverseDream", menuName = "SkillLogic/Player/ReverseDream")]
public class SkillLogic_ReverseDream : SkillLogicBase
{
    [Header("레벨별 흡혈 비율 (%)")]
    // Lv.1: 20%, Lv.2: 30%, Lv.3: 40%
    public float[] lifestealRates = { 0.20f, 0.30f, 0.40f };

    [Header("최대 데미지 증폭치 (별과 당신(1.5)보다 높은 2.0)")]
    public float maxDamageBonus = 2.0f; // 최대 3배 증폭

    public override float GetDamageMultiplier(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        if (isPlayerAttacking)
        {
            return CombatMath.GetMissingHPMultiplier(pStats.maxHp, pStats.currentHp, maxDamageBonus);
        }
        return 1.0f;
    }

    public override void ApplyEffect(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        if (isPlayerAttacking)
        {
            int index = Mathf.Clamp(skill.skillLevel - 1, 0, lifestealRates.Length - 1);
            float lifestealRate = lifestealRates[index];

            // 1. 가해질 데미지를 역산하여 흡혈량을 계산 (기본 계수 * 증폭 계수)
            float skillMultiplier = skill.GetCurrentDamageMultiplier() * GetDamageMultiplier(skill, pStats, enemy, isPlayerAttacking);

            // 방어자의 실시간 방어력을 가져와 데미지 감소율(DR) 적용
            int defenderDef = StatManager.Instance.GetEffectiveStat(false, TargetStat.Defense);
            float expectedDamage = (pStats.strength * skillMultiplier) * (1f - CombatMath.GetDamageReduction(defenderDef));

            // 2. 최종 회복량 산출
            int healAmount = Mathf.RoundToInt(expectedDamage * lifestealRate);

            // 3. 체력 회복 적용 (최대 체력을 넘지 않도록 Clamp)
            pStats.currentHp = Mathf.Clamp(pStats.currentHp + healAmount, 0, pStats.maxHp);

            // 4. UI 연출: 초록색 텍스트로 회복량 표시
            if (CombatUIManager.Instance != null)
            {
                CombatUIManager.Instance.playerStatusUI.UpdateHP(pStats.currentHp, pStats.maxHp);
                CombatUIManager.Instance.SpawnDamageText($"<color=#00FF00>+{healAmount}</color>", false, true);
            }

            DevLog.Log($"[스킬 효과] 역몽 발동! 딜 계수: {skillMultiplier:F1} / {healAmount} 체력 흡혈 완료.");
        }
    }
}