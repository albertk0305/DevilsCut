using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "EnemyAI_Diligere", menuName = "EnemyAI/Diligere AI")]
public class EnemyAI_Diligere : EnemyAIBase
{
    [SerializeField] private SkillData vagueHope;
    [SerializeField] private SkillData wretchedWeaponry;

    [SerializeField] private StatusEffectData passiveApBuff;

    private bool passiveApplied;
    private int patternIndex;

    public override EnemyActionIntent DecideNextAction(int currentTurnCount, PlayerStats pStats, EnemyData enemy)
    {
        if (!passiveApplied) ApplyPassive();

        EnemyActionIntent intent = new EnemyActionIntent();

        SkillData intendedSkill = (patternIndex == 0) ? vagueHope : wretchedWeaponry;
        SkillData fallbackSkill = (intendedSkill == vagueHope) ? wretchedWeaponry : vagueHope;

        intent.skillToUse = intendedSkill != null ? intendedSkill : fallbackSkill;
        patternIndex = (patternIndex + 1) % 2;

        if (intent.skillToUse == null)
        {
            DevLog.LogWarning("[Diligere AI] No usable skill is assigned.");
        }

        return intent;
    }

    public override List<SkillData> GetEnemySkills()
    {
        List<SkillData> skillList = new List<SkillData>();

        if (vagueHope != null) skillList.Add(vagueHope);
        if (wretchedWeaponry != null) skillList.Add(wretchedWeaponry);

        return skillList;
    }

    public override void UpdatePassives(EnemyData enemy)
    {
    }

    private void ApplyPassive()
    {
        if (passiveApplied) return;

        if (passiveApBuff == null)
        {
            DevLog.LogWarning("[Diligere] passiveApBuff is not assigned.");
            passiveApplied = true;
            return;
        }

        if (BuffManager.Instance == null) return;

        BuffManager.Instance.AddEffect(false, passiveApBuff, 0.25f, 999);
        passiveApplied = true;
        DevLog.Log("[Diligere] Birth of a Wish: permanent AP +25% applied.");
    }
}
