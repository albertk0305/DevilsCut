using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Remiel_ColdWindSentiment", menuName = "SkillLogic/Remiel/Cold Wind Sentiment")]
public class SkillLogic_Remiel_ColdWindSentiment : SkillLogic_Remiel_Base
{
    [SerializeField] private StatusEffectData remielBleed;
    [SerializeField] private float bleedMultiplier = 5f;
    [SerializeField] private int bleedTurns = 3;

    public override void ApplyEffectOnHit(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking, bool isHit)
    {
        if (!isHit) return;
        if (isPlayerAttacking) return;

        if (remielBleed != null)
        {
            BuffManager.Instance.AddEffect(true, remielBleed, bleedMultiplier, bleedTurns);
        }
        else
        {
            DevLog.LogWarning("[Cold Wind Sentiment] remielBleed is not assigned.");
        }
    }
}
