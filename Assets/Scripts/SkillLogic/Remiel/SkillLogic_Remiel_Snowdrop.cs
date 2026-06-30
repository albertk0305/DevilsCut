using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "SkillLogic_Remiel_Snowdrop", menuName = "SkillLogic/Remiel/snowdrop")]
public class SkillLogic_Remiel_Snowdrop : SkillLogic_Remiel_Base
{
    [SerializeField] private StatusEffectData remielBleed;
    [SerializeField] private float bleedMultiplierPerHit = 1f;
    [SerializeField] private int bleedTurns = 3;
    [SerializeField] private float explosionMultiplier = 3f;

    public override void ApplyEffectOnHit(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking, bool isHit)
    {
        if (!isHit) return;
        if (isPlayerAttacking) return;

        if (remielBleed == null)
        {
            DevLog.LogWarning("[snowdrop] remielBleed is not assigned.");
            return;
        }

        int hitCount = 0;
        if (CombatManager.Instance != null)
        {
            hitCount = CombatManager.Instance.currentState.lastSuccessfulHits;
        }

        for (int i = 0; i < hitCount; i++)
        {
            BuffManager.Instance.AddEffect(true, remielBleed, bleedMultiplierPerHit, bleedTurns);
        }

        List<BuffManager.ActiveEffect> playerEffects = BuffManager.Instance.GetEffects(true);
        List<BuffManager.ActiveEffect> bleedStacks = playerEffects.FindAll(e => e.effectData == remielBleed);

        float totalBleedMultiplier = 0f;
        foreach (var stack in bleedStacks)
        {
            totalBleedMultiplier += stack.value;
        }

        if (totalBleedMultiplier <= 0f) return;

        playerEffects.RemoveAll(e => e.effectData == remielBleed);
        if (CombatUIManager.Instance != null) CombatUIManager.Instance.RefreshBuffUI();

        int remielStr = GetRemielStrength(enemy);
        int explosionDamage = Mathf.Max(1, Mathf.RoundToInt(totalBleedMultiplier * remielStr * explosionMultiplier));

        if (CombatManager.Instance != null)
        {
            CombatManager.Instance.ApplyDamageToEntity(true, explosionDamage);
        }

        if (CombatUIManager.Instance != null
            && (CombatManager.Instance == null || !CombatManager.Instance.LastDamageResolutionResult.showEndureText))
        {
            CombatUIManager.Instance.SpawnDamageText("\u2605" + explosionDamage.ToString(), false, true);
        }

        DevLog.Log($"[snowdrop] Total bleed multiplier {totalBleedMultiplier}, explosion damage {explosionDamage}.");
    }
}
