using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Crusader", menuName = "SkillLogic/Player/Crusader")]
public class SkillLogic_Crusader : SkillLogicBase
{
    [Header("기본: 레벨별 그로기 추가 데미지 증폭률")]
    public float[] bonusDamageRatesOnBreak = { 0.50f, 0.75f, 1.0f };

    [Header("진화 A (Bloody Stream)")]
    public StatusEffectData defDownEffect; // 방어력 감소 디버프 (인스펙터 할당 필요)
    public StatusEffectData brDownEffect;  // BR 감소 디버프 (인스펙터 할당 필요)
    public float pathA_DebuffPerHit = 0.03f; // 1타당 3% 감소 (10타면 30%)

    [Header("진화 C (Last Train Home)")]
    public StatusEffectData timeBombEffect;

    public override bool AlwaysHits(SkillData skill)
    {
        if (skill.currentEvolution == SkillEvolution.PathC) return true;
        return base.AlwaysHits(skill);
    }

    // 1. 기본 데미지 & 진화 C 단타(1데미지) 처리
    public override float GetDamageMultiplier(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        // [진화 C] 시한폭탄 설치를 위해 데미지 배율을 0으로 만듭니다. (시스템이 알아서 1데미지로 보정해 줍니다!)
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
            // consecutiveHits는 0부터 시작하므로 첫 타는 1.15^0 = 1.0 (기본딜)
            // 명중할 때마다 1.15배씩 복리로 증가! (빗나가면 BattleCalculator가 알아서 0스택으로 리셋해 줍니다!)
            return Mathf.Pow(1.15f, consecutiveHits);
        }
        return 1.0f;
    }

    // 3. [진화 C] 라스트 트레인 홈 (시한폭탄 스냅샷 장전)
    public override void PaySkillCost(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        if (skill.currentEvolution == SkillEvolution.PathC && isPlayerAttacking)
        {
            float skillMult = skill.GetCurrentDamageMultiplier();
            int hits = skill.GetCurrentHitCount();
            if (hits <= 1) hits = 10; // 만약 SkillData에 타수가 없다면 10타로 기본 가정

            int def = StatManager.Instance.GetEffectiveStat(false, TargetStat.Defense);
            float dr = CombatMath.GetDamageReduction(def);

            // [핵심] 현재 스탯 기반으로 총 데미지 스냅샷 저장 (2.5배 증폭)
            float rawDmg = (pStats.strength * skillMult) * (1f - dr);
            int totalDmg = Mathf.RoundToInt(rawDmg * hits * 2.5f);

            CombatManager.Instance.savedBombDamage = totalDmg;
            CombatManager.Instance.isBombActive = true;

            if (timeBombEffect != null)
            {
                BuffManager.Instance.AddEffect(false, timeBombEffect, totalDmg, 1);
            }

            DevLog.Log($"[진화 C] 라스트 트레인 홈 장전! 다음 적 턴에 {totalDmg} 피해 대기 중.");
        }
    }

    // 4. [진화 C] 타수 변환 (단타로)
    public override int GetHitCount(SkillData skill)
    {
        if (skill.currentEvolution == SkillEvolution.PathC) return 1; // 톡 치고 빠지기 위해 1타로 변경
        return base.GetHitCount(skill);
    }

    // 5. [진화 A] 블러디 스트림 (타수 비례 디버프 적용)
    public override void ApplyEffectOnHit(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking, bool isHit)
    {
        if (!isHit || !isPlayerAttacking) return;

        if (skill.currentEvolution == SkillEvolution.PathA)
        {
            // CombatManager에 기록된 성공 타수를 가져옵니다.
            int hitCount = CombatManager.Instance.lastSuccessfulHits;
            float totalDebuff = -(pathA_DebuffPerHit * hitCount); // 3% * 적중 타수

            if (defDownEffect != null) BuffManager.Instance.AddEffect(false, defDownEffect, totalDebuff, 3);
            if (brDownEffect != null) BuffManager.Instance.AddEffect(false, brDownEffect, totalDebuff, 3);

            DevLog.Log($"[진화 A] 블러디 스트림! {hitCount}타 적중. 방어력/BR을 {Mathf.Abs(totalDebuff) * 100}% 감소시킵니다.");
        }
    }
}