using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "EnemyAI_Hibiki", menuName = "EnemyAI/Hibiki AI")]
public class EnemyAI_Hibiki : EnemyAIBase
{
    [SerializeField] private SkillData masshiro;
    [SerializeField] private SkillData tabiji;
    [SerializeField] private SkillData prema;
    [SerializeField] private SkillData kirari;
    [SerializeField] private SkillData damn;
    [SerializeField] private SkillData kazeyo;
    [SerializeField] private SkillData grace;
    [SerializeField] private SkillData matsuri;

    [SerializeField] private StatusEffectData matsuriApPermanentBuff;

    private int normalSkillUseCount;
    private SkillData lastNormalSkill;
    private int matsuriApStackCount;
    private const int MaxMatsuriApStacks = 5;

    public override EnemyActionIntent DecideNextAction(int currentTurnCount, PlayerStats pStats, EnemyData enemy)
    {
        EnemyActionIntent intent = new EnemyActionIntent();

        if (normalSkillUseCount >= 5 && matsuri != null)
        {
            intent.skillToUse = matsuri;
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

        intent.skillToUse = matsuri;

        if (intent.skillToUse == null)
        {
            DevLog.LogWarning("[Hibiki AI] No usable skill is assigned.");
        }

        return intent;
    }

    public override List<SkillData> GetEnemySkills()
    {
        List<SkillData> skillList = new List<SkillData>();

        AddIfNotNull(skillList, masshiro);
        AddIfNotNull(skillList, tabiji);
        AddIfNotNull(skillList, prema);
        AddIfNotNull(skillList, kirari);
        AddIfNotNull(skillList, damn);
        AddIfNotNull(skillList, kazeyo);
        AddIfNotNull(skillList, grace);
        AddIfNotNull(skillList, matsuri);

        return skillList;
    }

    public void AddMatsuriApStackIfPossible()
    {
        if (matsuriApStackCount >= MaxMatsuriApStacks) return;

        if (matsuriApPermanentBuff == null)
        {
            DevLog.LogWarning("[Hibiki] matsuriApPermanentBuff is not assigned.");
            return;
        }

        if (BuffManager.Instance == null) return;

        BuffManager.Instance.AddEffect(false, matsuriApPermanentBuff, 0.10f, 999);
        matsuriApStackCount++;
        DevLog.Log($"[Hibiki] Matsuri permanent AP stack: {matsuriApStackCount}/{MaxMatsuriApStacks}");
    }

    private List<SkillData> GetNormalSkillCandidates()
    {
        List<SkillData> candidates = new List<SkillData>();

        AddIfNotNull(candidates, masshiro);
        AddIfNotNull(candidates, tabiji);
        AddIfNotNull(candidates, prema);
        AddIfNotNull(candidates, kirari);
        AddIfNotNull(candidates, damn);
        AddIfNotNull(candidates, kazeyo);
        AddIfNotNull(candidates, grace);

        return candidates;
    }

    private void AddIfNotNull(List<SkillData> list, SkillData skill)
    {
        if (skill != null) list.Add(skill);
    }
}
