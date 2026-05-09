using UnityEngine;

[CreateAssetMenu(fileName = "DummyStartSkill", menuName = "SkillLogic/Supporter/DummyStart")]
public class DummyStartSkill : SupporterLogicBase
{
    [Header("부여할 버프 데이터")]
    public StatusEffectData attackBuffData; // 유니티 에디터에서 방금 만든 SO를 여기 드래그해서 넣습니다!

    public override void ApplyEffect(PlayerStats pStats, EnemyData enemy)
    {
        // 아군(true)에게 공격력 20%(0.2f) 증가 버프를 3턴 동안 부여합니다.
        if (attackBuffData != null)
        {
            BuffManager.Instance.AddEffect(true, attackBuffData, 0.2f, 3);
            DevLog.Log("[조력자 개전 스킬] 셰리의 공격력이 3턴 동안 20% 증가합니다!");
        }
    }
}