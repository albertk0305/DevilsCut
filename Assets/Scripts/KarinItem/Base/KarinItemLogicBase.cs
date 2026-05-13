using UnityEngine;

// 모든 카린 아이템 로직의 부모 클래스
public abstract class KarinItemLogicBase : ScriptableObject
{
    // 적에게 입힐 데미지를 계산하여 반환
    public abstract int CalculateDamage(PlayerStats pStats, EnemyData eData);

    // 공격 명중 시 발동할 특수 효과 (버프, 디버프, 그로기 등)
    public abstract void ApplyEffect(PlayerStats pStats, EnemyData eData);
}