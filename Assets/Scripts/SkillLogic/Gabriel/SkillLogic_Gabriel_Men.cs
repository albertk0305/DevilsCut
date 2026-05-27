using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Gabriel_Men", menuName = "SkillLogic/Gabriel/Men")]
public class SkillLogic_Gabriel_Men : SkillLogic_Gabriel_Base
{
    [SerializeField] private StatusEffectData apDebuff;
    [SerializeField] private float apDebuffValue = -0.20f;
    [SerializeField] private int apDebuffTurns = 3;

    public override void ApplyEffectOnHit(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking, bool isHit)
    {
        if (!isHit) return;
        if (isPlayerAttacking) return;

        ApplyGabrielPassiveOnHit(isPlayerAttacking, isHit);

        if (apDebuff != null)
        {
            BuffManager.Instance.AddEffect(true, apDebuff, apDebuffValue, apDebuffTurns);
        }
    }
}
