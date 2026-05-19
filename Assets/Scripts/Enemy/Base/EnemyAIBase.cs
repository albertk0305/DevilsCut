using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class EnemyActionIntent
{
    public SkillData skillToUse; // 이것만 남기고 다 지워도 완벽하게 돌아갑니다!
}

// 모든 적 AI 스크립트의 '부모'가 될 추상 클래스
public abstract class EnemyAIBase : ScriptableObject
{
    public abstract EnemyActionIntent DecideNextAction(int currentTurnCount, PlayerStats pStats, EnemyData enemy);

    // [신규 추가] 체력 변동 등 이벤트 발생 시 패시브 스탯을 갱신하기 위한 가상 함수
    public virtual void UpdatePassives(EnemyData enemy) { }

    public virtual List<SkillData> GetEnemySkills()
    {
        return new List<SkillData>();
    }
}