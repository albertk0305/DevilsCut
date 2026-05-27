using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "EnemyAI_Gabriel", menuName = "EnemyAI/Gabriel Boss AI")]
public class EnemyAI_Gabriel : EnemyAIBase
{
    [SerializeField] private SkillData men;
    [SerializeField] private SkillData kote;
    [SerializeField] private SkillData doSkill;
    [SerializeField] private SkillData tsuki;
    [SerializeField] private SkillData sirius;

    private int patternIndex = 0;
    private bool pendingSirius = false;

    public override EnemyActionIntent DecideNextAction(int currentTurnCount, PlayerStats pStats, EnemyData enemy)
    {
        EnemyActionIntent intent = new EnemyActionIntent();

        bool shouldResetAfterAction = false;
        SkillData intendedSkill = null;

        if (BreakManager.Instance.IsBroken(true))
        {
            intendedSkill = sirius;
            shouldResetAfterAction = true;
        }
        else if (pendingSirius)
        {
            intendedSkill = sirius;
            shouldResetAfterAction = true;
        }
        else
        {
            switch (patternIndex)
            {
                case 0:
                    intendedSkill = men;
                    patternIndex = 1;
                    break;
                case 1:
                    intendedSkill = kote;
                    patternIndex = 2;
                    break;
                case 2:
                    intendedSkill = doSkill;
                    patternIndex = 3;
                    break;
                default:
                    intendedSkill = tsuki;
                    pendingSirius = true;
                    break;
            }
        }

        intent.skillToUse = intendedSkill != null ? intendedSkill : GetFallbackSkill();

        if (intent.skillToUse == null)
        {
            DevLog.LogWarning("[Gabriel AI] 사용할 수 있는 스킬이 없습니다.");
            return intent;
        }

        if (shouldResetAfterAction || intent.skillToUse == sirius)
        {
            pendingSirius = false;
            patternIndex = 0;
        }

        return intent;
    }

    public override List<SkillData> GetEnemySkills()
    {
        List<SkillData> skillList = new List<SkillData>();

        if (men != null) skillList.Add(men);
        if (kote != null) skillList.Add(kote);
        if (doSkill != null) skillList.Add(doSkill);
        if (tsuki != null) skillList.Add(tsuki);
        if (sirius != null) skillList.Add(sirius);

        return skillList;
    }

    public override void UpdatePassives(EnemyData enemy)
    {
    }

    private SkillData GetFallbackSkill()
    {
        if (men != null) return men;
        if (kote != null) return kote;
        if (doSkill != null) return doSkill;
        if (tsuki != null) return tsuki;
        if (sirius != null) return sirius;

        return null;
    }
}
