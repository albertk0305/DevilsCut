using UnityEngine;
using System.Collections.Generic;

// 모든 조력자 스킬(개전, 전투)의 부모 클래스입니다.
public class SupporterLogicBase : ScriptableObject
{
    // 1. 단일 타격 데미지 계산 (기본값 0)
    public virtual int CalculateDamage(PlayerStats pStats, EnemyData enemy, int skillLevel = 1) { return 0; }

    // 1-2. 다단히트 데미지 계산 (기본값 null)
    // 리스트에 데미지를 담아 반환하면 CompanionManager가 알아서 연타 연출을 해줍니다!
    public virtual List<int> CalculateMultiHitDamages(PlayerStats pStats, EnemyData enemy, int skillLevel = 1) { return null; }

    // 2. 특수 효과 발동 (버프, 디버프 등)
    public virtual void ApplyEffect(PlayerStats pStats, EnemyData enemy, int skillLevel = 1) { }
}