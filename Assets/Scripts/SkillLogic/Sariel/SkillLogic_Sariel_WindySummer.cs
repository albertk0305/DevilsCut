using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Sariel_WindySummer", menuName = "SkillLogic/Sariel/Windy Summer")]
public class SkillLogic_Sariel_WindySummer : SkillLogic_Sariel_Base
{
    [SerializeField] private StatusEffectData fallbackDamageReductionBuff;
    [SerializeField] private float fallbackDamageReductionValue = 0.05f;
    [SerializeField] private int fallbackBuffTurns = 3;

    public override void ApplyEffectOnHit(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking, bool isHit)
    {
        if (!isHit) return;
        if (isPlayerAttacking) return;

        int successfulHits = 0;
        if (CombatManager.Instance != null)
        {
            successfulHits = CombatManager.Instance.currentState.lastSuccessfulHits;
        }

        if (successfulHits <= 0) return;

        for (int i = 0; i < successfulHits; i++)
        {
            if (!TryStealRandomPlayerBuff(enemy, out _))
            {
                ApplyFallbackDamageReduction(enemy, fallbackDamageReductionBuff, fallbackDamageReductionValue, fallbackBuffTurns);
            }
        }

        RefreshSarielPassive(enemy);
        if (CombatUIManager.Instance != null) CombatUIManager.Instance.RefreshBuffUI();
    }
}
