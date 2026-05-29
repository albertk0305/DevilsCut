using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Hibiki_Tabiji", menuName = "SkillLogic/Hibiki/Tabiji")]
public class SkillLogic_Hibiki_Tabiji : SkillLogicBase
{
    [SerializeField] private StatusEffectData accuracyDebuff;
    [SerializeField] private StatusEffectData evasionDebuff;
    [SerializeField] private float accuracyDebuffValue = -20f;
    [SerializeField] private float evasionDebuffValue = -20f;
    [SerializeField] private int debuffTurns = 3;

    public override void ApplyEffectOnHit(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking, bool isHit)
    {
        if (!isHit) return;
        if (isPlayerAttacking) return;

        if (accuracyDebuff != null)
            BuffManager.Instance.AddEffect(true, accuracyDebuff, accuracyDebuffValue, debuffTurns);
        else
            DevLog.LogWarning("[Tabiji] accuracyDebuff is not assigned.");

        if (evasionDebuff != null)
            BuffManager.Instance.AddEffect(true, evasionDebuff, evasionDebuffValue, debuffTurns);
        else
            DevLog.LogWarning("[Tabiji] evasionDebuff is not assigned.");
    }
}
