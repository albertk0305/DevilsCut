using UnityEngine;

[System.Serializable]
public class EnemyActionIntent
{
    public string dialogueKey;             // 턴 시작 시 출력할 대사 (없으면 null/빈칸)
    public float dialogueDuration = 1.5f;  // 대사 출력 시간

    public bool isCharging;                // 기 모으기 패턴인지 여부
    public string chargeComment;           // 기 모을 때 출력할 시스템 텍스트

    public SkillData skillToUse;           // 실제로 사용할 스킬 (기만 모으고 턴을 넘긴다면 null)
}

// 모든 적 AI 스크립트의 '부모'가 될 추상 클래스
public abstract class EnemyAIBase : ScriptableObject
{
    public abstract EnemyActionIntent DecideNextAction(int currentTurnCount, PlayerStats pStats, EnemyData enemy);
}