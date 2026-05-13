using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewPatternAI", menuName = "EnemyAI/Pattern AI")]
public class PatternAI : EnemyAIBase
{
    [Header("스킬 사용 순서 (위에서부터 차례대로)")]
    public List<SkillData> skillPattern;

    public override EnemyActionIntent DecideNextAction(int currentTurnCount, PlayerStats pStats, EnemyData enemy)
    {
        // 1. 제출할 빈 계획서 생성
        EnemyActionIntent intent = new EnemyActionIntent();

        // 스킬 패턴이 비어있으면 빈 계획서(아무 행동 안 함) 반환
        if (skillPattern == null || skillPattern.Count == 0)
            return intent;

        // 2. 턴 수에 따라 스킬 선택
        int index = currentTurnCount % skillPattern.Count;

        // 3. 계획서에 스킬 기입 후 제출
        intent.skillToUse = skillPattern[index];

        return intent;
    }
}