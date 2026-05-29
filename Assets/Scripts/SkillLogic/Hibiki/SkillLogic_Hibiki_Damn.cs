using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Hibiki_Damn", menuName = "SkillLogic/Hibiki/Damn")]
public class SkillLogic_Hibiki_Damn : SkillLogicBase
{
    [SerializeField] private StatusEffectData breakResistanceDebuff;
    [SerializeField] private float breakResistanceDebuffValue = -0.20f;
    [SerializeField] private int debuffTurns = 3;

    public override void ApplyEffectOnHit(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking, bool isHit)
    {
        if (!isHit) return;
        if (isPlayerAttacking) return;

        if (breakResistanceDebuff != null)
            BuffManager.Instance.AddEffect(true, breakResistanceDebuff, breakResistanceDebuffValue, debuffTurns);
        else
            DevLog.LogWarning("[Damn] breakResistanceDebuff is not assigned.");
    }
}
