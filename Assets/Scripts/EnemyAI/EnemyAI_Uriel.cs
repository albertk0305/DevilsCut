using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "EnemyAI_Uriel", menuName = "EnemyAI/Uriel Boss AI")]
public class EnemyAI_Uriel : EnemyAIBase
{
    [SerializeField] private SkillData peterPan;
    [SerializeField] private SkillData strongest;
    [SerializeField] private SkillData merryGoRound;
    [SerializeField] private SkillData curtainCall;

    [SerializeField] private StatusEffectData infinityDamageReduction;
    [SerializeField] private Sprite counterImage;

    private int enduranceStacks;
    private bool infinityApplied;

    public int EnduranceStacks => enduranceStacks;

    public void AddEnduranceStack(int amount = 1)
    {
        enduranceStacks += amount;
        DevLog.Log($"[Uriel] Endurance stacks: {enduranceStacks}");
    }

    public void SpendCurtainCallStacks()
    {
        enduranceStacks = Mathf.Max(0, enduranceStacks - 7);
        DevLog.Log($"[Uriel] Curtain Call spent 7 endurance. Remaining: {enduranceStacks}");
    }

    public Sprite GetCounterImage(EnemyData enemy)
    {
        if (counterImage != null) return counterImage;
        if (enemy != null && enemy.enemyImage != null) return enemy.enemyImage;
        if (enemy != null && enemy.guardImage != null) return enemy.guardImage;
        return enemy != null ? enemy.hit : null;
    }

    public override EnemyActionIntent DecideNextAction(int currentTurnCount, PlayerStats pStats, EnemyData enemy)
    {
        ApplyInfinity();

        EnemyActionIntent intent = new EnemyActionIntent();
        SkillData intendedSkill = GetSkillForCurrentEndurance();

        intent.skillToUse = intendedSkill != null ? intendedSkill : GetFallbackSkill();

        if (intent.skillToUse == null)
        {
            DevLog.LogWarning("[Uriel AI] No usable skill is assigned.");
        }

        return intent;
    }

    public override List<SkillData> GetEnemySkills()
    {
        List<SkillData> skillList = new List<SkillData>();

        if (peterPan != null) skillList.Add(peterPan);
        if (strongest != null) skillList.Add(strongest);
        if (merryGoRound != null) skillList.Add(merryGoRound);
        if (curtainCall != null) skillList.Add(curtainCall);

        return skillList;
    }

    private void ApplyInfinity()
    {
        if (infinityApplied) return;

        if (infinityDamageReduction == null)
        {
            DevLog.LogWarning("[Uriel] infinityDamageReduction is not assigned.");
            infinityApplied = true;
            return;
        }

        if (BuffManager.Instance == null) return;

        BuffManager.Instance.AddEffect(false, infinityDamageReduction, 0.30f, 999);
        infinityApplied = true;
        DevLog.Log("[Uriel] Infinity: permanent damage reduction 30% applied.");
    }

    private SkillData GetSkillForCurrentEndurance()
    {
        if (enduranceStacks <= 0) return peterPan;
        if (enduranceStacks <= 3) return strongest;
        if (enduranceStacks <= 6) return merryGoRound;
        return curtainCall;
    }

    private SkillData GetFallbackSkill()
    {
        if (peterPan != null) return peterPan;
        if (strongest != null) return strongest;
        if (merryGoRound != null) return merryGoRound;
        if (curtainCall != null) return curtainCall;

        return null;
    }
}
