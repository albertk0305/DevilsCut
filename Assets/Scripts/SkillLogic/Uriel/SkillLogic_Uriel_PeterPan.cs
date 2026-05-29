using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Uriel_PeterPan", menuName = "SkillLogic/Uriel/Peter Pan")]
public class SkillLogic_Uriel_PeterPan : SkillLogic_Uriel_Base
{
    [SerializeField] private StatusEffectData defenseBuff;
    [SerializeField] private float defenseBuffValue = 0.50f;
    [SerializeField] private int buffTurns = 3;

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

        if (defenseBuff != null)
        {
            BuffManager.Instance.AddEffect(false, defenseBuff, defenseBuffValue, buffTurns);
        }
        else
        {
            DevLog.LogWarning("[Peter Pan] defenseBuff is not assigned.");
        }

        AddEndurance(enemy, 1);
    }
}
