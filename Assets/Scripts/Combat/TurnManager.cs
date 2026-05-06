using UnityEngine;
using System.Collections.Generic;

// [분리] CombatManager에 있던 클래스를 이쪽으로 이사했습니다.
[System.Serializable]
public class TurnEntity
{
    public string entityName;
    public int ap;
    public float actionGauge;
    public bool isPlayer;
    public float speedMultiplier = 1f;
    public Sprite portraitIcon;
}

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance;

    public List<TurnEntity> turnQueue = new List<TurnEntity>();
    private List<TurnEntity> simQueue = new List<TurnEntity>();
    private List<Sprite> futureTurnIcons = new List<Sprite>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    // 1. 대기열 초기화
    public void ClearQueue()
    {
        turnQueue.Clear();
    }

    // 2. 대기열에 캐릭터 추가
    public void AddEntity(string name, int ap, bool isPlayer, float speedMult, Sprite icon)
    {
        turnQueue.Add(new TurnEntity
        {
            entityName = name,
            ap = ap,
            actionGauge = 0,
            isPlayer = isPlayer,
            speedMultiplier = speedMult,
            portraitIcon = icon
        });
    }

    // 3. 내부 수학 공식
    private float GetGaugeFillAmount(int ap)
    {
        return 20f * (ap / (ap + 100f));
    }

    private float GetTicksToNextTurn(TurnEntity entity)
    {
        float fillPerTick = GetGaugeFillAmount(entity.ap) * entity.speedMultiplier;
        if (fillPerTick <= 0) return 9999f;
        return (100f - entity.actionGauge) / fillPerTick;
    }

    // 4. 다음 턴 계산 및 결과 반환
    public TurnEntity CalculateAndGetNextTurn()
    {
        turnQueue.Sort((a, b) =>
        {
            float aTicks = GetTicksToNextTurn(a);
            float bTicks = GetTicksToNextTurn(b);
            int compare = aTicks.CompareTo(bTicks);
            if (compare == 0) return a.isPlayer ? -1 : 1;
            return compare;
        });

        TurnEntity nextTurnEntity = turnQueue[0];
        float ticksToAdvance = GetTicksToNextTurn(nextTurnEntity);

        if (ticksToAdvance > 0)
        {
            foreach (var entity in turnQueue)
            {
                entity.actionGauge += GetGaugeFillAmount(entity.ap) * entity.speedMultiplier * ticksToAdvance;
            }
        }

        nextTurnEntity.actionGauge -= 100f;
        return nextTurnEntity; // 턴을 획득한 캐릭터를 CombatManager에게 보고합니다!
    }

    // 5. 미래 예측 UI용 이미지 리스트 반환
    public List<Sprite> GetFutureTurnIcons(int count)
    {
        futureTurnIcons.Clear();
        while (simQueue.Count < turnQueue.Count) simQueue.Add(new TurnEntity());

        for (int i = 0; i < turnQueue.Count; i++)
        {
            simQueue[i].entityName = turnQueue[i].entityName;
            simQueue[i].ap = turnQueue[i].ap;
            simQueue[i].actionGauge = turnQueue[i].actionGauge;
            simQueue[i].isPlayer = turnQueue[i].isPlayer;
            simQueue[i].speedMultiplier = turnQueue[i].speedMultiplier;
            simQueue[i].portraitIcon = turnQueue[i].portraitIcon;
        }

        while (futureTurnIcons.Count < count)
        {
            simQueue.Sort((a, b) =>
            {
                float aTicks = GetTicksToNextTurn(a);
                float bTicks = GetTicksToNextTurn(b);
                int compare = aTicks.CompareTo(bTicks);
                if (compare == 0) return a.isPlayer ? -1 : 1;
                return compare;
            });

            TurnEntity nextSimEntity = simQueue[0];
            float ticksToAdvance = GetTicksToNextTurn(nextSimEntity);

            foreach (var e in simQueue)
            {
                e.actionGauge += GetGaugeFillAmount(e.ap) * e.speedMultiplier * ticksToAdvance;
            }

            futureTurnIcons.Add(nextSimEntity.portraitIcon);
            nextSimEntity.actionGauge -= 100f;
        }

        return futureTurnIcons; // 예측된 초상화 리스트만 CombatManager에게 전달합니다!
    }

    public void ResetGauge(string targetName)
    {
        foreach (var entity in turnQueue)
        {
            if (entity.entityName == targetName)
            {
                entity.actionGauge = 0f;
                DevLog.Log($"[{targetName}]의 행동 게이지가 0으로 강제 초기화되었습니다!");
                break;
            }
        }
    }
}