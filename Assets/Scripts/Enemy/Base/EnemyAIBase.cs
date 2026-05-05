using UnityEngine;

// 모든 적 AI 스크립트의 '부모'가 될 추상 클래스입니다.
public abstract class EnemyAIBase : ScriptableObject
{
    // 현재 턴 수, 아군 스탯, 적 스탯을 보고 무슨 스킬을 쓸지 결정해서 반환합니다.
    public abstract SkillData DecideNextSkill(int currentTurnCount, PlayerStats pStats, EnemyData enemy);
}