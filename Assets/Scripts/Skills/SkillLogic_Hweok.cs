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
    public StatusEffectData pathA_StrengthFlatBuff; // 힘 고정치(Flat) 버프
    public float[] pathA_HpToStrengthRates = { 0.05f, 0.10f, 0.15f }; // 체력 100 잃었을 때 * 0.1 = 힘 10 증가
    public float[] pathA_BreakToStrengthRates = { 0.3f, 0.5f, 0.8f }; // 그로기 50 쌓였을 때 * 0.5 = 힘 25 증가

    [Header("진화 B: 무하한 (무적)")]
    public StatusEffectData pathB_InvincibleBuff;

    [Header("진화 C: 무량공처 (스턴)")]
    public StatusEffectData pathC_StunDebuff;

    public override bool AlwaysHits(SkillData skill) => true;

    // ApplyEffectOnHit 대신 기존처럼 즉발 적용인 ApplyEffect 사용 (필살기 및 생존기)
    public override void ApplyEffect(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        if (!isPlayerAttacking) return;

        int index = Mathf.Clamp(skill.skillLevel - 1, 0, statBuffRates.Length - 1);
        float buffValue = statBuffRates[index];

        // ---------------------------------------------------------
        // [진화 A] 반전술식: 회복 '전'의 잃은 체력과 그로기 수치를 미리 스냅샷!
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

        // 1. 전 스탯(공/방/속/운) 3턴 버프 부여
        if (strengthBuff != null) BuffManager.Instance.AddEffect(true, strengthBuff, buffValue, 3);
        if (defenseBuff != null) BuffManager.Instance.AddEffect(true, defenseBuff, buffValue, 3);
        if (speedBuff != null) BuffManager.Instance.AddEffect(true, speedBuff, buffValue, 3);
        if (luckBuff != null) BuffManager.Instance.AddEffect(true, luckBuff, buffValue, 3);

        // 2. 체력 회복 연산 및 UI 업데이트
        float baseHeal = pStats.maxHp * healRates[index];
        // [추가] 데몬 시너지 회복량 증폭
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

        // 3. 버스트(그로기) 게이지 감소 연산
        float breakRecover = breakRecoveryAmounts[index];
        if (BreakManager.Instance != null) BreakManager.Instance.RecoverBreakInstantly(true, breakRecover);

        DevLog.Log($"[스킬 효과] 회옥 발동! 전 스탯 {buffValue * 100}% 증가, 체력 {healAmount} 회복, 버스트 {breakRecover} 감소.");

        // ---------------------------------------------------------
        // [진화 B] 무하한: 999턴짜리 무적 버프 부여
        // ---------------------------------------------------------
        if (skill.currentEvolution == SkillEvolution.PathB && pathB_InvincibleBuff != null)
        {
            BuffManager.Instance.AddEffect(true, pathB_InvincibleBuff, 0, 999);
            DevLog.Log("[진화 B] 무하한 전개! 적의 다음 턴 공격을 완벽하게 무효화합니다.");
        }

        // ---------------------------------------------------------
        // [진화 C] 무량공처: 적에게 스턴 부여
        // ---------------------------------------------------------
        if (skill.currentEvolution == SkillEvolution.PathC && pathC_StunDebuff != null)
        {
            BuffManager.Instance.AddEffect(false, pathC_StunDebuff, 0, 1);
            DevLog.Log("[진화 C] 무량공처 전개! 끝없는 정보로 적의 정신을 붕괴시켜 다음 행동을 마비시킵니다.");
        }
    }
}