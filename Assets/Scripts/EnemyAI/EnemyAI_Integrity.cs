using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "EnemyAI_Integrity", menuName = "EnemyAI/Integrity AI")]
public class EnemyAI_Integrity : EnemyAIBase
{
    [SerializeField] private SkillData brandNewDays;
    [SerializeField] private SkillData massDestruction;
    [SerializeField] private SkillData heartfulCry;

    private int patternIndex;

    public override EnemyActionIntent DecideNextAction(int currentTurnCount, PlayerStats pStats, EnemyData enemy)
    {
        EnemyActionIntent intent = new EnemyActionIntent();
        SkillData intendedSkill = GetPatternSkill();

        intent.skillToUse = intendedSkill != null ? intendedSkill : GetFallbackSkill();
        patternIndex = (patternIndex + 1) % 3;

        if (intent.skillToUse == null)
        {
            DevLog.LogWarning("[Integrity AI] No usable skill is assigned.");
        }

        return intent;
    }

    public override List<SkillData> GetEnemySkills()
    {
        List<SkillData> skillList = new List<SkillData>();

        if (brandNewDays != null) skillList.Add(brandNewDays);
        if (massDestruction != null) skillList.Add(massDestruction);
        if (heartfulCry != null) skillList.Add(heartfulCry);

        return skillList;
    }

    private SkillData GetPatternSkill()
    {
        if (patternIndex == 0) return brandNewDays;
        if (patternIndex == 1) return massDestruction;
        return heartfulCry;
    }

    private SkillData GetFallbackSkill()
    {
        if (brandNewDays != null) return brandNewDays;
        if (massDestruction != null) return massDestruction;
        if (heartfulCry != null) return heartfulCry;

        return null;
    }
}
