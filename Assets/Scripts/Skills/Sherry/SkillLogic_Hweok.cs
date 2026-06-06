using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Hweok", menuName = "SkillLogic/Player/Hweok")]
public class SkillLogic_Hweok : SkillLogicBase
{
    [Header("기본: 버프 데이터 (전 스탯)")]
    public StatusEffectData strengthBuff;
    public StatusEffectData defenseBuff;
    public StatusEffectData speedBuff;
    public StatusEffectData luckBuff;

    [Header("기본: 레벨별 버프 수치 (%)")]
    public float[] statBuffRates = { 0.40f, 0.60f, 0.80f };

    [Header("기본: 레벨별 체력 회복률 (%)")]
    public float[] healRates = { 0.50f, 0.75f, 1.0f };

    [Header("기본: 레벨별 버스트(그로기) 감소량")]
    public float[] breakRecoveryAmounts = { 50f, 80f, 100f };

    [Header("진화 A: 반전술식 (잃은 체력/그로기 비례 힘 증가)")]
    public StatusEffectData pathA_StrengthFlatBuff;
    public float[] pathA_HpToStrengthRates = { 0.05f, 0.10f, 0.15f };
    public float[] pathA_BreakToStrengthRates = { 0.3f, 0.5f, 0.8f };

    [Header("진화 B: 무하한 (무적)")]
    public StatusEffectData pathB_InvincibleBuff;

    [Header("진화 C: 무량공처 (스턴)")]
    public StatusEffectData pathC_StunDebuff;

    public override bool AlwaysHits(SkillData skill) => true;

    // Turn gauge rule.
    public override void ApplyEffect(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        if (!isPlayerAttacking) return;

        int index = Mathf.Clamp(skill.skillLevel - 1, 0, statBuffRates.Length - 1);
        float buffValue = statBuffRates[index];

        // ---------------------------------------------------------
        // Path A rule.
        // ---------------------------------------------------------
        if (skill.currentEvolution == SkillEvolution.PathA)
        {
            int missingHp = pStats.maxHp - pStats.currentHp;
            float currentBreak = BreakManager.Instance.GetBreakGauge(true);

            float hpRate = pathA_HpToStrengthRates[index];
            float breakRate = pathA_BreakToStrengthRates[index];

            int bonusStr = Mathf.RoundToInt((missingHp * hpRate) + (currentBreak * breakRate));

            if (pathA_StrengthFlatBuff != null && bonusStr > 0)
            {
                BuffManager.Instance.AddEffect(true, pathA_StrengthFlatBuff, bonusStr, 3);
                DevLog.Log($"[진화 A] 반전술식! 잃은 체력({missingHp})과 버스트({currentBreak:F1})를 극한의 화력으로 치환하여 힘이 {bonusStr} 증가했습니다.");
            }
        }

        // Buff/debuff rule.
        if (strengthBuff != null) BuffManager.Instance.AddEffect(true, strengthBuff, buffValue, 3);
        if (defenseBuff != null) BuffManager.Instance.AddEffect(true, defenseBuff, buffValue, 3);
        if (speedBuff != null) BuffManager.Instance.AddEffect(true, speedBuff, buffValue, 3);
        if (luckBuff != null) BuffManager.Instance.AddEffect(true, luckBuff, buffValue, 3);

        // HP cost/recovery rule.
        float baseHeal = pStats.maxHp * healRates[index];
        // HP cost/recovery rule.
        int healAmount = Mathf.RoundToInt(baseHeal * (1f + pStats.healingReceivedAmp));

        int excessHeal = (pStats.currentHp + healAmount) - pStats.maxHp;
        pStats.currentHp = Mathf.Clamp(pStats.currentHp + healAmount, 0, pStats.maxHp);

        if (CombatUIManager.Instance != null)
        {
            CombatUIManager.Instance.playerStatusUI.UpdateHP(pStats.currentHp, pStats.maxHp);
            CombatUIManager.Instance.SpawnDamageText($"<color=#00FF00>+{healAmount}</color>", false, true);
        }

        if (excessHeal > 0 && CombatManager.Instance != null)
            CombatManager.Instance.ApplyOverhealBuff(excessHeal);

        // Break rule.
        float breakRecover = breakRecoveryAmounts[index];
        if (BreakManager.Instance != null) BreakManager.Instance.RecoverBreakInstantly(true, breakRecover);

        DevLog.Log($"[스킬 효과] 회옥 발동! 전 스탯 {buffValue * 100}% 증가, 체력 {healAmount} 회복, 버스트 {breakRecover} 감소.");

        // ---------------------------------------------------------
        // Path B rule.
        // ---------------------------------------------------------
        if (skill.currentEvolution == SkillEvolution.PathB && pathB_InvincibleBuff != null)
        {
            BuffManager.Instance.AddEffect(true, pathB_InvincibleBuff, 0, 999);
            DevLog.Log("[진화 B] 무하한 전개! 적의 다음 턴 공격을 완벽하게 무효화합니다.");
        }

        // ---------------------------------------------------------
        // Path C rule.
        // ---------------------------------------------------------
        if (skill.currentEvolution == SkillEvolution.PathC && pathC_StunDebuff != null)
        {
            BuffManager.Instance.AddEffect(false, pathC_StunDebuff, 0, 1);
            DevLog.Log($"[Stun Debug] Hweok applied stun to Enemy. effectName={pathC_StunDebuff.effectName}, specialType={pathC_StunDebuff.specialType}");
            DevLog.Log("[진화 C] 무량공처 전개! 끝없는 정보로 적의 정신을 붕괴시켜 다음 행동을 마비시킵니다.");
        }
    }
}
