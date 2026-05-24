using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_EmptyBox", menuName = "SkillLogic/Player/EmptyBox")]
public class SkillLogic_EmptyBox : SkillLogicBase
{
    [Header("적에게 부여할 효과 데이터")]
    public StatusEffectData enemyStrBuff;
    public StatusEffectData enemyDefDebuff;

    [Header("진화 A: 회피 버프")]
    public StatusEffectData evasionBuff;
    public float[] pathA_EvasionRates = { 0.50f, 0.60f, 0.75f };

    [Header("진화 B: 다음 턴 피해 증폭")]
    public StatusEffectData damageGivenAmpBuff;
    public float[] pathB_AmpRates = { 0.50f, 0.75f, 1.00f };

    [Header("진화 C: 1턴 가드 (스타일 보호)")]
    public StatusEffectData guardBuff;
    public float[] pathC_GuardRates = { 0.30f, 0.45f, 0.60f };

    [Header("레벨별 적 공격력 증가율 (%)")]
    public float[] strBuffRates = { 0f, 0.20f, 0.40f };

    [Header("레벨별 적 방어력 감소율 (%)")]
    public float[] defDebuffRates = { 0f, -0.10f, -0.20f };

    public override bool AlwaysHits(SkillData skill) => true;

    public override void ApplyEffect(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        if (!isPlayerAttacking) return;

        int index = Mathf.Clamp(skill.skillLevel - 1, 0, strBuffRates.Length - 1);

        if (StyleRankManager.Instance != null)
        {
            StyleRankManager.Instance.IncreaseRank(2);
        }

        // ---------------------------------------------------------
        // Path A rule.
        // ---------------------------------------------------------
        if (skill.currentEvolution == SkillEvolution.PathA && evasionBuff != null)
        {
            BuffManager.Instance.AddEffect(true, evasionBuff, pathA_EvasionRates[index], 1);
            DevLog.Log($"[진화 A] 빈 상자! 1턴간 회피율이 {pathA_EvasionRates[index] * 100}% 상승합니다.");
        }

        // ---------------------------------------------------------
        // Path B rule.
        // ---------------------------------------------------------
        else if (skill.currentEvolution == SkillEvolution.PathB && damageGivenAmpBuff != null)
        {
            BuffManager.Instance.AddEffect(true, damageGivenAmpBuff, pathB_AmpRates[index], 1);
            DevLog.Log($"[진화 B] 빈 상자! 다음 턴에 가하는 피해가 {pathB_AmpRates[index] * 100}% 증폭됩니다.");
        }

        // ---------------------------------------------------------
        // Path C rule.
        // ---------------------------------------------------------
        else if (skill.currentEvolution == SkillEvolution.PathC && guardBuff != null)
        {
            // Buff/debuff rule.
            BuffManager.Instance.AddEffect(true, guardBuff, pathC_GuardRates[index], 1);
            DevLog.Log($"[진화 C] 빈 상자! 1턴간 가드 상태가 되어 피해를 줄이고 스타일을 보호합니다.");
        }

        // Buff/debuff rule.
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