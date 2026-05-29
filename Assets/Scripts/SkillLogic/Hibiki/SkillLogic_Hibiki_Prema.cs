using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "SkillLogic_Hibiki_Prema", menuName = "SkillLogic/Hibiki/Prema")]
public class SkillLogic_Hibiki_Prema : SkillLogicBase
{
    public override void ApplyEffectOnHit(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking, bool isHit)
    {
        if (!isHit) return;
        if (isPlayerAttacking) return;
        if (BuffManager.Instance == null) return;

        List<BuffManager.ActiveEffect> playerEffects = BuffManager.Instance.GetEffects(true);
        List<BuffManager.ActiveEffect> candidates = new List<BuffManager.ActiveEffect>();

        foreach (var effect in playerEffects)
        {
            if (effect.effectData == null) continue;
            if (effect.effectData.isPermanentPassive) continue;
            if (effect.effectData.category == EffectCategory.Buff) candidates.Add(effect);
        }

        if (candidates.Count <= 0) return;

        BuffManager.ActiveEffect stolenEffect = candidates[Random.Range(0, candidates.Count)];
        BuffManager.Instance.AddEffect(false, stolenEffect.effectData, stolenEffect.value, stolenEffect.turnsLeft);
        playerEffects.Remove(stolenEffect);

        if (CombatUIManager.Instance != null) CombatUIManager.Instance.RefreshBuffUI();
        DevLog.Log("[Prema] Stole a player buff.");
    }
}
