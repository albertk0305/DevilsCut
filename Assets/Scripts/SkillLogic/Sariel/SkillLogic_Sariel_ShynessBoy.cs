using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Sariel_ShynessBoy", menuName = "SkillLogic/Sariel/SHYNESS BOY")]
public class SkillLogic_Sariel_ShynessBoy : SkillLogic_Sariel_Base
{
    [SerializeField] private StatusEffectData fallbackDamageReductionBuff;
    [SerializeField] private float fallbackDamageReductionValue = 0.05f;
    [SerializeField] private int fallbackBuffTurns = 3;

    public override void ApplyEffectOnHit(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking, bool isHit)
    {
        if (!isHit) return;
        if (isPlayerAttacking) return;

        if (!TryStealRandomPlayerBuff(enemy, out _))
        {
            ApplyFallbackDamageReduction(enemy, fallbackDamageReductionBuff, fallbackDamageReductionValue, fallbackBuffTurns);
        }
    }
}
