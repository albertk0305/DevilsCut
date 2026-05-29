using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Uriel_Strongest", menuName = "SkillLogic/Uriel/Strongest")]
public class SkillLogic_Uriel_Strongest : SkillLogic_Uriel_Base
{
    [SerializeField] private StatusEffectData evasionDebuff;
    [SerializeField] private float evasionDebuffValue = -25f;
    [SerializeField] private int debuffTurns = 3;

    public override void PaySkillCost(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        if (isPlayerAttacking) return;

        AddEndurance(enemy, 1);
    }

    public override void ApplyEffectOnHit(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking, bool isHit)
    {
        if (!isHit) return;
        if (isPlayerAttacking) return;

        if (evasionDebuff != null)
        {
            BuffManager.Instance.AddEffect(true, evasionDebuff, evasionDebuffValue, debuffTurns);
        }
        else
        {
            DevLog.LogWarning("[Strongest] evasionDebuff is not assigned.");
        }
    }
}
