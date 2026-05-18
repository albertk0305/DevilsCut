using UnityEngine;

[System.Serializable]
public class EnemyActionIntent
{
    public SkillData skillToUse; // 이것만 남기고 다 지워도 완벽하게 돌아갑니다!
}

// 모든 적 AI 스크립트의 '부모'가 될 추상 클래스
public abstract class EnemyAIBase : ScriptableObject
{
    public abstract EnemyActionIntent DecideNextAction(int currentTurnCount, PlayerStats pStats, EnemyData enemy);
}