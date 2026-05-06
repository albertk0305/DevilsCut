using UnityEngine;

[CreateAssetMenu(fileName = "DummyStartSkill", menuName = "SkillLogic/Supporter/DummyStart")]
public class DummyStartSkill : SupporterLogicBase
{
    public override void ApplyEffect(PlayerStats pStats, EnemyData enemy)
    {
        CombatManager.Instance.AddPlayerBuff(CombatManager.BuffStat.Strength, CombatManager.BuffType.Percentage, 0.2f, 3);

        DevLog.Log("[조력자 개전 스킬] 셰리의 공격력이 3턴 동안 증가합니다!");
    }
}