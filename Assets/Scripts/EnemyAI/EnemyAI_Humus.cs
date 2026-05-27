using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "EnemyAI_Humus", menuName = "EnemyAI/Humus AI")]
public class EnemyAI_Humus : EnemyAIBase
{
    [SerializeField] private SkillData sparklingDaydream;
    [SerializeField] private SkillData voice;

    private int patternIndex = 0;

    public override EnemyActionIntent DecideNextAction(int currentTurnCount, PlayerStats pStats, EnemyData enemy)
    {
        EnemyActionIntent intent = new EnemyActionIntent();

        SkillData intendedSkill = (patternIndex == 0) ? sparklingDaydream : voice;
        SkillData fallbackSkill = (intendedSkill == sparklingDaydream) ? voice : sparklingDaydream;

        intent.skillToUse = intendedSkill != null ? intendedSkill : fallbackSkill;
        patternIndex = (patternIndex + 1) % 3;

        if (intent.skillToUse == null)
        {
            DevLog.LogWarning("[Humus AI] 사용할 수 있는 스킬이 없습니다.");
        }

        return intent;
    }

    public override void UpdatePassives(EnemyData enemy)
    {
        if (enemy == null) return;

        if (enemy.maxHp <= 0)
        {
            enemy.damageGivenAmp = 0f;
            enemy.lifeSteal = 0f;
            return;
        }

        float missingHpRatio = (float)(enemy.maxHp - enemy.currentHp) / enemy.maxHp;

        enemy.damageGivenAmp = missingHpRatio * 0.6f;
        enemy.lifeSteal = 0f;
    }

    public override List<SkillData> GetEnemySkills()
    {
        List<SkillData> skillList = new List<SkillData>();

        if (sparklingDaydream != null) skillList.Add(sparklingDaydream);
        if (voice != null) skillList.Add(voice);

        return skillList;
    }
}
