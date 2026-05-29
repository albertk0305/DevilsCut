using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Sariel_HeavensFallingDown", menuName = "SkillLogic/Sariel/Heaven's falling down")]
public class SkillLogic_Sariel_HeavensFallingDown : SkillLogic_Sariel_Base
{
    public override void ApplyEffect(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        if (isPlayerAttacking) return;
        if (BuffManager.Instance == null) return;

        int extendedCount = 0;
        var enemyEffects = BuffManager.Instance.GetEffects(false);

        foreach (var effect in enemyEffects)
        {
            if (effect.effectData == null) continue;
            if (effect.effectData.isPermanentPassive) continue;
            if (effect.effectData.category != EffectCategory.Buff) continue;

            effect.turnsLeft += 1;
            extendedCount++;
        }

        if (CombatUIManager.Instance != null) CombatUIManager.Instance.RefreshBuffUI();
        DevLog.Log($"[Heaven's falling down] Extended {extendedCount} Sariel buff(s).");
    }
}
