using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_WorldWithoutReason", menuName = "SkillLogic/Player/WorldWithoutReason")]
public class SkillLogic_WorldWithoutReason : SkillLogicBase
{
    [Header("기본: 레벨별 체력 회복률 (%)")]
    // Lv.1: 20%, Lv.2: 30%, Lv.3: 40%
    public float[] healRates = { 0.20f, 0.30f, 0.40f };

    [Header("기본: 레벨별 버스트(그로기) 감소량")]
    public float[] breakRecoveryAmounts = { 30f, 50f, 100f };

    [Header("진화 B: 초재생 버프")]
    public StatusEffectData hpRegenBuff;
    public StatusEffectData breakRegenBuff;

    [Header("진화 C: 구속제어술식 (회복량 -> 피해 전환율)")]
    // HP cost/recovery rule.
    public float[] pathC_DamageRates = { 0.5f, 0.75f, 1.0f };

    public override bool AlwaysHits(SkillData skill) => true;

    // Path C rule.
    public override int GetHitCount(SkillData skill)
    {
        return (skill.currentEvolution == SkillEvolution.PathC) ? 1 : 0;
    }

    // Path C rule.
    public override float GetDamageMultiplier(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        if (skill.currentEvolution == SkillEvolution.PathC) return 1.0f;
        return 0f;
    }

    public override void ApplyEffectOnHit(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking, bool isHit)
    {
        if (!isPlayerAttacking) return;

        int index = Mathf.Clamp(skill.skillLevel - 1, 0, healRates.Length - 1);

        // HP cost/recovery rule.
        float baseHeal = pStats.maxHp * healRates[index];
        // HP cost/recovery rule.
        int healAmount = Mathf.RoundToInt(baseHeal * (1f + pStats.healingReceivedAmp));

        int excessHeal = (pStats.currentHp + healAmount) - pStats.maxHp;
        pStats.currentHp = Mathf.Clamp(pStats.currentHp + healAmount, 0, pStats.maxHp);

        // HP cost/recovery rule.
        if (CombatUIManager.Instance != null)
        {
            CombatUIManager.Instance.playerStatusUI.UpdateHP(pStats.currentHp, pStats.maxHp);
            CombatUIManager.Instance.SpawnDamageText($"<color=#00FF00>+{healAmount}</color>", false, true);
        }

        if (excessHeal > 0 && CombatManager.Instance != null)
            CombatManager.Instance.ApplyOverhealBuff(excessHeal);

        // Break rule.
        float breakRecover = breakRecoveryAmounts[index];
        if (BreakManager.Instance != null)
        {
            BreakManager.Instance.RecoverBreakInstantly(true, breakRecover);
        }

        DevLog.Log($"[이성이 없는 세계] 체력 {healAmount} 회복, 그로기 수치 {breakRecover} 감소.");

        // ---------------------------------------------------------
        // Path A rule.
        // ---------------------------------------------------------
        if (skill.currentEvolution == SkillEvolution.PathA)
        {
            var effects = BuffManager.Instance.GetEffects(true);
            int removedCount = effects.RemoveAll(e => e.effectData.category == EffectCategory.Debuff);
            if (removedCount > 0)
            {
                CombatUIManager.Instance.RefreshBuffUI();
                DevLog.Log($"[진화 A] 샤인 발동! {removedCount}개의 치명적인 디버프를 즉시 정화했습니다.");
            }
        }

        // ---------------------------------------------------------
        // Path B rule.
        // ---------------------------------------------------------
        else if (skill.currentEvolution == SkillEvolution.PathB)
        {
            // HP cost/recovery rule.
            if (hpRegenBuff != null) BuffManager.Instance.AddEffect(true, hpRegenBuff, 0.1f, 3);

            // Break rule.
            if (breakRegenBuff != null)
            {
                float breakValue = pStats.maxBreakGauge * 0.1f;
                BuffManager.Instance.AddEffect(true, breakRegenBuff, breakValue, 3);
            }

            DevLog.Log("[진화 B] 초재생 발동! 3턴간 지속적인 생명력 및 그로기 회복 상태에 돌입합니다.");
        }

        // ---------------------------------------------------------
        // Path C rule.
        // ---------------------------------------------------------
        else if (skill.currentEvolution == SkillEvolution.PathC)
        {
            float damageRate = pathC_DamageRates[index];
            int reflectionDamage = Mathf.RoundToInt(healAmount * damageRate);

            // HP cost/recovery rule.
            CombatManager.Instance.ApplyDamageToEnemy(reflectionDamage);
            CombatUIManager.Instance.SpawnDamageText(reflectionDamage.ToString(), true, false);

            DevLog.Log($"[진화 C] 구속제어술식 발동! 셰리가 회복한 생명력의 {damageRate * 100}%를 적에게 치명적인 카운터 피해({reflectionDamage})로 되돌려줍니다!");
        }
    }
}