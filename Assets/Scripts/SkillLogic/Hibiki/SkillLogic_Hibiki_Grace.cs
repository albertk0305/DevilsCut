using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Hibiki_Grace", menuName = "SkillLogic/Hibiki/Grace")]
public class SkillLogic_Hibiki_Grace : SkillLogicBase
{
    [SerializeField] private StatusEffectData damageAmpBuff;
    [SerializeField] private StatusEffectData damageReductionBuff;
    [SerializeField] private float buffValue = 0.25f;
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
            DevLog.LogWarning("[Grace] damageAmpBuff is not assigned.");

        if (damageReductionBuff != null)
            BuffManager.Instance.AddEffect(false, damageReductionBuff, buffValue, buffTurns);
        else
            DevLog.LogWarning("[Grace] damageReductionBuff is not assigned.");
    }
}
