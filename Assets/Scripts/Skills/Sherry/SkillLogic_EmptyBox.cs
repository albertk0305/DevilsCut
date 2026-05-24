using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_EmptyBox", menuName = "SkillLogic/Player/EmptyBox")]
public class SkillLogic_EmptyBox : SkillLogicBase
{
    [Header("적에게 부여할 효과 데이터")]
    public StatusEffectData enemyStrBuff; // 적 공격력 증가 (광폭화)
    public StatusEffectData enemyDefDebuff; // 적 방어력 감소 (방어 허점)

    [Header("진화 A: 회피 버프")]
    public StatusEffectData evasionBuff;
    public float[] pathA_EvasionRates = { 0.50f, 0.60f, 0.75f };

    [Header("진화 B: 다음 턴 피해 증폭")]
    // 변수명을 명확하게 DamageGivenAmp로 변경했습니다.
    public StatusEffectData damageGivenAmpBuff;
    public float[] pathB_AmpRates = { 0.50f, 0.75f, 1.00f };

    [Header("진화 C: 1턴 가드 (스타일 보호)")]
    public StatusEffectData guardBuff;
    public float[] pathC_GuardRates = { 0.30f, 0.45f, 0.60f };

    [Header("레벨별 적 공격력 증가율 (%)")]
    public float[] strBuffRates = { 0f, 0.20f, 0.40f };

    [Header("레벨별 적 방어력 감소율 (%)")]
    public float[] defDebuffRates = { 0f, -0.10f, -0.20f };

    // 도발 스킬이므로 빗나가지 않고 무조건 적중합니다.
    public override bool AlwaysHits(SkillData skill) => true;

    public override void ApplyEffect(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        if (!isPlayerAttacking) return;

        int index = Mathf.Clamp(skill.skillLevel - 1, 0, strBuffRates.Length - 1);

        // 1. 스타일 랭크 즉시 3단계 상승 (공통 효과)
        if (StyleRankManager.Instance != null)
        {
            StyleRankManager.Instance.IncreaseRank(2); // D -> B 근처로 펌핑
        }

        // ---------------------------------------------------------
        // [진화 A] 누구도 될 수 없는 나니까: 1턴간 대폭 회피
        // ---------------------------------------------------------
        if (skill.currentEvolution == SkillEvolution.PathA && evasionBuff != null)
        {
            BuffManager.Instance.AddEffect(true, evasionBuff, pathA_EvasionRates[index], 1);
            DevLog.Log($"[진화 A] 빈 상자! 1턴간 회피율이 {pathA_EvasionRates[index] * 100}% 상승합니다.");
        }

        // ---------------------------------------------------------
        // [진화 B] 공백과 카타르시스: 다음 턴 피해 증폭 (DamageGivenAmp)
        // ---------------------------------------------------------
        // 수정됨: damageGivenAmpBuff를 사용하도록 변경
        else if (skill.currentEvolution == SkillEvolution.PathB && damageGivenAmpBuff != null)
        {
            BuffManager.Instance.AddEffect(true, damageGivenAmpBuff, pathB_AmpRates[index], 1);
            DevLog.Log($"[진화 B] 빈 상자! 다음 턴에 가하는 피해가 {pathB_AmpRates[index] * 100}% 증폭됩니다.");
        }

        // ---------------------------------------------------------
        // [진화 C] 운명의 꽃: 1턴 가드 부여 (피격 시 스타일 감소 방어)
        // ---------------------------------------------------------
        else if (skill.currentEvolution == SkillEvolution.PathC && guardBuff != null)
        {
            // 가드 버프는 시스템적으로 OnPlayerHit 시 스타일 랭크 하락을 막아줍니다.
            BuffManager.Instance.AddEffect(true, guardBuff, pathC_GuardRates[index], 1);
            DevLog.Log($"[진화 C] 빈 상자! 1턴간 가드 상태가 되어 피해를 줄이고 스타일을 보호합니다.");
        }

        // 2. 적에게 디버프(및 광폭화 버프) 부여 (Lv.2부터 작동)
        if (strBuffRates[index] > 0f && enemyStrBuff != null)
        {
            BuffManager.Instance.AddEffect(false, enemyStrBuff, strBuffRates[index], 3);
        }

        if (defDebuffRates[index] < 0f && enemyDefDebuff != null)
        {
            BuffManager.Instance.AddEffect(false, enemyDefDebuff, defDebuffRates[index], 3);
        }

        DevLog.Log($"[스킬 효과] 빈 상자 발동! 스타일 랭크 상승 및 진화 효과 적용.");
    }
}