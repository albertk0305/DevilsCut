using UnityEngine;
using System.Collections.Generic;

public class SkillLogic_Caritas_Base : SkillLogicBase
{
    protected EnemyAI_Caritas GetCaritasAI(EnemyData enemy)
    {
        return enemy != null ? enemy.aiBrain as EnemyAI_Caritas : null;
    }

    protected void RefreshCaritasPassive(EnemyData enemy)
    {
        EnemyAI_Caritas caritasAI = GetCaritasAI(enemy);
        if (caritasAI != null)
        {
            caritasAI.UpdatePassives(enemy);
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
        RefreshCaritasPassive(enemy);

        return true;
    }

    protected void ApplyFallbackDamageReduction(EnemyData enemy, StatusEffectData fallbackEffect, float value, int turns)
    {
        if (fallbackEffect == null)
        {
            DevLog.LogWarning("[Caritas] fallback damage reduction effect is not assigned.");
            return;
        }

        BuffManager.Instance.AddEffect(false, fallbackEffect, value, turns);
        RefreshCaritasPassive(enemy);
    }
}
