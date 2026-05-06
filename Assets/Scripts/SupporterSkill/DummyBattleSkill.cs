using UnityEngine;

[CreateAssetMenu(fileName = "DummyBattleSkill", menuName = "SkillLogic/Supporter/DummyBattle")]
public class DummyBattleSkill : SupporterLogicBase
{
    public override int CalculateDamage(PlayerStats pStats, EnemyData enemy)
    {
        // 기획하신 대로 셰리 힘의 2배 데미지를 줍니다!
        return pStats.strength * 2;
    }

    public override void ApplyEffect(PlayerStats pStats, EnemyData enemy)
    {
        DevLog.Log("[조력자 전투 스킬] 적에게 강력한 일격을 가했습니다!");
    }
}