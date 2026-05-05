using UnityEngine;

// 모든 스킬 로직 스크립트들의 '부모'가 될 클래스입니다.
public class SkillLogicBase : ScriptableObject
{
    // 1. 브레이크 배율 (기본값 1.0배)
    // virtual로 선언했으므로, 필요한 스킬만 override해서 사용하면 됩니다.
    public virtual float GetBreakMultiplier(PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        return 1.0f;
    }

    // 2. 데미지 증폭 배율 (기본값 1.0배) - [신규 추가]
    public virtual float GetDamageMultiplier(PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        return 1.0f;
    }

    // 3. 특수 효과 발동 (기본값: 아무 효과 없음)
    public virtual void ApplyEffect(PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        // 평범한 스킬은 아무것도 하지 않고 넘어갑니다.
    }
}