using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Integrity_HeartfulCry", menuName = "SkillLogic/Integrity/Heartful Cry")]
public class SkillLogic_Integrity_HeartfulCry : SkillLogicBase
{
    public override void ApplyEffectOnHit(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking, bool isHit)
    {
        if (!isHit) return;
        if (isPlayerAttacking) return;
        if (BuffManager.Instance == null) return;

        int successfulHits = 0;
        if (CombatManager.Instance != null)
        {
            successfulHits = CombatManager.Instance.currentState.lastSuccessfulHits;
        }

        if (successfulHits <= 0) return;

        var playerEffects = BuffManager.Instance.GetEffects(true);
        int removedCount = 0;

        for (int i = playerEffects.Count - 1; i >= 0 && removedCount < successfulHits; i--)
        {
            var effect = playerEffects[i];
            if (effect.effectData == null) continue;
            if (effect.effectData.isPermanentPassive) continue;
            if (effect.effectData.category != EffectCategory.Buff) continue;

            playerEffects.RemoveAt(i);
            removedCount++;
        }

        if (removedCount > 0 && CombatUIManager.Instance != null)
        {
            CombatUIManager.Instance.RefreshBuffUI();
        }

        DevLog.Log($"[Heartful Cry] Removed {removedCount} player buffs from {successfulHits} hits.");
    }
}
