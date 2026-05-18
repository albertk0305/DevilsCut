using UnityEngine;
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

    private int phase1Index = 0;
    private int phase2Index = 0;
    private bool isPhase2 = false;

    public override EnemyActionIntent DecideNextAction(int currentTurnCount, PlayerStats pStats, EnemyData enemy)
    {
        EnemyActionIntent intent = new EnemyActionIntent();

        // 현재 체력 퍼센트 계산 (CombatManager나 StatManager 구조에 맞게 수정해주세요)
        // 예: float hpPct = (float)CombatManager.Instance.currentEnemyHp / enemy.maxHp;
        float hpPct = 0.5f; // 임시

        // 1. 광폭화 스킬 발동 (50% 이하가 된 최초의 턴)
        if (!isPhase2 && hpPct <= 0.5f)
        {
            isPhase2 = true;
            intent.skillToUse = enrageSkill; // 이번 턴은 광폭화 스킬을 쓰고 턴을 넘깁니다!
            return intent;
        }

        // 2. 그로기 처형 기믹
        if (isPhase2 && BreakManager.Instance.IsBroken(true))
        {
            intent.skillToUse = ironMaiden;
            return intent;
        }

        // 3. 정규 루프
        if (!isPhase2)
        {
            if (phase1Index == 0) intent.skillToUse = chainBury;
            else intent.skillToUse = chainsawScratch;
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
}