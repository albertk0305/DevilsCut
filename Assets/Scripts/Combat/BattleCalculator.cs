using UnityEngine;
using System.Collections.Generic;

public struct HitResult
{
    public bool isHit;
    public bool isCrit;
    public int damage;
    public float breakDamage;
}

public class SkillResult
{
    public List<HitResult> hits = new List<HitResult>();
    public bool anyHit = false;
    public bool anyCrit = false;
    public bool isGuardTriggered = false;
    public int totalMitigatedDamage = 0;
}

// Pure combat math only; UI and presentation are handled elsewhere.
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

        var guardEffect = BuffManager.Instance.GetEffects(!isPlayerAttacking)
            .Find(e => e.effectData.specialType == SpecialEffectType.Guard || e.effectData.specialType == SpecialEffectType.AbsoluteGuard);

        float defenderExtraEvasion = 0f;
        var defenderEffects = BuffManager.Instance.GetEffects(!isPlayerAttacking);
        foreach (var effect in defenderEffects)
        {
            if (effect.effectData.specialType == SpecialEffectType.EvasionUp)
            {
                defenderExtraEvasion += effect.value;
            }
        }

        if (!isPlayerAttacking) defenderExtraEvasion += pStats.bonusEvasion;

        int consecutiveHits = 0;

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

                if (isPlayerAttacking) finalAccuracy += pStats.bonusAccuracy;

                // Accuracy modifiers apply to whichever side is attacking.
                var attackerEffectsForAcc = BuffManager.Instance.GetEffects(isPlayerAttacking);
                foreach (var eff in attackerEffectsForAcc)
                {
                    if (eff.effectData != null && eff.effectData.specialType == SpecialEffectType.AccuracyUp)
                        finalAccuracy += eff.value;
                }

                hit.isHit = CombatMath.CheckHitSuccess(finalAccuracy, attackerSpeed, defenderSpeed, defenderExtraEvasion);
            }

            if (hit.isHit)
            {
                result.anyHit = true;

                float baseMult = skill.GetCurrentDamageMultiplier();
                float logicMult = skill.skillLogic != null ? skill.skillLogic.GetDamageMultiplier(skill, pStats, eData, isPlayerAttacking) : 1f;
                bool forceAttackSkill = skill.skillLogic != null && skill.skillLogic.TreatAsAttackSkill(skill);

                // Utility skills become attacks when their logic provides a real damage multiplier.
                bool isAttackSkill = forceAttackSkill || baseMult > 0f || (baseMult <= 0f && logicMult > 0f && logicMult != 1.0f);

                if (baseMult <= 0f && isAttackSkill) baseMult = 1.0f;

                float calculatedDamage = attackerStrength * baseMult;
                float currentBreakPower = skill.GetCurrentBreakPower();

                // Path C can replace the base damage and break formula.
                if (skill.skillLogic != null &&
                    skill.skillLogic.TryOverrideBaseHitCalculation(
                        skill,
                        attackerStrength,
                        attackerDefense,
                        out float overrideDamage,
                        out float overrideBreakPower))
                {
                    calculatedDamage = overrideDamage;
                    currentBreakPower = overrideBreakPower;
                }

                if (skill.skillLogic != null)
                {
                    calculatedDamage *= logicMult;
                    calculatedDamage *= skill.skillLogic.GetDynamicDamageMultiplier(skill, consecutiveHits);
                }

                if (isPlayerAttacking)
                    calculatedDamage *= StyleRankManager.Instance.GetRankDamageMultiplier();

                if (!isPlayerAttacking && BreakManager.Instance.IsBroken(true)) calculatedDamage *= 2.0f;
                else if (isPlayerAttacking && BreakManager.Instance.IsBroken(false)) calculatedDamage *= 2.0f;

                if (isAttackSkill)
                {
                    float dynamicCrit = skill.skillLogic != null ? skill.skillLogic.GetDynamicCritRateBonus(skill, consecutiveHits) : 0f;

                    float totalCritRateBonus = skill.GetCurrentBonusCritRate() + dynamicCrit;
                    if (isPlayerAttacking)
                    {
                        totalCritRateBonus += (pStats.critRate * 100f);

                        var attackerEffectsForCrit = BuffManager.Instance.GetEffects(true);
                        foreach (var eff in attackerEffectsForCrit)
                            if (eff.effectData != null && eff.effectData.specialType == SpecialEffectType.CritRateUp) totalCritRateBonus += eff.value;
                    }

                    hit.isCrit = skill.skillLogic != null && skill.skillLogic.AlwaysCrits(skill)
                        ? true
                        : CombatMath.CheckCriticalSuccess(totalCritRateBonus, attackerLuck);
                    if (hit.isCrit)
                    {
                        float baseCritMult = skill.skillLogic != null ? skill.skillLogic.GetCritDamageMultiplier(skill) : 1.5f;

                        float finalCritMult = baseCritMult;
                        if (isPlayerAttacking)
                        {
                            finalCritMult += (pStats.critDamage - 1.5f);

                            var attackerEffectsForCritDmg = BuffManager.Instance.GetEffects(true);
                            foreach (var eff in attackerEffectsForCritDmg)
                                if (eff.effectData != null && eff.effectData.specialType == SpecialEffectType.CritDamageUp) finalCritMult += eff.value;
                        }

                        calculatedDamage *= finalCritMult;
                        result.anyCrit = true;
                    }
                }
                else
                {
                    hit.isCrit = false;
                }

                // Damage amplification is combined before defense and reduction.
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
                    ItemSynergyBalanceData synergyBalance = ItemSynergyBalanceData.Resolve();

                    // Saber epic: bonus damage against enemies at 70% HP or higher.
                    if (defenderMaxHp > 0 && ((float)defenderCurrentHp / defenderMaxHp) >= 0.7f)
                    {
                        var saberEpics = inventory.FindAll(x => x.data.itemClass == ItemClass.Saber && x.data.grade == ItemGrade.Epic);
                        foreach (var saberEpic in saberEpics)
                        {
                            if (saberEpic.starLevel == 1) damageGivenAmp += 0.04f;
                            else if (saberEpic.starLevel == 2) damageGivenAmp += 0.15f;
                            else if (saberEpic.starLevel >= 3) damageGivenAmp += 0.50f;
                        }
                    }

                    int apDiff = pStats.ActionPoints - eData.ActionPoints;

                    if (apDiff > 0)
                    {
                        if (syn.GetValueOrDefault(ItemClass.Assassin) >= 4)
                        {
                            damageGivenAmp += (apDiff * 0.0015f);
                        }

                        var assassinEpics = inventory.FindAll(x => x.data.itemClass == ItemClass.Assassin && x.data.grade == ItemGrade.Epic);
                        foreach (var assassinEpic in assassinEpics)
                        {
                            if (assassinEpic.starLevel == 1) damageGivenAmp += (apDiff * 0.0004f);
                            else if (assassinEpic.starLevel == 2) damageGivenAmp += (apDiff * 0.0015f);
                            else if (assassinEpic.starLevel >= 3) damageGivenAmp += (apDiff * 0.005f);
                        }
                    }

                    int spdDiff = attackerSpeed - defenderSpeed;

                    if (spdDiff > 0)
                    {
                        var boxerEpics = inventory.FindAll(x => x.data.itemClass == ItemClass.Boxer && x.data.grade == ItemGrade.Epic);
                        foreach (var boxerEpic in boxerEpics)
                        {
                            if (boxerEpic.starLevel == 1) damageGivenAmp += (spdDiff * 0.0004f);
                            else if (boxerEpic.starLevel == 2) damageGivenAmp += (spdDiff * 0.0015f);
                            else if (boxerEpic.starLevel >= 3) damageGivenAmp += (spdDiff * 0.005f);
                        }
                    }

                    int activeBuffCount = 0;
                    foreach (var eff in attackerEffects)
                    {
                        if (eff.effectData != null && eff.effectData.category == EffectCategory.Buff) activeBuffCount++;
                    }

                    if (activeBuffCount > 0)
                    {
                        if (syn.GetValueOrDefault(ItemClass.Caster) >= 6)
                            damageGivenAmp += (activeBuffCount * synergyBalance.caster6DamageAmpPerBuff);

                        var casterLegendary = inventory.Find(x => x.data.itemClass == ItemClass.Caster && x.data.grade == ItemGrade.Legendary);
                        if (casterLegendary != null)
                            damageGivenAmp += (activeBuffCount * synergyBalance.casterLegendaryDamageAmpPerBuff);
                    }

                    int activeDebuffCount = 0;
                    foreach (var eff in defenderEffects)
                    {
                        if (eff.effectData != null && eff.effectData.category == EffectCategory.Debuff) activeDebuffCount++;
                    }

                    if (activeDebuffCount > 0)
                    {
                        if (syn.GetValueOrDefault(ItemClass.Trickster) >= 6)
                            damageGivenAmp += (activeDebuffCount * synergyBalance.trickster6DamageAmpPerDebuff);

                        var tricksterLegendary = inventory.Find(x => x.data.itemClass == ItemClass.Trickster && x.data.grade == ItemGrade.Legendary);
                        if (tricksterLegendary != null)
                            damageGivenAmp += (activeDebuffCount * synergyBalance.tricksterLegendaryDamageAmpPerDebuff);
                    }

                    if (syn.GetValueOrDefault(ItemClass.Berserker) >= 4)
                    {
                        damageGivenAmp += (CombatMath.GetMissingHPMultiplier(pStats.maxHp, pStats.currentHp, 0.20f) - 1.0f);
                    }

                    var berserkerRares = inventory.FindAll(x => x.data.itemClass == ItemClass.Berserker && x.data.grade == ItemGrade.Rare);
                    foreach (var bRare in berserkerRares)
                    {
                        float maxBonus = bRare.starLevel == 1 ? 0.05f : (bRare.starLevel == 2 ? 0.20f : 0.80f);
                        damageGivenAmp += (CombatMath.GetMissingHPMultiplier(pStats.maxHp, pStats.currentHp, maxBonus) - 1.0f);
                    }
                }
                if (isPlayerAttacking) damageGivenAmp += pStats.finalDamageAmp;

                foreach (var eff in defenderEffects)
                {
                    if (eff.effectData.specialType == SpecialEffectType.DamageAmp) damageAmp += eff.value;
                    if (eff.effectData.specialType == SpecialEffectType.DamageReduction) damageReduction += eff.value;
                }
                if (!isPlayerAttacking) damageReduction += pStats.finalDamageReduction;

                float totalAmp = damageAmp + damageGivenAmp;
                if (totalAmp > 0f) calculatedDamage *= (1f + totalAmp);


                // Split fixed damage before applying defense and damage reduction.
                float armorPenRatio = skill.skillLogic != null ? skill.skillLogic.GetArmorPenetrationRatio(skill, skill.skillLevel) : 0f;
                if (isPlayerAttacking) armorPenRatio += pStats.trueDamageConversion;

                armorPenRatio = Mathf.Clamp01(armorPenRatio);

                float fixedDamage = calculatedDamage * armorPenRatio;
                float normalDamage = calculatedDamage * (1f - armorPenRatio);

                // Defense and reduction apply only to normal damage.
                normalDamage *= (1f - CombatMath.GetDamageReduction(defenderDefense));

                if (damageReduction > 0f) normalDamage *= (1f - Mathf.Clamp01(damageReduction));


                // Snapshot pre-guard damage for reflection effects.
                int originalDamage = Mathf.RoundToInt(fixedDamage + normalDamage);
                if (originalDamage <= 0) originalDamage = 1;


                if (guardEffect != null && (normalDamage + fixedDamage) > 0)
                {
                    float reductionRate = guardEffect.value > 0f ? guardEffect.value : 0.5f;

                    // Guard reduces normal damage; AbsoluteGuard also reduces fixed damage.
                    normalDamage *= (1f - reductionRate);

                    if (guardEffect.effectData.specialType == SpecialEffectType.AbsoluteGuard)
                        fixedDamage *= (1f - reductionRate);

                    result.isGuardTriggered = true;
                }

                calculatedDamage = fixedDamage + normalDamage;
                hit.damage = Mathf.RoundToInt(calculatedDamage);

                var invincibleEffect = defenderEffects.Find(e => e.effectData.specialType == SpecialEffectType.Invincible);

                if (invincibleEffect != null)
                {
                    hit.damage = 0;
                }
                else if (isAttackSkill)
                {
                    if (hit.damage <= 0) hit.damage = 1;
                }
                else
                {
                    hit.damage = 0;
                }

                if (result.isGuardTriggered) result.totalMitigatedDamage += (originalDamage - hit.damage);

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
