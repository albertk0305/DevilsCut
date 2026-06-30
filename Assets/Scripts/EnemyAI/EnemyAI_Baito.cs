using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "EnemyAI_Baito", menuName = "EnemyAI/Baito Hidden Boss AI")]
public class EnemyAI_Baito : EnemyAIBase
{
    [SerializeField] private SkillData fatimaSkill;
    [SerializeField] private SkillData anotherHeavenSkill;
    [SerializeField] private SkillData skycladObserverSkill;
    [SerializeField] private SkillData gateOfSteinerSkill;
    [SerializeField] private SkillData lyraSkill;
    [SerializeField] private SkillData amadeusSkill;
    [SerializeField] private StatusEffectData lyraStateBuff;
    [SerializeField] private StatusEffectData amadeusStateBuff;

    private bool hasTriggeredLyra;
    private bool hasTriggeredAmadeus;
    private bool isLyraState;
    private bool isAmadeusState;
    private int lyraTurnCount;
    private SkillData lastNormalSkill;

    public override EnemyActionIntent DecideNextAction(int currentTurnCount, PlayerStats pStats, EnemyData enemy)
    {
        EnemyActionIntent intent = new EnemyActionIntent();
        intent.skillSequence = new List<SkillData>();

        if (ShouldTriggerLyra(enemy))
        {
            AddSkill(intent.skillSequence, lyraSkill, "Lyra");
            AddSkill(intent.skillSequence, PickRandomOffensiveSkill(), "Lyra follow-up");
            MarkLyraTurnStarted();
            return FinalizeIntent(intent);
        }

        if (ShouldTriggerAmadeus(enemy))
        {
            AddSkill(intent.skillSequence, amadeusSkill, "Amadeus");
            AddSkill(intent.skillSequence, gateOfSteinerSkill, "Gate of Steiner");
            return FinalizeIntent(intent);
        }

        if (isLyraState)
            lyraTurnCount++;

        bool hasDebuff = HasEnemyDebuff();
        SkillData firstSkill = hasDebuff ? skycladObserverSkill : PickFirstNormalSkill();
        SkillData secondSkill = PickSecondSkill(hasDebuff, firstSkill);

        AddSkill(intent.skillSequence, firstSkill, "first skill");
        AddSkill(intent.skillSequence, secondSkill, "second skill");

        return FinalizeIntent(intent);
    }

    public override List<SkillData> GetEnemySkills()
    {
        List<SkillData> skillList = new List<SkillData>();

        AddIfNotNull(skillList, fatimaSkill);
        AddIfNotNull(skillList, anotherHeavenSkill);
        AddIfNotNull(skillList, skycladObserverSkill);
        AddIfNotNull(skillList, gateOfSteinerSkill);
        AddIfNotNull(skillList, lyraSkill);
        AddIfNotNull(skillList, amadeusSkill);

        return skillList;
    }

    public void NotifyLyraResolved(EnemyData enemy)
    {
        hasTriggeredLyra = true;
        isLyraState = true;
        lyraTurnCount = Mathf.Max(1, lyraTurnCount);
        AddPermanentStateBuff(lyraStateBuff, "lyraStateBuff");
        DevLog.Log("[Baito] Lyra state entered.");
    }

    public void NotifyAmadeusResolved(EnemyData enemy)
    {
        hasTriggeredAmadeus = true;
        isAmadeusState = true;
        AddPermanentStateBuff(amadeusStateBuff, "amadeusStateBuff");
        DevLog.Log("[Baito] Amadeus state entered.");
    }

    private bool ShouldTriggerLyra(EnemyData enemy)
    {
        if (hasTriggeredLyra || enemy == null || enemy.maxHp <= 0) return false;
        return enemy.currentHp <= Mathf.FloorToInt(enemy.maxHp * 0.5f);
    }

    private bool ShouldTriggerAmadeus(EnemyData enemy)
    {
        if (!isLyraState || hasTriggeredAmadeus || enemy == null || enemy.maxHp <= 0) return false;
        return enemy.currentHp <= Mathf.FloorToInt(enemy.maxHp * 0.3f);
    }

    private void MarkLyraTurnStarted()
    {
        hasTriggeredLyra = true;
        isLyraState = true;
        lyraTurnCount = 1;
    }

    private SkillData PickFirstNormalSkill()
    {
        List<SkillData> candidates = GetNormalSkillCandidates(true);
        return PickRandomSkill(candidates);
    }

