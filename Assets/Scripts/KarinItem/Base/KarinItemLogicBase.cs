using UnityEngine;

// 모든 카린 아이템 로직 스크립트들의 '부모'가 될 클래스입니다.
public class KarinItemLogicBase : ScriptableObject
{
    // 1. 데미지 계산 (기본값 0)
    // 필요한 아이템만 override해서 데미지 공식을 만듭니다.
    public virtual int CalculateDamage(PlayerStats pStats, EnemyData enemy)
    {
        return 0;
    }

    // 2. 특수 효과 발동 (기본값 없음)
    // 힐링, 버프, 디버프 등 특수한 효과를 구현할 때 씁니다.
    public virtual void ApplyEffect(PlayerStats pStats, EnemyData enemy)
    {
    }
}