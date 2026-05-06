using UnityEngine;

// 모든 조력자 스킬(개전, 전투)의 부모 클래스입니다.
public class SupporterLogicBase : ScriptableObject
{
    // 1. 데미지 계산 (기본값 0)
    public virtual int CalculateDamage(PlayerStats pStats, EnemyData enemy)
    {
        return 0;
    }

    // 2. 특수 효과 발동 (버프, 디버프 등)
    public virtual void ApplyEffect(PlayerStats pStats, EnemyData enemy)
    {
    }
}