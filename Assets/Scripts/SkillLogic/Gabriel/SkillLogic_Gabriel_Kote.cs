using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Gabriel_Kote", menuName = "SkillLogic/Gabriel/Kote")]
public class SkillLogic_Gabriel_Kote : SkillLogic_Gabriel_Base
{
    [SerializeField] private StatusEffectData strengthDebuff;
    [SerializeField] private float strengthDebuffValue = -0.20f;
    [SerializeField] private int strengthDebuffTurns = 3;

    public override void ApplyEffectOnHit(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking, bool isHit)
    {
        if (!isHit) return;
        if (isPlayerAttacking) return;

        ApplyGabrielPassiveOnHit(isPlayerAttacking, isHit);

        if (strengthDebuff != null)
        {
            BuffManager.Instance.AddEffect(true, strengthDebuff, strengthDebuffValue, strengthDebuffTurns);
        }
    }
}
