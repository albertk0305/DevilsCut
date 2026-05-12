using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Crusader", menuName = "SkillLogic/Player/Crusader")]
public class SkillLogic_Crusader : SkillLogicBase
{
    [Header("기본: 레벨별 그로기 추가 데미지 증폭률")]
    public float[] bonusDamageRatesOnBreak = { 0.50f, 0.75f, 1.0f };

    [Header("진화 A (Bloody Stream)")]
    public StatusEffectData defDownEffect;
    public StatusEffectData brDownEffect;
    //  [수정됨] 레벨별 1타당 감소율 (2%, 3%, 4%)
    public float[] pathA_DebuffPerHitRates = { 0.02f, 0.03f, 0.04f };

    [Header("진화 B (Stand Proud)")]
    //  [신규 추가] 레벨별 복리 증폭 배율 (1.10배, 1.15배, 1.20배)
    public float[] pathB_CompoundRates = { 1.10f, 1.15f, 1.20f };

    [Header("진화 C (Last Train Home)")]
    public StatusEffectData timeBombEffect;
    //  [신규 추가] 레벨별 시한폭탄 스냅샷 증폭 배율 (2.0배, 2.5배, 3.0배)
    public float[] pathC_DamageMults = { 2.0f, 2.5f, 3.0f };


    public override bool AlwaysHits(SkillData skill)
    {
        if (skill.currentEvolution == SkillEvolution.PathC) return true;
        return base.AlwaysHits(skill);
    }

    // 1. 기본 데미지 & 진화 C 단타(1데미지) 처리
    public override float GetDamageMultiplier(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        // [진화 C] 시한폭탄 설치를 위해 데미지 배율을 0으로 만듭니다.
        if (skill.currentEvolution == SkillEvolution.PathC) return 0f;

        bool isTargetBroken = BreakManager.Instance.IsBroken(!isPlayerAttacking);
        if (isTargetBroken)
        {
            int index = Mathf.Clamp(skill.skillLevel - 1, 0, bonusDamageRatesOnBreak.Length - 1);
            return 1.0f + bonusDamageRatesOnBreak[index];
        }
        return 1.0f;
    }

    // 2. [진화 B] 스탠드 프라우드 (복리 증가)
    public override float GetDynamicDamageMultiplier(SkillData skill, int consecutiveHits)
    {
        if (skill.currentEvolution == SkillEvolution.PathB)
        {
            //  [수정됨] 스킬 레벨에 따라 복리 배율 적용!
            int index = Mathf.Clamp(skill.skillLevel - 1, 0, pathB_CompoundRates.Length - 1);
            float compoundRate = pathB_CompoundRates[index];

            return Mathf.Pow(compoundRate, consecutiveHits);
        }
        return 1.0f;
    }

    // 3. [진화 C] 라스트 트레인 홈 (시한폭탄 스냅샷 장전)
    public override void PaySkillCost(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        if (skill.currentEvolution == SkillEvolution.PathC && isPlayerAttacking)
        {
            //  [수정됨] 스킬 레벨에 따라 스냅샷 데미지 폭발 배율 적용!
            int index = Mathf.Clamp(skill.skillLevel - 1, 0, pathC_DamageMults.Length - 1);
            float snapshotMult = pathC_DamageMults[index];

            float skillMult = skill.GetCurrentDamageMultiplier();
            int hits = skill.GetCurrentHitCount();
            if (hits <= 1) hits = 10;

            int def = StatManager.Instance.GetEffectiveStat(false, TargetStat.Defense);
            float dr = CombatMath.GetDamageReduction(def);

            float rawDmg = (pStats.strength * skillMult) * (1f - dr);
            int totalDmg = Mathf.RoundToInt(rawDmg * hits * snapshotMult); // <- 배율 곱산

            CombatManager.Instance.savedBombDamage = totalDmg;
            CombatManager.Instance.isBombActive = true;

            if (timeBombEffect != null)
            {
                BuffManager.Instance.AddEffect(false, timeBombEffect, totalDmg, 1);
            }

            DevLog.Log($"[진화 C] 라스트 트레인 홈(Lv.{skill.skillLevel}) 장전! 배율 {snapshotMult}x -> {totalDmg} 피해 대기 중.");
        }
    }

    // 4. [진화 C] 타수 변환 (단타로)
    public override int GetHitCount(SkillData skill)
    {
        if (skill.currentEvolution == SkillEvolution.PathC) return 1;
        return base.GetHitCount(skill);
    }

    // 5. [진화 A] 블러디 스트림 (타수 비례 디버프 적용)
    public override void ApplyEffectOnHit(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking, bool isHit)
    {
        if (!isHit || !isPlayerAttacking) return;

        if (skill.currentEvolution == SkillEvolution.PathA)
        {
            //  [수정됨] 스킬 레벨에 따라 1타당 디버프 수치 적용!
            int index = Mathf.Clamp(skill.skillLevel - 1, 0, pathA_DebuffPerHitRates.Length - 1);
            float debuffPerHit = pathA_DebuffPerHitRates[index];

            int hitCount = CombatManager.Instance.lastSuccessfulHits;
            float totalDebuff = -(debuffPerHit * hitCount);

            if (defDownEffect != null) BuffManager.Instance.AddEffect(false, defDownEffect, totalDebuff, 3);
            if (brDownEffect != null) BuffManager.Instance.AddEffect(false, brDownEffect, totalDebuff, 3);

            DevLog.Log($"[진화 A] 블러디 스트림(Lv.{skill.skillLevel})! {hitCount}타 적중. 방어력/BR을 {Mathf.Abs(totalDebuff) * 100}% 감소시킵니다.");
        }
    }
}