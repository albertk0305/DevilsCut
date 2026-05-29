using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Silere_ItAintOver", menuName = "SkillLogic/Silere/It Ain't Over")]
public class SkillLogic_Silere_ItAintOver : SkillLogicBase
{
    [SerializeField] private StatusEffectData strengthDebuff;
    [SerializeField] private StatusEffectData defenseDebuff;
    [SerializeField] private float strengthDebuffValue = -0.20f;
    [SerializeField] private float defenseDebuffValue = -0.20f;
    [SerializeField] private int debuffTurns = 3;

    public override void ApplyEffectOnHit(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking, bool isHit)
    {
        if (!isHit) return;
        if (isPlayerAttacking) return;

        if (strengthDebuff != null)
            BuffManager.Instance.AddEffect(true, strengthDebuff, strengthDebuffValue, debuffTurns);
        else
            DevLog.LogWarning("[It Ain't Over] strengthDebuff is not assigned.");

        if (defenseDebuff != null)
            BuffManager.Instance.AddEffect(true, defenseDebuff, defenseDebuffValue, debuffTurns);
        else
            DevLog.LogWarning("[It Ain't Over] defenseDebuff is not assigned.");
    }
}
