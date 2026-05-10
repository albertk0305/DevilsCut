using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_EmptyBox", menuName = "SkillLogic/Player/EmptyBox")]
public class SkillLogic_EmptyBox : SkillLogicBase
{
    [Header("적에게 부여할 효과 데이터")]
    public StatusEffectData enemyStrBuff; // 적 공격력 증가 (광폭화)
    public StatusEffectData enemyDefDebuff; // 적 방어력 감소 (방어 허점)

    [Header("레벨별 적 공격력 증가율 (%)")]
    // Lv.1: 0%, Lv.2: 20%, Lv.3: 40%
    public float[] strBuffRates = { 0f, 0.20f, 0.40f };

    [Header("레벨별 적 방어력 감소율 (%)")]
    // Lv.1: 0%, Lv.2: -10%, Lv.3: -20%
    public float[] defDebuffRates = { 0f, -0.10f, -0.20f };

    // 도발 스킬이므로 빗나가지 않고 무조건 적중합니다.
    public override bool AlwaysHits() => true;

    public override void ApplyEffect(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        // 1. 스타일 랭크 즉시 3단계 상승
        if (StyleRankManager.Instance != null)
        {
            StyleRankManager.Instance.IncreaseRank(2);
        }

        // 2. 적에게 디버프(및 광폭화 버프) 부여 (Lv.2부터 작동)
        int index = Mathf.Clamp(skill.skillLevel - 1, 0, strBuffRates.Length - 1);

        if (strBuffRates[index] > 0f && enemyStrBuff != null)
        {
            // 적(!isPlayerAttacking)에게 3턴간 공격력 증가 부여
            BuffManager.Instance.AddEffect(!isPlayerAttacking, enemyStrBuff, strBuffRates[index], 3);
        }

        if (defDebuffRates[index] < 0f && enemyDefDebuff != null)
        {
            // 적(!isPlayerAttacking)에게 3턴간 방어력 감소 부여
            BuffManager.Instance.AddEffect(!isPlayerAttacking, enemyDefDebuff, defDebuffRates[index], 3);
        }

        DevLog.Log($"[스킬 효과] 빈 상자 발동! 스타일 랭크 +3. 적 광폭화 및 방어 약화 적용.");
    }
}