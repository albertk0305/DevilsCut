using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "EnemyAI_Sariel", menuName = "EnemyAI/Sariel Boss AI")]
public class EnemyAI_Sariel : EnemyAIBase
{
    [SerializeField] private SkillData stoneOcean;
    [SerializeField] private SkillData shynessBoy;
    [SerializeField] private SkillData rememberSummerDays;
    [SerializeField] private SkillData heavensFallingDown;
    [SerializeField] private SkillData windySummer;

    [SerializeField] private StatusEffectData passiveDamageGivenAmp;

    private int patternIndex;

    public override EnemyActionIntent DecideNextAction(int currentTurnCount, PlayerStats pStats, EnemyData enemy)
    {
        UpdatePassives(enemy);

        EnemyActionIntent intent = new EnemyActionIntent();
        SkillData intendedSkill = GetPatternSkill();

        intent.skillToUse = intendedSkill != null ? intendedSkill : GetFallbackSkill();
        patternIndex = (patternIndex + 1) % 5;

        if (intent.skillToUse == null)
        {
            DevLog.LogWarning("[Sariel AI] No usable skill is assigned.");
        }

        return intent;
    }

    public override List<SkillData> GetEnemySkills()
    {
        List<SkillData> skillList = new List<SkillData>();

        if (stoneOcean != null) skillList.Add(stoneOcean);
        if (shynessBoy != null) skillList.Add(shynessBoy);
        if (rememberSummerDays != null) skillList.Add(rememberSummerDays);
        if (heavensFallingDown != null) skillList.Add(heavensFallingDown);
        if (windySummer != null) skillList.Add(windySummer);

        return skillList;
    }

    public override void UpdatePassives(EnemyData enemy)
    {
        if (passiveDamageGivenAmp == null)
        {
            DevLog.LogWarning("[Sariel] passiveDamageGivenAmp is not assigned.");
            return;
        }

        if (BuffManager.Instance == null) return;

        var enemyEffects = BuffManager.Instance.GetEffects(false);
        enemyEffects.RemoveAll(e => e.effectData == passiveDamageGivenAmp);

        int buffCount = 0;
        foreach (var effect in enemyEffects)
        {
            if (effect.effectData == null) continue;
            if (effect.effectData == passiveDamageGivenAmp) continue;
            if (effect.effectData.isPermanentPassive) continue;
            if (effect.effectData.category == EffectCategory.Buff) buffCount++;
        }

        float ampValue = buffCount * 0.05f;
        if (ampValue > 0f)
        {
            BuffManager.Instance.AddEffect(false, passiveDamageGivenAmp, ampValue, 999);
        }

        if (CombatUIManager.Instance != null) CombatUIManager.Instance.RefreshBuffUI();
    }

    private SkillData GetPatternSkill()
    {
        switch (patternIndex)
        {
            case 0:
                return stoneOcean;
            case 1:
                return shynessBoy;
            case 2:
                return rememberSummerDays;
            case 3:
                return heavensFallingDown;
            default:
                return windySummer;
        }
    }

    private SkillData GetFallbackSkill()
    {
        if (stoneOcean != null) return stoneOcean;
        if (shynessBoy != null) return shynessBoy;
        if (rememberSummerDays != null) return rememberSummerDays;
        if (heavensFallingDown != null) return heavensFallingDown;
        if (windySummer != null) return windySummer;

        return null;
    }
}