    private SkillData PickSecondSkill(bool firstWasForcedSkyclad, SkillData firstSkill)
    {
        if (isAmadeusState)
            return gateOfSteinerSkill;

        if (isLyraState && lyraTurnCount > 0 && lyraTurnCount % 8 == 0)
            return gateOfSteinerSkill;

        List<SkillData> candidates = firstWasForcedSkyclad
            ? GetOffensiveSkillCandidates()
            : GetNormalSkillCandidates(true);

        if (!firstWasForcedSkyclad && firstSkill != null && candidates.Count >= 2)
            candidates.Remove(firstSkill);

        return PickRandomSkill(candidates);
    }

    private SkillData PickRandomOffensiveSkill()
    {
        return PickRandomSkill(GetOffensiveSkillCandidates());
    }

    private SkillData PickRandomSkill(List<SkillData> candidates)
    {
        if (candidates == null || candidates.Count == 0)
            return GetFallbackSkill();

        SkillData selectedSkill = candidates[Random.Range(0, candidates.Count)];
        if (selectedSkill != null && IsNormalSkill(selectedSkill))
            lastNormalSkill = selectedSkill;

        return selectedSkill;
    }

    private List<SkillData> GetNormalSkillCandidates(bool avoidLastNormalSkill)
    {
        List<SkillData> candidates = new List<SkillData>();

        AddIfNotNull(candidates, fatimaSkill);
        AddIfNotNull(candidates, anotherHeavenSkill);
        AddIfNotNull(candidates, skycladObserverSkill);

        if (avoidLastNormalSkill && lastNormalSkill != null && candidates.Count >= 2)
            candidates.Remove(lastNormalSkill);

        return candidates;
    }

    private List<SkillData> GetOffensiveSkillCandidates()
    {
        List<SkillData> candidates = new List<SkillData>();

        AddIfNotNull(candidates, fatimaSkill);
        AddIfNotNull(candidates, anotherHeavenSkill);

        return candidates;
    }

    private bool HasEnemyDebuff()
    {
        if (BuffManager.Instance == null) return false;

        List<BuffManager.ActiveEffect> effects = BuffManager.Instance.GetEffects(false);
        foreach (var effect in effects)
        {
            if (effect.effectData != null && effect.effectData.category == EffectCategory.Debuff)
                return true;
        }

        return false;
    }

    private EnemyActionIntent FinalizeIntent(EnemyActionIntent intent)
    {
        if (intent.skillSequence == null || intent.skillSequence.Count == 0)
        {
            intent.skillToUse = GetFallbackSkill();
            if (intent.skillToUse == null)
                DevLog.LogWarning("[Baito AI] No usable skill is assigned.");
            return intent;
        }

        intent.skillToUse = intent.skillSequence[0];
        return intent;
    }

    private void AddPermanentStateBuff(StatusEffectData stateBuff, string label)
    {
        if (stateBuff == null)
        {
            DevLog.LogWarning($"[Baito] {label} is not assigned.");
            return;
        }

        if (BuffManager.Instance == null) return;

        bool alreadyExists = BuffManager.Instance.GetEffects(false).Exists(e => e.effectData == stateBuff);
        if (!alreadyExists)
            BuffManager.Instance.AddEffect(false, stateBuff, 1f, 999);
    }

    private SkillData GetFallbackSkill()
    {
        if (fatimaSkill != null) return fatimaSkill;
        if (anotherHeavenSkill != null) return anotherHeavenSkill;
        if (skycladObserverSkill != null) return skycladObserverSkill;
        if (gateOfSteinerSkill != null) return gateOfSteinerSkill;
        if (lyraSkill != null) return lyraSkill;
        if (amadeusSkill != null) return amadeusSkill;

        return null;
    }

    private bool IsNormalSkill(SkillData skill)
    {
        return skill == fatimaSkill || skill == anotherHeavenSkill || skill == skycladObserverSkill;
    }

    private void AddSkill(List<SkillData> skillList, SkillData skill, string label)
    {
        if (skill != null)
        {
            skillList.Add(skill);
        }
        else
        {
            DevLog.LogWarning($"[Baito AI] {label} is not assigned.");
        }
    }

    private void AddIfNotNull(List<SkillData> list, SkillData skill)
    {
        if (skill != null) list.Add(skill);
    }
}
