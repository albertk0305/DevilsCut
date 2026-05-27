using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Castus_Hymn", menuName = "SkillLogic/Castus/Hymn")]
public class SkillLogic_Castus_Hymn : SkillLogic_Castus_Base
{
    [SerializeField] private StatusEffectData strengthDebuff;
    [SerializeField] private StatusEffectData defenseDebuff;
    [SerializeField] private float strengthDebuffValue = -0.10f;
    [SerializeField] private float defenseDebuffValue = -0.10f;
    [SerializeField] private int debuffTurns = 3;

    public override void ApplyEffectOnHit(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking, bool isHit)
    {
        if (!isHit) return;
        if (isPlayerAttacking) return;

        ApplyCastusPassiveOnHit(isPlayerAttacking, isHit);

        if (strengthDebuff != null)
        {
            BuffManager.Instance.AddEffect(true, strengthDebuff, strengthDebuffValue, debuffTurns);
        }
        else
        {
            DevLog.LogWarning("[Hymn] strengthDebuff가 연결되지 않았습니다.");
        }

        if (defenseDebuff != null)
        {
            BuffManager.Instance.AddEffect(true, defenseDebuff, defenseDebuffValue, debuffTurns);
        }
        else
        {
            DevLog.LogWarning("[Hymn] defenseDebuff가 연결되지 않았습니다.");
        }
    }
}
