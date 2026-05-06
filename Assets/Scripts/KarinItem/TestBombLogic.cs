using UnityEngine;

// 유니티 에디터에서 우클릭으로 이 로직 데이터를 생성할 수 있게 만듭니다.
[CreateAssetMenu(fileName = "TestBombLogic", menuName = "SkillLogic/Karin/TestBomb")]
public class TestBombLogic : KarinItemLogicBase
{
    public override int CalculateDamage(PlayerStats pStats, EnemyData enemy)
    {
        // 기획하신 대로 셰리(주인공)의 힘 스탯의 1배수를 데미지로 반환합니다.
        return pStats.strength;
    }

    public override void ApplyEffect(PlayerStats pStats, EnemyData enemy)
    {
        DevLog.Log("카린이 테스트 폭탄을 던졌습니다 쾅!");
        // 나중에 화상 디버프 등을 적에게 부여하는 코드를 여기에 추가할 수 있습니다.
    }
}