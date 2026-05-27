using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Gabriel_Tsuki", menuName = "SkillLogic/Gabriel/Tsuki")]
public class SkillLogic_Gabriel_Tsuki : SkillLogic_Gabriel_Base
{
    [SerializeField] private StatusEffectData defenseDebuff;
    [SerializeField] private float defenseDebuffValue = -0.20f;
    [SerializeField] private int defenseDebuffTurns = 3;

    public override void ApplyEffectOnHit(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking, bool isHit)
    {
        if (!isHit) return;
        if (isPlayerAttacking) return;

        ApplyGabrielPassiveOnHit(isPlayerAttacking, isHit);

        if (defenseDebuff != null)
        {
            BuffManager.Instance.AddEffect(true, defenseDebuff, defenseDebuffValue, defenseDebuffTurns);
        }
    }
}
