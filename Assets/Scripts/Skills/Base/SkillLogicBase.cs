using UnityEngine;

// 모든 스킬 로직 스크립트들의 '부모'가 될 클래스입니다.
public class SkillLogicBase : ScriptableObject
{
    // 1. 브레이크 배율 (기본값 1.0배)
    // virtual로 선언했으므로, 필요한 스킬만 override해서 사용하면 됩니다.
    public virtual float GetBreakMultiplier(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        return 1.0f;
    }

    public virtual float GetDamageMultiplier(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        return 1.0f;
    }

    public virtual void ApplyEffect(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
    }

    public virtual float GetArmorPenetrationRatio(SkillData skill, int skillLevel)
    {
        return 0f;
    }

    public virtual int GetHitCount(SkillData skill)
    {
        return skill.GetCurrentHitCount();
    }
}