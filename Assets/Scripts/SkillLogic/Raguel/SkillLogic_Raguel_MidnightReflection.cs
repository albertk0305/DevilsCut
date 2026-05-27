using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Raguel_MidnightReflection", menuName = "SkillLogic/Raguel/MidnightReflection")]
public class SkillLogic_Raguel_MidnightReflection : SkillLogic_Raguel_Base
{
    [SerializeField] private StatusEffectData speedDebuff;
    [SerializeField] private float speedDebuffValue = -0.25f;
    [SerializeField] private int speedDebuffTurns = 3;

    public override void ApplyEffectOnHit(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking, bool isHit)
    {
        if (!isHit) return;
        if (isPlayerAttacking) return;

        if (speedDebuff == null)
        {
            DevLog.LogWarning("[Midnight Reflection] speedDebuff가 연결되지 않았습니다.");
            return;
        }

        BuffManager.Instance.AddEffect(true, speedDebuff, speedDebuffValue, speedDebuffTurns);
    }
}
