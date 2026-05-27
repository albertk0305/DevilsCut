using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Raguel_BloomInTheNight", menuName = "SkillLogic/Raguel/BloomInTheNight")]
public class SkillLogic_Raguel_BloomInTheNight : SkillLogic_Raguel_Base
{
    [SerializeField] private StatusEffectData defenseDebuff;
    [SerializeField] private float defenseDebuffValue = -0.20f;
    [SerializeField] private int defenseDebuffTurns = 3;

    public override void ApplyEffectOnHit(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking, bool isHit)
    {
        if (!isHit) return;
        if (isPlayerAttacking) return;

        if (defenseDebuff == null)
        {
            DevLog.LogWarning("[Bloom In The Night] defenseDebuff가 연결되지 않았습니다.");
            return;
        }

        BuffManager.Instance.AddEffect(true, defenseDebuff, defenseDebuffValue, defenseDebuffTurns);
    }
}
