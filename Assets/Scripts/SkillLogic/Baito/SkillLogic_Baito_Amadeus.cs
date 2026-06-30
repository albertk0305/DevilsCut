using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Baito_Amadeus", menuName = "SkillLogic/Baito/Amadeus")]
public class SkillLogic_Baito_Amadeus : SkillLogic_Baito_UtilityBase
{
    [SerializeField] private float missingHpHealRatio = 0.30f;

    public override void ApplyEffect(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        if (isPlayerAttacking || enemy == null) return;

        int removedCount = RemoveEnemyDebuffs();
        int healAmount = CalculateMissingHpHeal(enemy, missingHpHealRatio);
        HealEnemyByAmount(healAmount);

        EnemyAI_Baito baitoAI = GetBaitoAI(enemy);
        if (baitoAI != null)
            baitoAI.NotifyAmadeusResolved(enemy);

        DevLog.Log($"[Baito: Amadeus] Removed {removedCount} debuffs and healed {healAmount} HP.");
    }
}
