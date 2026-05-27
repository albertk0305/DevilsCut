using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Raguel_BeyondTheTime", menuName = "SkillLogic/Raguel/BeyondTheTime")]
public class SkillLogic_Raguel_BeyondTheTime : SkillLogic_Raguel_Base
{
    [SerializeField] private StatusEffectData overheatDamageAmpDebuff;

    public override bool AlwaysHits(SkillData skill) => true;

    public override void ApplyEffectOnHit(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking, bool isHit)
    {
        if (isPlayerAttacking) return;
        if (!isHit) return;

        EnemyAI_Raguel raguelAI = GetRaguelAI(enemy);
        if (raguelAI != null)
        {
            raguelAI.EnterOverheat(enemy, overheatDamageAmpDebuff);
        }
    }
}
