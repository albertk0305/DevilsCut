using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Arete_HumanNature", menuName = "SkillLogic/Arete/Human Nature")]
public class SkillLogic_Arete_HumanNature : SkillLogicBase
{
    public override bool AlwaysHits(SkillData skill) => true;

    public override float GetDamageMultiplier(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        return 0f;
    }

    public override float GetBreakMultiplier(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        return 0f;
    }

    public override void ApplyEffect(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        if (isPlayerAttacking) return;

        if (CombatManager.Instance != null)
        {
            CombatManager.Instance.SetPlayerHpToOneForScriptedEffect();
        }

        if (CombatUIManager.Instance != null)
        {
            CombatUIManager.Instance.SpawnDamageText("★999999999", false, true);
        }

        DevLog.Log("[Human Nature] Player HP set to 1 by scripted effect.");
    }
}
