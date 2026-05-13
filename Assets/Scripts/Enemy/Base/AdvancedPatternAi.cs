using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class PatternStep
{
    [Header("스킬 설정")]
    public SkillData skillToUse;

    [Header("대사 설정 (선택)")]
    public string dialogueKey;
    public float dialogueDuration = 1.5f;

    [Header("기 모으기 설정 (선택)")]
    public bool isCharging;
    public string chargeComment;
}

[CreateAssetMenu(fileName = "NewAdvancedPatternAI", menuName = "EnemyAI/Advanced Pattern AI")]
public class AdvancedPatternAI : EnemyAIBase
{
    [Header("보스의 상세 행동 패턴 (위에서부터 차례대로 순환)")]
    public List<PatternStep> detailedPattern;

    public override EnemyActionIntent DecideNextAction(int currentTurnCount, PlayerStats pStats, EnemyData enemy)
    {
        EnemyActionIntent intent = new EnemyActionIntent();

        if (detailedPattern == null || detailedPattern.Count == 0)
            return intent;

        int index = currentTurnCount % detailedPattern.Count;
        PatternStep currentStep = detailedPattern[index];

        // 인스펙터에서 세팅한 설정값들을 계획서(Intent)에 그대로 복사해서 지휘관에게 넘깁니다.
        intent.skillToUse = currentStep.skillToUse;
        intent.dialogueKey = currentStep.dialogueKey;
        intent.dialogueDuration = currentStep.dialogueDuration;
        intent.isCharging = currentStep.isCharging;
        intent.chargeComment = currentStep.chargeComment;

        return intent;
    }
}