using UnityEngine;
using System.Collections.Generic;

public class SkillLogic_Sariel_Base : SkillLogicBase
{
    protected EnemyAI_Sariel GetSarielAI(EnemyData enemy)
    {
        return enemy != null ? enemy.aiBrain as EnemyAI_Sariel : null;
    }

    protected void RefreshSarielPassive(EnemyData enemy)
    {
        EnemyAI_Sariel sarielAI = GetSarielAI(enemy);
        if (sarielAI != null)
        {
            sarielAI.UpdatePassives(enemy);
        }
    }

    protected bool TryStealRandomPlayerBuff(EnemyData enemy, out BuffManager.ActiveEffect stolenEffect)
    {
        stolenEffect = null;
        if (BuffManager.Instance == null) return false;

        List<BuffManager.ActiveEffect> playerEffects = BuffManager.Instance.GetEffects(true);
        List<BuffManager.ActiveEffect> candidates = new List<BuffManager.ActiveEffect>();

        foreach (var effect in playerEffects)
        {
            if (effect.effectData == null) continue;
            if (effect.effectData.isPermanentPassive) continue;
            if (effect.effectData.category == EffectCategory.Buff) candidates.Add(effect);
        }

        if (candidates.Count <= 0) return false;

        stolenEffect = candidates[Random.Range(0, candidates.Count)];
        BuffManager.Instance.AddEffect(false, stolenEffect.effectData, stolenEffect.value, stolenEffect.turnsLeft);
        playerEffects.Remove(stolenEffect);

        if (CombatUIManager.Instance != null) CombatUIManager.Instance.RefreshBuffUI();
        RefreshSarielPassive(enemy);

        return true;
    }

    protected void ApplyFallbackDamageReduction(EnemyData enemy, StatusEffectData fallbackEffect, float value, int turns)
    {
        if (fallbackEffect == null)
        {
            DevLog.LogWarning("[Sariel] fallback damage reduction effect is not assigned.");
            return;
        }

        BuffManager.Instance.AddEffect(false, fallbackEffect, value, turns);
        RefreshSarielPassive(enemy);
    }
}
