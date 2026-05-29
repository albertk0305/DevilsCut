using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Integrity_BrandNewDays", menuName = "SkillLogic/Integrity/Brand New Days")]
public class SkillLogic_Integrity_BrandNewDays : SkillLogicBase
{
    [SerializeField] private StatusEffectData damageAmpBuff;
    [SerializeField] private StatusEffectData damageReductionBuff;
    [SerializeField] private float buffValue = 0.20f;
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

        if (damageAmpBuff != null)
            BuffManager.Instance.AddEffect(false, damageAmpBuff, buffValue, buffTurns);
        else
            DevLog.LogWarning("[Brand New Days] damageAmpBuff is not assigned.");

        if (damageReductionBuff != null)
            BuffManager.Instance.AddEffect(false, damageReductionBuff, buffValue, buffTurns);
        else
            DevLog.LogWarning("[Brand New Days] damageReductionBuff is not assigned.");

        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.SetGauge(EntityType.Enemy, 100f);
        }
    }
}
