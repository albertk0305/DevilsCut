using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Michael_Enrage", menuName = "SkillLogic/Michael/Enrage")]
public class SkillLogic_Michael_Enrage : SkillLogic_Michael_Base
{
    [Header("광폭화 시 부여할 스탯 버프들")]
    public StatusEffectData strBuff;
    public StatusEffectData defBuff;
    public StatusEffectData lukDebuff;

    public override bool AlwaysHits(SkillData skill) => true;

    // Damage scaling rule.
    public override float GetDamageMultiplier(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        return 0f;
    }

    public override void ApplyEffect(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        // HP cost/recovery rule.
        int healAmount = Mathf.RoundToInt(enemy.maxHp * 0.5f);
        if (CombatManager.Instance != null)
        {
            // HP cost/recovery rule.
            CombatManager.Instance.HealEntity(false, healAmount);

            if (CombatUIManager.Instance != null)
                CombatUIManager.Instance.SpawnDamageText($"<color=#00FF00>+{healAmount}</color>", false, false);
        }

        // Buff/debuff rule.
        if (strBuff != null) BuffManager.Instance.AddEffect(false, strBuff, 0.25f, 999);
        if (defBuff != null) BuffManager.Instance.AddEffect(false, defBuff, 0.25f, 999);
        if (lukDebuff != null) BuffManager.Instance.AddEffect(false, lukDebuff, -1.0f, 999);

        DevLog.Log("[미카엘] 광폭화 스킬 발동! 50% 체력 회복 및 버프 적용.");
    }
}