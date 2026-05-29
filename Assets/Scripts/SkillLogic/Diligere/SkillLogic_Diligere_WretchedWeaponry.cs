using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Diligere_WretchedWeaponry", menuName = "SkillLogic/Diligere/Wretched Weaponry")]
public class SkillLogic_Diligere_WretchedWeaponry : SkillLogic_Diligere_Base
{
    [SerializeField] private StatusEffectData diligereBleed;
    [SerializeField] private float bleedMultiplierPerHit = 1f;
    [SerializeField] private int bleedTurns = 3;

    public override void ApplyEffectOnHit(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking, bool isHit)
    {
        if (!isHit) return;
        if (isPlayerAttacking) return;

        if (diligereBleed == null)
        {
            DevLog.LogWarning("[Wretched Weaponry] diligereBleed is not assigned.");
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
            BuffManager.Instance.AddEffect(true, diligereBleed, bleedMultiplierPerHit, bleedTurns);
        }
    }
}
