using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "EnemyAI_Temperare", menuName = "EnemyAI/Temperare AI")]
public class EnemyAI_Temperare : EnemyAIBase
{
    [SerializeField] private SkillData instinct;
    [SerializeField] private SkillData overflowingWealth;

    [SerializeField] private StatusEffectData passiveEvasionBuff;

    private bool passiveApplied;
    private int patternIndex;

    public override EnemyActionIntent DecideNextAction(int currentTurnCount, PlayerStats pStats, EnemyData enemy)
    {
        EnemyActionIntent intent = new EnemyActionIntent();

        SkillData intendedSkill = (patternIndex == 0) ? instinct : overflowingWealth;
        SkillData fallbackSkill = (intendedSkill == instinct) ? overflowingWealth : instinct;

        intent.skillToUse = intendedSkill != null ? intendedSkill : fallbackSkill;
        patternIndex = (patternIndex + 1) % 2;

        if (intent.skillToUse == null)
        {
            DevLog.LogWarning("[Temperare AI] No usable skill is assigned.");
        }

        return intent;
    }

    public override List<SkillData> GetEnemySkills()
    {
        List<SkillData> skillList = new List<SkillData>();

        if (instinct != null) skillList.Add(instinct);
        if (overflowingWealth != null) skillList.Add(overflowingWealth);

        return skillList;
    }

    public override void UpdatePassives(EnemyData enemy)
    {
        ApplyPassive();
    }

    private void ApplyPassive()
    {
        if (passiveApplied) return;

        if (passiveEvasionBuff == null)
        {
            DevLog.LogWarning("[Temperare] passiveEvasionBuff is not assigned.");
            passiveApplied = true;
            return;
        }

        if (BuffManager.Instance == null) return;

        BuffManager.Instance.AddEffect(false, passiveEvasionBuff, 25f, 999);
        passiveApplied = true;
        DevLog.Log("[Temperare] Life Is Full of Dreams: permanent evasion +25pp applied.");
    }
}
