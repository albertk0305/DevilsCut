using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Baito_SkycladObserver", menuName = "SkillLogic/Baito/Skyclad Observer")]
public class SkillLogic_Baito_SkycladObserver : SkillLogic_Baito_UtilityBase
{
    [SerializeField] private float missingHpHealRatio = 0.30f;

    public override void ApplyEffect(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        if (isPlayerAttacking || enemy == null) return;

        int removedCount = RemoveEnemyDebuffs();
        int healAmount = CalculateMissingHpHeal(enemy, missingHpHealRatio);
        HealEnemyByAmount(healAmount);

        DevLog.Log($"[Baito: Skyclad Observer] Removed {removedCount} debuffs and healed {healAmount} HP.");
    }
}
