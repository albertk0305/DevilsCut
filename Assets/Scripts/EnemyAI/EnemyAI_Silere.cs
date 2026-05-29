using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "EnemyAI_Silere", menuName = "EnemyAI/Silere AI")]
public class EnemyAI_Silere : EnemyAIBase
{
    [SerializeField] private SkillData garden;
    [SerializeField] private SkillData itAintOver;
    [SerializeField] private SkillData hachiko;

    private int patternIndex;

    public override EnemyActionIntent DecideNextAction(int currentTurnCount, PlayerStats pStats, EnemyData enemy)
    {
        EnemyActionIntent intent = new EnemyActionIntent();
        SkillData intendedSkill = GetPatternSkill();

        intent.skillToUse = intendedSkill != null ? intendedSkill : GetFallbackSkill();
        patternIndex = (patternIndex + 1) % 3;

        if (intent.skillToUse == null)
        {
            DevLog.LogWarning("[Silere AI] No usable skill is assigned.");
        }

        return intent;
    }

    public override List<SkillData> GetEnemySkills()
    {
        List<SkillData> skillList = new List<SkillData>();

        if (garden != null) skillList.Add(garden);
        if (itAintOver != null) skillList.Add(itAintOver);
        if (hachiko != null) skillList.Add(hachiko);

        return skillList;
    }

    private SkillData GetPatternSkill()
    {
        if (patternIndex == 0) return garden;
        if (patternIndex == 1) return itAintOver;
        return hachiko;
    }

    private SkillData GetFallbackSkill()
    {
        if (garden != null) return garden;
        if (itAintOver != null) return itAintOver;
        if (hachiko != null) return hachiko;

        return null;
    }
}
