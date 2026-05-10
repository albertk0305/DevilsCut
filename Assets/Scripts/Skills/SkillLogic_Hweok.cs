using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Hweok", menuName = "SkillLogic/Player/Hweok")]
public class SkillLogic_Hweok : SkillLogicBase
{
    [Header("버프 데이터 (전 스탯)")]
    public StatusEffectData strengthBuff;
    public StatusEffectData defenseBuff;
    public StatusEffectData speedBuff;
    public StatusEffectData luckBuff;

    [Header("레벨별 버프 수치 (%)")]
    // Lv.1: 40%, Lv.2: 60%, Lv.3: 80%
    public float[] statBuffRates = { 0.40f, 0.60f, 0.80f };

    [Header("레벨별 체력 회복률 (%)")]
    // Lv.1: 50%, Lv.2: 75%, Lv.3: 100%
    public float[] healRates = { 0.50f, 0.75f, 1.0f };

    [Header("레벨별 버스트(그로기) 감소량")]
    // Lv.1: 50, Lv.2: 80, Lv.3: 100
    public float[] breakRecoveryAmounts = { 50f, 80f, 100f };

    // 요술 계열 필살기이므로 무조건 발동합니다.
    public override bool AlwaysHits() => true;

    public override void ApplyEffect(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        // 셰리(플레이어)가 사용했을 때만 작동하도록 합니다.
        if (isPlayerAttacking)
        {
            int index = Mathf.Clamp(skill.skillLevel - 1, 0, statBuffRates.Length - 1);

            // 1. 전 스탯(공/방/속/운) 3턴 버프 부여
            float buffValue = statBuffRates[index];
            if (strengthBuff != null) BuffManager.Instance.AddEffect(true, strengthBuff, buffValue, 3);
            if (defenseBuff != null) BuffManager.Instance.AddEffect(true, defenseBuff, buffValue, 3);
            if (speedBuff != null) BuffManager.Instance.AddEffect(true, speedBuff, buffValue, 3);
            if (luckBuff != null) BuffManager.Instance.AddEffect(true, luckBuff, buffValue, 3);

            // 2. 체력 회복 연산 및 UI 업데이트
            int healAmount = Mathf.RoundToInt(pStats.maxHp * healRates[index]);
            pStats.currentHp = Mathf.Clamp(pStats.currentHp + healAmount, 0, pStats.maxHp);

            if (CombatUIManager.Instance != null)
            {
                CombatUIManager.Instance.playerStatusUI.UpdateHP(pStats.currentHp, pStats.maxHp);
                CombatUIManager.Instance.SpawnDamageText($"<color=#00FF00>+{healAmount}</color>", false, true);
            }

            // 3. 버스트(그로기) 게이지 감소 연산
            float breakRecover = breakRecoveryAmounts[index];
            if (BreakManager.Instance != null)
            {
                BreakManager.Instance.RecoverBreakInstantly(true, breakRecover);
            }

            DevLog.Log($"[스킬 효과] 회옥 발동! 전 스탯 {buffValue * 100}% 증가, 체력 {healAmount} 회복, 버스트 {breakRecover} 감소.");

            // [추후 진화 기믹 예시]
            // - 반전술식: 잃은 체력에 비례하여 공격력 추가 폭증 (기획서 반영 시) [cite: 838, 839]
            // - 무하한: 1턴간 완전 무적 (데미지 무시 플래그 추가) [cite: 842, 843]
        }
    }
}