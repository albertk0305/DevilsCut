using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Remiel_StapleStaple", menuName = "SkillLogic/Remiel/staple staple")]
public class SkillLogic_Remiel_StapleStaple : SkillLogic_Remiel_Base
{
    [SerializeField] private StatusEffectData apDebuff;
    [SerializeField] private float apDebuffValue = -0.25f;
    [SerializeField] private int debuffTurns = 3;

    public override void ApplyEffectOnHit(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking, bool isHit)
    {
        if (!isHit) return;
        if (isPlayerAttacking) return;

        if (apDebuff != null)
        {
            BuffManager.Instance.AddEffect(true, apDebuff, apDebuffValue, debuffTurns);
        }
        else
        {
            DevLog.LogWarning("[staple staple] apDebuff is not assigned.");
        }
    }
}
