using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "EnemyAI_Castus", menuName = "EnemyAI/Castus AI")]
public class EnemyAI_Castus : EnemyAIBase
{
    [SerializeField] private SkillData ambiguous;
    [SerializeField] private SkillData hymn;

    private int patternIndex = 0;

    public override EnemyActionIntent DecideNextAction(int currentTurnCount, PlayerStats pStats, EnemyData enemy)
    {
        EnemyActionIntent intent = new EnemyActionIntent();

        SkillData intendedSkill = (patternIndex == 0) ? ambiguous : hymn;
        SkillData fallbackSkill = (intendedSkill == ambiguous) ? hymn : ambiguous;

        intent.skillToUse = intendedSkill != null ? intendedSkill : fallbackSkill;
        patternIndex = (patternIndex + 1) % 2;

        if (intent.skillToUse == null)
        {
            DevLog.LogWarning("[Castus AI] 사용할 수 있는 스킬이 없습니다.");
        }

        return intent;
    }

    public override List<SkillData> GetEnemySkills()
    {
        List<SkillData> skillList = new List<SkillData>();

        if (ambiguous != null) skillList.Add(ambiguous);
        if (hymn != null) skillList.Add(hymn);

        return skillList;
    }

    public override void UpdatePassives(EnemyData enemy)
    {
    }
}
