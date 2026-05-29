using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Arete_ManInTheMirror", menuName = "SkillLogic/Arete/Man in the Mirror")]
public class SkillLogic_Arete_ManInTheMirror : SkillLogicBase
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
        int removedCount = playerEffects.RemoveAll(e =>
            e.effectData != null &&
            !e.effectData.isPermanentPassive &&
            e.effectData.category == EffectCategory.Buff);

        if (removedCount > 0 && CombatUIManager.Instance != null)
        {
            CombatUIManager.Instance.RefreshBuffUI();
        }

        DevLog.Log($"[Man in the Mirror] Removed {removedCount} player buffs.");
    }
}
