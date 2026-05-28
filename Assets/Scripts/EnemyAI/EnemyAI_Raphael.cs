using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "EnemyAI_Raphael", menuName = "EnemyAI/Raphael Boss AI")]
public class EnemyAI_Raphael : EnemyAIBase
{
    [SerializeField] private SkillData lie;
    [SerializeField] private SkillData goldenTimeRubber;
    [SerializeField] private SkillData period;
    [SerializeField] private SkillData letItOut;
    [SerializeField] private SkillData shunkanSentimental;

    [SerializeField] private StatusEffectData againEvasionBuff;

    private bool passiveApplied;
    private int againCount;
    private int patternIndex;

    private void OnEnable()
    {
        BattleEventSystem.OnEvaded -= HandleEvaded;
        BattleEventSystem.OnEvaded += HandleEvaded;
    }

    private void OnDisable()
    {
        BattleEventSystem.OnEvaded -= HandleEvaded;
    }

    public override EnemyActionIntent DecideNextAction(int currentTurnCount, PlayerStats pStats, EnemyData enemy)
    {
        if (!passiveApplied) ApplyPassive(enemy);

        EnemyActionIntent intent = new EnemyActionIntent();

        if (againCount >= 3 && shunkanSentimental != null)
        {
            intent.skillToUse = shunkanSentimental;
            againCount = 0;
            return intent;
        }

        SkillData intendedSkill = null;
        switch (patternIndex)
        {
            case 0:
                intendedSkill = lie;
                break;
            case 1:
                intendedSkill = goldenTimeRubber;
                break;
            case 2:
                intendedSkill = period;
                break;
            default:
                intendedSkill = letItOut;
                break;
        }

        intent.skillToUse = intendedSkill != null ? intendedSkill : GetFallbackSkill();
        patternIndex = (patternIndex + 1) % 4;

        if (intent.skillToUse == null)
        {
            DevLog.LogWarning("[Raphael AI] 사용할 수 있는 스킬이 없습니다.");
        }

        return intent;
    }

    public void ApplyPassive(EnemyData enemy)
    {
        if (passiveApplied) return;

        if (againEvasionBuff == null)
        {
            DevLog.LogWarning("[Raphael] againEvasionBuff가 연결되지 않았습니다.");
            return;
        }

        if (BuffManager.Instance == null) return;

        bool alreadyExists = BuffManager.Instance
            .GetEffects(false)
            .Exists(e => e.effectData == againEvasionBuff);

        if (!alreadyExists)
        {
            BuffManager.Instance.AddEffect(false, againEvasionBuff, 25f, 999);
        }

        passiveApplied = true;
        DevLog.Log("[Raphael] Again: 회피율 25% 영구 버프가 적용되었습니다.");
    }

    public override List<SkillData> GetEnemySkills()
    {
        List<SkillData> skillList = new List<SkillData>();

        if (lie != null) skillList.Add(lie);
        if (goldenTimeRubber != null) skillList.Add(goldenTimeRubber);
        if (period != null) skillList.Add(period);
        if (letItOut != null) skillList.Add(letItOut);
        if (shunkanSentimental != null) skillList.Add(shunkanSentimental);

        return skillList;
    }

    public override void UpdatePassives(EnemyData enemy)
    {
        ApplyPassive(enemy);
    }

    private void HandleEvaded(bool isPlayerTarget)
    {
        if (!passiveApplied) return;
        if (isPlayerTarget) return;

        againCount++;
        DevLog.Log($"[Raphael] Again 카운트 증가: {againCount}/3");
    }

    private SkillData GetFallbackSkill()
    {
        if (lie != null) return lie;
        if (goldenTimeRubber != null) return goldenTimeRubber;
        if (period != null) return period;
        if (letItOut != null) return letItOut;
        if (shunkanSentimental != null) return shunkanSentimental;

        return null;
    }
}
