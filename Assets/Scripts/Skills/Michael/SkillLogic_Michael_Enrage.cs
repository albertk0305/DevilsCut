using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Michael_Enrage", menuName = "SkillLogic/Michael/Enrage")]
public class SkillLogic_Michael_Enrage : SkillLogic_Michael_Base
{
    [Header("광폭화 시 부여할 스탯 버프들")]
    public StatusEffectData strBuff;    // 힘 증가 (+25%)
    public StatusEffectData defBuff;    // 방어 증가 (+25%)
    public StatusEffectData lukDebuff;  // 운 감소 (-100%)

    // [핵심] 요술 생존기처럼 빗나가지 않게 강제합니다.
    public override bool AlwaysHits(SkillData skill) => true;

    // [핵심] 피해 계수를 0으로 반환하여 CombatManager가 '유틸리티 스킬'로 인식하게 합니다.
    public override float GetDamageMultiplier(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        return 0f;
    }

    public override void ApplyEffect(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        // 1. 체력 회복 (최대 체력의 50%)
        int healAmount = Mathf.RoundToInt(enemy.maxHp * 0.5f);
        if (CombatManager.Instance != null)
        {
            // HealEntity를 호출하면 체력 업데이트, UI 갱신, 패시브 갱신까지 한 번에 처리됩니다.
            CombatManager.Instance.HealEntity(false, healAmount);

            // 텍스트 출력
            if (CombatUIManager.Instance != null)
                CombatUIManager.Instance.SpawnDamageText($"<color=#00FF00>+{healAmount}</color>", false, false);
        }

        // 2. 스탯 버프 부여
        if (strBuff != null) BuffManager.Instance.AddEffect(false, strBuff, 0.25f, 999);
        if (defBuff != null) BuffManager.Instance.AddEffect(false, defBuff, 0.25f, 999);
        if (lukDebuff != null) BuffManager.Instance.AddEffect(false, lukDebuff, -1.0f, 999);

        DevLog.Log("[미카엘] 광폭화 스킬 발동! 50% 체력 회복 및 버프 적용.");
    }
}