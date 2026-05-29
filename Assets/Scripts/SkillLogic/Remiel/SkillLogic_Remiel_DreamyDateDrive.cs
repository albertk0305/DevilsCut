using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Remiel_DreamyDateDrive", menuName = "SkillLogic/Remiel/dreamy date drive")]
public class SkillLogic_Remiel_DreamyDateDrive : SkillLogic_Remiel_Base
{
    [SerializeField] private StatusEffectData remielBleed;
    [SerializeField] private float bleedMultiplierPerHit = 1f;
    [SerializeField] private int bleedTurns = 3;

    public override void ApplyEffectOnHit(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking, bool isHit)
    {
        if (!isHit) return;
        if (isPlayerAttacking) return;

        if (remielBleed == null)
        {
            DevLog.LogWarning("[dreamy date drive] remielBleed is not assigned.");
            return;
        }

        int hitCount = 0;
        if (CombatManager.Instance != null)
        {
            hitCount = CombatManager.Instance.currentState.lastSuccessfulHits;
        }

        if (hitCount <= 0) return;

        for (int i = 0; i < hitCount; i++)
        {
            BuffManager.Instance.AddEffect(true, remielBleed, bleedMultiplierPerHit, bleedTurns);
        }
    }
}
