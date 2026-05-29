using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "EnemyAI_Arete", menuName = "EnemyAI/Arete AI")]
public class EnemyAI_Arete : EnemyAIBase
{
    [SerializeField] private SkillData rememberTheTime;
    [SerializeField] private SkillData beatIt;
    [SerializeField] private SkillData blackAndWhite;
    [SerializeField] private SkillData healTheWorld;
    [SerializeField] private SkillData offTheWall;
    [SerializeField] private SkillData manInTheMirror;
    [SerializeField] private SkillData smoothCriminal;
    [SerializeField] private SkillData humanNature;

    private int normalSkillUseCount;
    private SkillData lastNormalSkill;

    public override EnemyActionIntent DecideNextAction(int currentTurnCount, PlayerStats pStats, EnemyData enemy)
    {
        EnemyActionIntent intent = new EnemyActionIntent();

        if (normalSkillUseCount >= 6 && humanNature != null)
        {
            intent.skillToUse = humanNature;
            normalSkillUseCount = 0;
            return intent;
        }

        List<SkillData> candidates = GetNormalSkillCandidates();

        if (candidates.Count >= 2 && lastNormalSkill != null)
        {
            candidates.Remove(lastNormalSkill);
        }

        if (candidates.Count > 0)
        {
            SkillData selectedSkill = candidates[Random.Range(0, candidates.Count)];
            intent.skillToUse = selectedSkill;
            normalSkillUseCount++;
            lastNormalSkill = selectedSkill;
            return intent;
        }

        intent.skillToUse = humanNature;

        if (intent.skillToUse == null)
        {
            DevLog.LogWarning("[Arete AI] No usable skill is assigned.");
        }

        return intent;
    }

    public override List<SkillData> GetEnemySkills()
    {
        List<SkillData> skillList = new List<SkillData>();

        AddIfNotNull(skillList, rememberTheTime);
        AddIfNotNull(skillList, beatIt);
        AddIfNotNull(skillList, blackAndWhite);
        AddIfNotNull(skillList, healTheWorld);
        AddIfNotNull(skillList, offTheWall);
        AddIfNotNull(skillList, manInTheMirror);
        AddIfNotNull(skillList, smoothCriminal);
        AddIfNotNull(skillList, humanNature);

        return skillList;
    }

    private List<SkillData> GetNormalSkillCandidates()
    {
        List<SkillData> candidates = new List<SkillData>();

        AddIfNotNull(candidates, rememberTheTime);
        AddIfNotNull(candidates, beatIt);
        AddIfNotNull(candidates, blackAndWhite);
        AddIfNotNull(candidates, healTheWorld);
        AddIfNotNull(candidates, offTheWall);
        AddIfNotNull(candidates, manInTheMirror);
        AddIfNotNull(candidates, smoothCriminal);

        return candidates;
    }

    private void AddIfNotNull(List<SkillData> list, SkillData skill)
    {
        if (skill != null) list.Add(skill);
    }
}
