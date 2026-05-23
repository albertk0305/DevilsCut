using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SkillDatabase", menuName = "GameData/SkillDatabase")]
public class SkillDatabase : ScriptableObject
{
    public List<SkillData> allSkills = new List<SkillData>();

    public SkillData GetByID(string skillID)
    {
        if (string.IsNullOrEmpty(skillID))
        {
            Debug.LogWarning("SkillDatabase: empty skillID.");
            return null;
        }

        SkillData found = null;

        foreach (SkillData skill in allSkills)
        {
            if (skill == null || skill.skillID != skillID)
                continue;

            if (found != null)
                Debug.LogWarning($"SkillDatabase: duplicate skillID '{skillID}'.");

            found = skill;
        }

        return found;
    }

    public SkillData GetByNameKeyFallback(string skillNameKey)
    {
        if (string.IsNullOrEmpty(skillNameKey))
        {
            Debug.LogWarning("SkillDatabase: empty skillNameKey fallback.");
            return null;
        }

        foreach (SkillData skill in allSkills)
        {
            if (skill != null && skill.skillNameKey == skillNameKey)
                return skill;
        }

        return null;
    }
}
