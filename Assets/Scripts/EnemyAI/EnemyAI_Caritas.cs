using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "EnemyAI_Caritas", menuName = "EnemyAI/Caritas AI")]
public class EnemyAI_Caritas : EnemyAIBase
{
    [SerializeField] private SkillData iCantStopTheLoneliness;
    [SerializeField] private SkillData flydayChinatown;

    [SerializeField] private StatusEffectData passiveDamageGivenAmp;

    private int patternIndex;

    public override EnemyActionIntent DecideNextAction(int currentTurnCount, PlayerStats pStats, EnemyData enemy)
    {
        UpdatePassives(enemy);

        EnemyActionIntent intent = new EnemyActionIntent();

        SkillData intendedSkill = (patternIndex == 0) ? iCantStopTheLoneliness : flydayChinatown;
        SkillData fallbackSkill = (intendedSkill == iCantStopTheLoneliness) ? flydayChinatown : iCantStopTheLoneliness;

        intent.skillToUse = intendedSkill != null ? intendedSkill : fallbackSkill;
        patternIndex = (patternIndex + 1) % 2;

        if (intent.skillToUse == null)
        {
            DevLog.LogWarning("[Caritas AI] No usable skill is assigned.");
        }

        return intent;
    }

    public override List<SkillData> GetEnemySkills()
    {
        List<SkillData> skillList = new List<SkillData>();

        if (iCantStopTheLoneliness != null) skillList.Add(iCantStopTheLoneliness);
        if (flydayChinatown != null) skillList.Add(flydayChinatown);

        return skillList;
    }

    public override void UpdatePassives(EnemyData enemy)
    {
        if (passiveDamageGivenAmp == null)
        {
            DevLog.LogWarning("[Caritas] passiveDamageGivenAmp is not assigned.");
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
}
