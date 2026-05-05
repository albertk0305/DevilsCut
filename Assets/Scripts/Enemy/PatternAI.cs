using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewPatternAI", menuName = "EnemyAI/Pattern AI")]
public class PatternAI : EnemyAIBase
{
    [Header("스킬 사용 순서 (위에서부터 차례대로)")]
    public List<SkillData> skillPattern;

    public override SkillData DecideNextSkill(int currentTurnCount, PlayerStats pStats, EnemyData enemy)
    {
        // 스킬 패턴이 비어있으면 에러 방지를 위해 null 반환
        if (skillPattern == null || skillPattern.Count == 0)
            return null;

        // 핵심 수학 로직: 턴 수에 따라 리스트를 순환합니다. (예: 3개짜리 리스트면 0, 1, 2, 0, 1, 2...)
        int index = currentTurnCount % skillPattern.Count;

        return skillPattern[index];
    }
}