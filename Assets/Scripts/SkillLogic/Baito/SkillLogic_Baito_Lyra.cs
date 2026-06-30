using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Baito_Lyra", menuName = "SkillLogic/Baito/Lyra")]
public class SkillLogic_Baito_Lyra : SkillLogic_Baito_UtilityBase
{
    public override void ApplyEffect(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        if (isPlayerAttacking || enemy == null) return;

        int removedCount = RemoveEnemyDebuffs();
        int healAmount = Mathf.Max(0, enemy.maxHp - enemy.currentHp);
        HealEnemyByAmount(healAmount);

        EnemyAI_Baito baitoAI = GetBaitoAI(enemy);
        if (baitoAI != null)
            baitoAI.NotifyLyraResolved(enemy);

        DevLog.Log($"[Baito: Lyra] Removed {removedCount} debuffs and healed to full.");
    }
}
