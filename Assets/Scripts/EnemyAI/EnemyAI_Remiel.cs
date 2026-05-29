using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "EnemyAI_Remiel", menuName = "EnemyAI/Remiel Boss AI")]
public class EnemyAI_Remiel : EnemyAIBase
{
    [SerializeField] private SkillData shiori;
    [SerializeField] private SkillData stapleStaple;
    [SerializeField] private SkillData coldWindSentiment;
    [SerializeField] private SkillData dreamyDateDrive;
    [SerializeField] private SkillData snowdrop;

    [SerializeField] private StatusEffectData passiveApBuff;

    private bool passiveApplied;
    private int patternIndex;

    public override EnemyActionIntent DecideNextAction(int currentTurnCount, PlayerStats pStats, EnemyData enemy)
    {
        if (!passiveApplied) ApplyPassive();

        EnemyActionIntent intent = new EnemyActionIntent();
        SkillData intendedSkill = GetPatternSkill();

        intent.skillToUse = intendedSkill != null ? intendedSkill : GetFallbackSkill();
        patternIndex = (patternIndex + 1) % 9;

        if (intent.skillToUse == null)
        {
            DevLog.LogWarning("[Remiel AI] No usable skill is assigned.");
        }

        return intent;
    }

    public override List<SkillData> GetEnemySkills()
    {
        List<SkillData> skillList = new List<SkillData>();

        if (shiori != null) skillList.Add(shiori);
        if (stapleStaple != null) skillList.Add(stapleStaple);
        if (coldWindSentiment != null) skillList.Add(coldWindSentiment);
        if (dreamyDateDrive != null) skillList.Add(dreamyDateDrive);
        if (snowdrop != null) skillList.Add(snowdrop);

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
            DevLog.LogWarning("[Remiel] passiveApBuff is not assigned.");
            passiveApplied = true;
            return;
        }

        if (BuffManager.Instance == null) return;

        BuffManager.Instance.AddEffect(false, passiveApBuff, 0.25f, 999);
        passiveApplied = true;
        DevLog.Log("[Remiel] Fast Love: permanent AP +25% applied.");
    }

    private SkillData GetPatternSkill()
    {
        switch (patternIndex)
        {
            case 0:
            case 4:
                return shiori;
            case 1:
            case 5:
                return stapleStaple;
            case 2:
            case 6:
                return coldWindSentiment;
            case 3:
            case 7:
                return dreamyDateDrive;
            default:
                return snowdrop;
        }
    }

    private SkillData GetFallbackSkill()
    {
        if (shiori != null) return shiori;
        if (stapleStaple != null) return stapleStaple;
        if (coldWindSentiment != null) return coldWindSentiment;
        if (dreamyDateDrive != null) return dreamyDateDrive;
        if (snowdrop != null) return snowdrop;

        return null;
    }
}
