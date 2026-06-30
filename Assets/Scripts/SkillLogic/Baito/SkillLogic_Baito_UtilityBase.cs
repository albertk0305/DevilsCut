using UnityEngine;

public abstract class SkillLogic_Baito_UtilityBase : SkillLogicBase
{
    public override bool AlwaysHits(SkillData skill) => true;

    public override float GetDamageMultiplier(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        return 0f;
    }

    public override float GetBreakMultiplier(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        return 0f;
    }

    protected EnemyAI_Baito GetBaitoAI(EnemyData enemy)
    {
        return enemy != null ? enemy.aiBrain as EnemyAI_Baito : null;
    }

    protected int RemoveEnemyDebuffs()
    {
        if (BuffManager.Instance == null) return 0;

        var enemyEffects = BuffManager.Instance.GetEffects(false);
        int removedCount = enemyEffects.RemoveAll(e =>
            e.effectData != null &&
            !e.effectData.isPermanentPassive &&
            e.effectData.category == EffectCategory.Debuff);

        if (removedCount > 0 && CombatUIManager.Instance != null)
            CombatUIManager.Instance.RefreshBuffUI();

        return removedCount;
    }

    protected void HealEnemyByAmount(int healAmount)
    {
        if (healAmount <= 0 || CombatManager.Instance == null) return;

        CombatManager.Instance.HealEntity(false, healAmount);

        if (CombatUIManager.Instance != null)
            CombatUIManager.Instance.SpawnDamageText($"<color=#00FF00>+{healAmount}</color>", false, false);
    }

    protected int CalculateMissingHpHeal(EnemyData enemy, float ratio)
    {
        if (enemy == null) return 0;

        int missingHp = Mathf.Max(0, enemy.maxHp - enemy.currentHp);
        return Mathf.RoundToInt(missingHp * ratio);
    }
}
