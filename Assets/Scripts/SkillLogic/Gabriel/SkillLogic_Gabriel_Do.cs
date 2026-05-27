using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Gabriel_Do", menuName = "SkillLogic/Gabriel/Do")]
public class SkillLogic_Gabriel_Do : SkillLogic_Gabriel_Base
{
    [SerializeField] private StatusEffectData speedDebuff;
    [SerializeField] private float speedDebuffValue = -0.20f;
    [SerializeField] private int speedDebuffTurns = 3;

    public override void ApplyEffectOnHit(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking, bool isHit)
    {
        if (!isHit) return;
        if (isPlayerAttacking) return;

        ApplyGabrielPassiveOnHit(isPlayerAttacking, isHit);

        if (speedDebuff != null)
        {
            BuffManager.Instance.AddEffect(true, speedDebuff, speedDebuffValue, speedDebuffTurns);
        }
    }
}
