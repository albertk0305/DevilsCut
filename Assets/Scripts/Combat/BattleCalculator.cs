using UnityEngine;
using System.Collections.Generic;

// [분리] CombatManager에 있던 구조체를 이곳으로 이사!
public struct HitResult
{
    public bool isHit;
    public bool isCrit;
    public int damage;
    public float breakDamage;
}

// 스킬 한 번 사용(다단 히트 포함)의 종합 결과를 담는 데이터 통
public class SkillResult
{
    public List<HitResult> hits = new List<HitResult>();
    public bool anyHit = false;
    public bool anyCrit = false;
    public bool isGuardTriggered = false;
    public int totalMitigatedDamage = 0;
}

// [핵심] 오직 '수학적 계산'만 전담하며, UI나 이펙트에는 전혀 관여하지 않습니다.
public static class BattleCalculator
{
    public static SkillResult CalculateSkill(
        SkillData skill, bool isPlayerAttacking,
        PlayerStats pStats, EnemyData eData,
        int attackerStrength, int attackerDefense, int attackerLuck, int attackerSpeed,
        int defenderDefense, int defenderSpeed, int defenderBR, int defenderCurrentHp, int defenderMaxHp)
    {
        SkillResult result = new SkillResult();
        int totalHits = skill.skillLogic != null ? skill.skillLogic.GetHitCount(skill) : skill.GetCurrentHitCount();

        // 방어자의 가드 버프 찾기 (확장성을 위해 아군/적군 모두 판정 가능하게 구현)
        var guardEffect = BuffManager.Instance.GetEffects(!isPlayerAttacking)
            .Find(e => e.effectData.specialType == SpecialEffectType.Guard || e.effectData.specialType == SpecialEffectType.AbsoluteGuard);

        float defenderExtraEvasion = 0f;
        var defenderEffects = BuffManager.Instance.GetEffects(!isPlayerAttacking);
        foreach (var effect in defenderEffects)
        {
            if (effect.effectData.specialType == SpecialEffectType.EvasionUp)
            {
                defenderExtraEvasion += effect.value; // 예: 20, 30, 40 합산
            }
        }

        int consecutiveHits = 0; // 연속 적중 카운터 초기화

        bool isDefenderBroken = BreakManager.Instance.IsBroken(!isPlayerAttacking);

        for (int i = 0; i < totalHits; i++)
        {
            HitResult hit = new HitResult();
            bool isAlwaysHit = skill.skillLogic != null && skill.skillLogic.AlwaysHits(skill);
            bool isForcedMiss = skill.skillLogic != null && skill.skillLogic.AlwaysMisses(skill, i);

            if (isForcedMiss)
            {
                hit.isHit = false; 
            }
            else if (isAlwaysHit || isDefenderBroken)
            {
                hit.isHit = true;
            }
            else
            {
                float currentBaseAccuracy = skill.skillLogic != null ? skill.skillLogic.GetBaseAccuracy(skill) : skill.baseAccuracy;

                float finalAccuracy = currentBaseAccuracy + skill.GetCurrentBonusAccuracy();
                hit.isHit = CombatMath.CheckHitSuccess(finalAccuracy, attackerSpeed, defenderSpeed, defenderExtraEvasion);
            }

            if (hit.isHit)
            {
                result.anyHit = true;

                // 1. 기본 데미지 및 브레이크 수치 산출
                float baseMult = skill.GetCurrentDamageMultiplier();
                float logicMult = skill.skillLogic != null ? skill.skillLogic.GetDamageMultiplier(skill, pStats, eData, isPlayerAttacking) : 1f;

                // 원본이 버프(baseMult가 0)라도, 로직에서 계수를 줬다면 이 스킬은 이제 공격기(isAttackSkill)입니다!
                bool isAttackSkill = baseMult > 0f || (baseMult <= 0f && logicMult > 0f && logicMult != 1.0f);

                if (baseMult <= 0f && isAttackSkill) baseMult = 1.0f; // 공격기로 확인된 놈만 배율을 살려줍니다.

                // 1. 기본 데미지 및 브레이크 수치 산출
                float calculatedDamage = attackerStrength * baseMult;
                float currentBreakPower = skill.GetCurrentBreakPower();

                // Path C (제물의 낙인) 특수 공식 적용
                if (skill.currentEvolution == SkillEvolution.PathC && skill.skillLogic is SkillLogic_Courage)
                {
                    int combinedStat = attackerStrength + attackerDefense;
                    int cIndex = Mathf.Clamp(skill.skillLevel - 1, 0, skill.evolutionC_DamageMultipliers.Length - 1);
                    calculatedDamage = combinedStat * skill.evolutionC_DamageMultipliers[cIndex];
                    currentBreakPower = skill.evolutionC_BreakPowers[cIndex];
                }

                // 2. 외부 보정 (스킬 고유 로직, 스타일 랭크, 그로기 증폭)
                if (skill.skillLogic != null)
                {
                    calculatedDamage *= logicMult; // 여기서 위에서 계산해둔 logicMult를 곱해 최종 데미지를 뻥튀기합니다!
                    calculatedDamage *= skill.skillLogic.GetDynamicDamageMultiplier(skill, consecutiveHits);
                }

                if (isPlayerAttacking)
                    calculatedDamage *= StyleRankManager.Instance.GetRankDamageMultiplier();

                if (!isPlayerAttacking && BreakManager.Instance.IsBroken(true)) calculatedDamage *= 2.0f;
                else if (isPlayerAttacking && BreakManager.Instance.IsBroken(false)) calculatedDamage *= 2.0f;

                // 3. 크리티컬 판정
                if (isAttackSkill)
                {
                    float dynamicCrit = skill.skillLogic != null ? skill.skillLogic.GetDynamicCritRateBonus(skill, consecutiveHits) : 0f;

                    float totalCritRateBonus = skill.GetCurrentBonusCritRate() + dynamicCrit;
                    if (isPlayerAttacking) totalCritRateBonus += (pStats.critRate * 100f);

                    hit.isCrit = CombatMath.CheckCriticalSuccess(totalCritRateBonus, attackerLuck);
                    if (hit.isCrit)
                    {
                        float baseCritMult = skill.skillLogic != null ? skill.skillLogic.GetCritDamageMultiplier(skill) : 1.5f;

                        float finalCritMult = baseCritMult;
                        if (isPlayerAttacking) finalCritMult += (pStats.critDamage - 1.5f);

                        calculatedDamage *= finalCritMult;
                        result.anyCrit = true;
                    }
                }
                else
                {
                    hit.isCrit = false;
                }

                // ==========================================================
                // 4. 피해 증폭(Amp) 수치 합산 및 선적용
                // ==========================================================
                float damageAmp = 0f;
                float damageGivenAmp = 0f;
                float damageReduction = 0f;

                var attackerEffects = BuffManager.Instance.GetEffects(isPlayerAttacking);
                foreach (var eff in attackerEffects)
                {
                    if (eff.effectData.specialType == SpecialEffectType.DamageGivenAmp) damageGivenAmp += eff.value;
                }

                if (isPlayerAttacking && PlayerManager.Instance != null)
                {
                    var syn = PlayerManager.Instance.GetCurrentSynergies();
                    var inventory = PlayerManager.Instance.inventory;

                    // [세이버 에픽 아이템 - 포식자의 이빨] 적 체력이 70% 이상일 때 최종 피해 증폭
                    if (defenderMaxHp > 0 && ((float)defenderCurrentHp / defenderMaxHp) >= 0.7f)
                    {
                        var saberEpic = inventory.Find(x => x.data.itemClass == ItemClass.Saber && x.data.grade == ItemGrade.Epic);
                        if (saberEpic != null)
                        {
                            if (saberEpic.starLevel == 1) damageGivenAmp += 0.04f;
                            else if (saberEpic.starLevel == 2) damageGivenAmp += 0.20f;
                            else if (saberEpic.starLevel >= 3) damageGivenAmp += 1.00f;
                        }
                    }
                }
                if (isPlayerAttacking) damageGivenAmp += pStats.finalDamageAmp;

                // (defenderEffects는 상단에 이미 var로 선언된 변수를 그대로 재사용합니다)
                foreach (var eff in defenderEffects)
                {
                    if (eff.effectData.specialType == SpecialEffectType.DamageAmp) damageAmp += eff.value;
                    if (eff.effectData.specialType == SpecialEffectType.DamageReduction) damageReduction += eff.value;
                }
                if (!isPlayerAttacking) damageReduction += pStats.finalDamageReduction;

                //  [버그 픽스] 주는 피해 증폭과 받는 피해 증폭을 합산(totalAmp)하여 전체 파이를 먼저 키웁니다!
                float totalAmp = damageAmp + damageGivenAmp;
                if (totalAmp > 0f) calculatedDamage *= (1f + totalAmp);


                // ==========================================================
                // 5. 고정 피해 분할 및 방어력 / 피해 감소(Reduction) 분리 적용
                // ==========================================================
                float armorPenRatio = skill.skillLogic != null ? skill.skillLogic.GetArmorPenetrationRatio(skill, skill.skillLevel) : 0f;
                if (isPlayerAttacking) armorPenRatio += pStats.trueDamageConversion;

                armorPenRatio = Mathf.Clamp01(armorPenRatio); // 최대 100%까지만 제한

                float fixedDamage = calculatedDamage * armorPenRatio;
                float normalDamage = calculatedDamage * (1f - armorPenRatio);

                // 일반 피해에만 스탯 방어력(Defense DR) 감쇄 적용
                normalDamage *= (1f - CombatMath.GetDamageReduction(defenderDefense));

                //  [핵심 수정] 일반 피해에만 데미지 감소(Damage Reduction) 버프/스탯 적용 (고정 피해는 절대 깎이지 않음!)
                if (damageReduction > 0f) normalDamage *= (1f - Mathf.Clamp01(damageReduction));


                // ==========================================================
                // 6. 인과율(반사)을 위한 '가드 직전 데미지' 스냅샷 저장
                // ==========================================================
                int originalDamage = Mathf.RoundToInt(fixedDamage + normalDamage);
                if (originalDamage <= 0) originalDamage = 1;


                // ==========================================================
                // 7. 가드(Guard) 및 갓 핸드(AbsoluteGuard) 방어 적용
                // ==========================================================
                if (guardEffect != null && (normalDamage + fixedDamage) > 0)
                {
                    float reductionRate = guardEffect.value > 0f ? guardEffect.value : 0.5f;

                    // 가드 역시 기본적으로 일반 피해만 줄입니다.
                    normalDamage *= (1f - reductionRate);

                    // 절대 가드(AbsoluteGuard) 효과일 때만 예외적으로 고정 피해도 막아냅니다.
                    if (guardEffect.effectData.specialType == SpecialEffectType.AbsoluteGuard)
                        fixedDamage *= (1f - reductionRate);

                    result.isGuardTriggered = true;
                }

                // 최종 데미지 합산
                calculatedDamage = fixedDamage + normalDamage;
                hit.damage = Mathf.RoundToInt(calculatedDamage);

                var invincibleEffect = defenderEffects.Find(e => e.effectData.specialType == SpecialEffectType.Invincible);

                if (invincibleEffect != null)
                {
                    hit.damage = 0; // 무적이면 최소 데미지 무시하고 무조건 0!
                }
                else if (isAttackSkill)
                {
                    if (hit.damage <= 0) hit.damage = 1; // 공격기면 최소 데미지 1 보장
                }
                else
                {
                    hit.damage = 0; // 순수 버프/디버프일 때만 데미지 0 고정
                }

                if (result.isGuardTriggered) result.totalMitigatedDamage += (originalDamage - hit.damage);

                // 8. 브레이크 데미지 연산
                if (isPlayerAttacking && !BreakManager.Instance.IsBroken(false))
                {
                    hit.breakDamage = currentBreakPower * (skill.skillLogic != null ? skill.skillLogic.GetBreakMultiplier(skill, pStats, eData, isPlayerAttacking) : 1f);
                    hit.breakDamage *= (1f - CombatMath.GetBreakDamageReduction(defenderBR));
                }
                else if (!isPlayerAttacking && !BreakManager.Instance.IsBroken(true))
                {
                    hit.breakDamage = currentBreakPower * (skill.skillLogic != null ? skill.skillLogic.GetBreakMultiplier(skill, pStats, eData, isPlayerAttacking) : 1f);
                    hit.breakDamage *= (1f - CombatMath.GetBreakDamageReduction(defenderBR));
                }
                else hit.breakDamage = 0f;

                if (invincibleEffect != null) hit.breakDamage = 0f;
                else if (result.isGuardTriggered) hit.breakDamage = 0f;
                consecutiveHits++;
            }
            else
            {
                consecutiveHits = 0;
            }
            result.hits.Add(hit);
        }
        return result;
    }
}