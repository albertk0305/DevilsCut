using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Hibiki_Matsuri", menuName = "SkillLogic/Hibiki/Matsuri")]
public class SkillLogic_Hibiki_Matsuri : SkillLogicBase
{
    [SerializeField] private StatusEffectData strengthBuff;
    [SerializeField] private StatusEffectData defenseBuff;
    [SerializeField] private StatusEffectData speedBuff;
    [SerializeField] private StatusEffectData luckBuff;
    [SerializeField] private float statBuffValue = 0.20f;
    [SerializeField] private int buffTurns = 3;
    [SerializeField] private float healRatio = 0.15f;

    public override bool AlwaysHits(SkillData skill) => true;

    public override float GetDamageMultiplier(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        return 0f;
    }

    public override float GetBreakMultiplier(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        return 0f;
    }

    public override void ApplyEffect(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        if (isPlayerAttacking) return;
        if (enemy == null) return;

        RemoveEnemyDebuffs();
        HealEnemy(enemy);
        ApplyStatBuffs(enemy);

        if (enemy.aiBrain is EnemyAI_Hibiki hibikiAI)
        {
            hibikiAI.AddMatsuriApStackIfPossible();
        }
    }

    private void RemoveEnemyDebuffs()
    {
        if (BuffManager.Instance == null) return;

        var enemyEffects = BuffManager.Instance.GetEffects(false);
        int removedCount = enemyEffects.RemoveAll(e =>
            e.effectData != null &&
            e.effectData.category == EffectCategory.Debuff);

        if (removedCount > 0 && CombatUIManager.Instance != null)
        {
            CombatUIManager.Instance.RefreshBuffUI();
        }

        DevLog.Log($"[Matsuri] Removed {removedCount} enemy debuffs.");
    }

    private void HealEnemy(EnemyData enemy)
    {
        int healAmount = Mathf.RoundToInt(enemy.maxHp * healRatio);
        if (CombatManager.Instance == null) return;

        CombatManager.Instance.HealEntity(false, healAmount);

        if (CombatUIManager.Instance != null)
        {
            CombatUIManager.Instance.SpawnDamageText($"<color=#00FF00>+{healAmount}</color>", false, false);
        }
    }

    private void ApplyStatBuffs(EnemyData enemy)
    {
        AddEnemyBuff(strengthBuff, "strengthBuff");
        AddEnemyBuff(defenseBuff, "defenseBuff");
        AddEnemyBuff(speedBuff, "speedBuff");
        AddEnemyBuff(luckBuff, "luckBuff");
    }

    private void AddEnemyBuff(StatusEffectData effect, string label)
    {
        if (effect != null)
        {
            BuffManager.Instance.AddEffect(false, effect, statBuffValue, buffTurns);
        }
        else
        {
            DevLog.LogWarning($"[Matsuri] {label} is not assigned.");
        }
    }
}
