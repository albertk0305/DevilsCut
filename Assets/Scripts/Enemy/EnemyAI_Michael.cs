using UnityEngine;
using System.Linq;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "EnemyAI_Michael", menuName = "EnemyAI/Michael Boss AI")]
public class EnemyAI_Michael : EnemyAIBase
{
    [Header("1페이즈 스킬")]
    public SkillData chainBury;
    public SkillData chainsawScratch;

    [Header("2페이즈 스킬")]
    public SkillData enrageSkill; // [추가] 광폭화 스킬 (회복 및 버프 시전용)
    public SkillData bloodCurse;
    public SkillData blazingChainsaw;
    public SkillData ironMaiden;

    public StatusEffectData phase2Marker;

    private int phase1Index = 0;
    private int phase2Index = 0;

    public override EnemyActionIntent DecideNextAction(int currentTurnCount, PlayerStats pStats, EnemyData enemy)
    {
        EnemyActionIntent intent = new EnemyActionIntent();

        // 1. BuffManager에서 광폭화 마커가 있는지 확인 (isPhase2 변수 대신 이것을 사용!)
        bool isEnraged = false;
        if (BuffManager.Instance != null && phase2Marker != null)
        {
            isEnraged = BuffManager.Instance.GetEffects(false).Exists(e => e.effectData == phase2Marker);
        }

        float hpPct = (float)enemy.currentHp / enemy.maxHp;

        // 2. 광폭화 발동 조건: 광폭화 아님 && 체력 50% 이하
        if (!isEnraged && hpPct <= 0.5f)
        {
            intent.skillToUse = enrageSkill;
            return intent;
        }

        // 3. 그로기 처형 기믹
        if (isEnraged && BreakManager.Instance.IsBroken(true))
        {
            intent.skillToUse = ironMaiden;
            return intent;
        }

        // 4. 패턴 루프
        if (!isEnraged)
        {
            intent.skillToUse = (phase1Index == 0) ? chainBury : chainsawScratch;
            phase1Index = (phase1Index + 1) % 3;
        }
        else
        {
            switch (phase2Index)
            {
                case 0: case 3: intent.skillToUse = bloodCurse; break;
                case 1: case 2: case 4: intent.skillToUse = blazingChainsaw; break;
                case 5: intent.skillToUse = ironMaiden; break;
            }
            phase2Index = (phase2Index + 1) % 6;
        }

        return intent;
    }

    public override void UpdatePassives(EnemyData enemy)
    {
        if (enemy == null || enemy.maxHp <= 0) return;

        float missingHpRatio = (float)(enemy.maxHp - enemy.currentHp) / enemy.maxHp;

        // 1. 패시브 1: 잃은 체력 비례 피해 증폭 (항상 적용)
        // 기획 공식: 잃은 체력 비율 * 1.2f (최대 120% 증폭)
        enemy.damageGivenAmp = missingHpRatio * 1.2f;

        // 2. 패시브 2: 광폭화 상태 시 잃은 체력 비례 흡혈률 적용
        bool isEnraged = false;
        if (BuffManager.Instance != null && phase2Marker != null)
        {
            isEnraged = BuffManager.Instance.GetEffects(false).Exists(e => e.effectData == phase2Marker);
        }

        if (isEnraged)
        {
            // 기획 공식: 기본 10% + (잃은 체력 비율 * 30%)
            enemy.lifeSteal = 0.10f + (missingHpRatio * 0.30f);
        }
        else
        {
            // 광폭화 전에는 흡혈이 없음
            enemy.lifeSteal = 0f;
        }
    }

    public override List<SkillData> GetEnemySkills()
    {
        List<SkillData> skillList = new List<SkillData>();

        if (chainBury != null) skillList.Add(chainBury);
        if (chainsawScratch != null) skillList.Add(chainsawScratch);
        if (enrageSkill != null) skillList.Add(enrageSkill);
        if (bloodCurse != null) skillList.Add(bloodCurse);
        if (blazingChainsaw != null) skillList.Add(blazingChainsaw);
        if (ironMaiden != null) skillList.Add(ironMaiden);

        return skillList;
    }
}