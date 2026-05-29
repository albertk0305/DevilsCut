using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Temperare_OverflowingWealth", menuName = "SkillLogic/Temperare/Overflowing Wealth")]
public class SkillLogic_Temperare_OverflowingWealth : SkillLogic_Temperare_Base
{
    [SerializeField] private StatusEffectData speedDebuff;
    [SerializeField] private StatusEffectData accuracyDebuff;
    [SerializeField] private float speedDebuffValue = -0.10f;
    [SerializeField] private float accuracyDebuffValue = -0.10f;
    [SerializeField] private int debuffTurns = 3;

    public override void ApplyEffectOnHit(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking, bool isHit)
    {
        if (!isHit) return;
        if (isPlayerAttacking) return;

        if (speedDebuff != null)
        {
            BuffManager.Instance.AddEffect(true, speedDebuff, speedDebuffValue, debuffTurns);
        }
        else
        {
            DevLog.LogWarning("[Overflowing Wealth] speedDebuff is not assigned.");
        }

        if (accuracyDebuff != null)
        {
            BuffManager.Instance.AddEffect(true, accuracyDebuff, accuracyDebuffValue * 100f, debuffTurns);
        }
        else
        {
            DevLog.LogWarning("[Overflowing Wealth] accuracyDebuff is not assigned.");
        }
    }
}
