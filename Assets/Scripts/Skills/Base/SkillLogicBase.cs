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

    public virtual void ApplyEffectOnHit(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking, bool isHit)
    {
        // 하위 호환성을 위해 기본적으로 기존 ApplyEffect를 호출합니다.
        ApplyEffect(skill, pStats, enemy, isPlayerAttacking);
    }

    public virtual float GetArmorPenetrationRatio(SkillData skill, int skillLevel)
    {
        return 0f;
    }

    public virtual int GetHitCount(SkillData skill)
    {
        return skill.GetCurrentHitCount();
    }

    public virtual bool AlwaysHits()
    {
        return false; // 기본적으로는 명중률을 계산합니다.
    }

    public virtual bool AlwaysHits(SkillData skill)
    {
        return AlwaysHits(); // 기본적으로는 기존 함수를 호출 (하위 호환성 유지)
    }

    public virtual void PaySkillCost(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
    }

    public virtual float GetCritDamageMultiplier(SkillData skill)
    {
        return 1.5f;
    }

    public virtual bool AlwaysMisses(SkillData skill, int hitIndex)
    {
        return false;
    }

    public virtual float GetBaseAccuracy(SkillData skill)
    {
        return skill.baseAccuracy; // 기본적으로는 SkillData 인스펙터에 적힌 값을 그대로 씁니다.
    }

    public virtual float GetDynamicDamageMultiplier(SkillData skill, int consecutiveHits)
    {
        return 1.0f;
    }

    public virtual float GetDynamicCritRateBonus(SkillData skill, int consecutiveHits)
    {
        return 0f;
    }

    public virtual Sprite GetCounterActionImage(SkillData skill)
    {
        // 기본적으로는 스킬의 일반 액션 이미지를 반환하여 하위 호환성을 유지합니다.
        return skill.skillActionImage;
    }
}