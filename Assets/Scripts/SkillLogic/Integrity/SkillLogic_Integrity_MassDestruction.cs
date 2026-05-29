using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Integrity_MassDestruction", menuName = "SkillLogic/Integrity/Mass Destruction")]
public class SkillLogic_Integrity_MassDestruction : SkillLogicBase
{
    [SerializeField] private float healRatioPerHit = 0.05f;

    public override void ApplyEffectOnHit(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking, bool isHit)
    {
        if (!isHit) return;
        if (isPlayerAttacking) return;
        if (enemy == null) return;

        int successfulHits = 0;
        if (CombatManager.Instance != null)
        {
            successfulHits = CombatManager.Instance.currentState.lastSuccessfulHits;
        }

        if (successfulHits <= 0) return;

        int healAmount = Mathf.RoundToInt(enemy.maxHp * healRatioPerHit * successfulHits);
        if (healAmount <= 0) return;

        CombatManager.Instance.HealEntity(false, healAmount);

        if (CombatUIManager.Instance != null)
        {
            CombatUIManager.Instance.SpawnDamageText($"<color=#00FF00>+{healAmount}</color>", false, false);
        }

        DevLog.Log($"[Mass Destruction] Integrity healed {healAmount} from {successfulHits} hits.");
    }
}
