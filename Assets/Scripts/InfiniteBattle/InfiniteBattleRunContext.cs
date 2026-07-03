using System.Collections.Generic;
using UnityEngine;

public static class InfiniteBattleRunContext
{
    private static readonly List<BossEncounterData> currentBlockMidBossQueue = new List<BossEncounterData>();

    public static string ClearId { get; private set; }
    public static int ClearNumber { get; private set; }
    public static GameClearRecordData Record { get; private set; }
    public static ClearRecordPlayerProfile Profile { get; private set; }
    public static InfiniteBattleConfig Config { get; private set; }
    public static int CurrentFloor { get; private set; }
    public static int ClearedFloorCount { get; private set; }
    public static int CurrentPlayerHp { get; private set; }
    public static int BestFloorBeforeRun { get; private set; }
    public static int HighestReachedFloor { get; private set; }
    public static bool IsRunPrepared { get; private set; }
    public static bool HasStartedRun { get; private set; }

    public static void Prepare(GameClearRecordData record, ClearRecordPlayerProfile profile, InfiniteBattleConfig config)
    {
        Clear();

        Record = record;
        Profile = profile;
        Config = config;
        ClearId = record != null ? record.clearId : "";
        ClearNumber = record != null ? record.clearNumber : 0;
        CurrentFloor = 1;
        ClearedFloorCount = 0;
        CurrentPlayerHp = 0;
        BestFloorBeforeRun = record != null ? Mathf.Max(0, record.infiniteBattleBestFloor) : 0;
        HighestReachedFloor = 0;
        IsRunPrepared = record != null && profile != null && config != null;
        HasStartedRun = false;
        currentBlockMidBossQueue.Clear();
    }

    public static void StartRunWithFullHeal(int maxHp)
    {
        CurrentFloor = Mathf.Max(1, CurrentFloor);
        ClearedFloorCount = 0;
        CurrentPlayerHp = Mathf.Max(1, maxHp);
        HighestReachedFloor = CurrentFloor;
        HasStartedRun = true;
    }

    public static void SetCurrentHpAfterBattle(int hp)
    {
        CurrentPlayerHp = Mathf.Max(0, hp);
    }

    public static void MarkCurrentFloorCleared()
    {
        ClearedFloorCount = Mathf.Max(ClearedFloorCount, CurrentFloor);
        HighestReachedFloor = Mathf.Max(HighestReachedFloor, CurrentFloor);
    }

    public static void AdvanceToNextFloor()
    {
        CurrentFloor = Mathf.Max(1, CurrentFloor + 1);
        HighestReachedFloor = Mathf.Max(HighestReachedFloor, CurrentFloor);
    }

    public static BossEncounterData GetNextBossForCurrentFloor()
    {
        if (Config == null)
            return null;

        int positionInBlock = ((Mathf.Max(1, CurrentFloor) - 1) % 10) + 1;
        if (positionInBlock >= 1 && positionInBlock <= 7)
        {
            if (positionInBlock == 1 || currentBlockMidBossQueue.Count == 0)
                RebuildMidBossQueue();

            if (currentBlockMidBossQueue.Count == 0)
                return null;

            BossEncounterData boss = currentBlockMidBossQueue[0];
            currentBlockMidBossQueue.RemoveAt(0);
            return boss;
        }

        if (positionInBlock == 8)
            return Config.FinalBoss;

        if (positionInBlock == 9)
            return Config.TrueFinalBoss;

        return Config.HiddenBoss;
    }

    public static void Clear()
    {
        bool wasRunPrepared = IsRunPrepared;

        if (Profile != null)
            Profile.Dispose();

        ClearId = "";
        ClearNumber = 0;
        Record = null;
        Profile = null;
        Config = null;
        CurrentFloor = 0;
        ClearedFloorCount = 0;
        CurrentPlayerHp = 0;
        BestFloorBeforeRun = 0;
        HighestReachedFloor = 0;
        IsRunPrepared = false;
        HasStartedRun = false;
        currentBlockMidBossQueue.Clear();

        if (wasRunPrepared && PlayerManager.Instance != null)
        {
            PlayerManager.Instance.suppressPendingBattleProgress = false;
            PlayerManager.Instance.pendingAdvanceBattleTurn = false;
            PlayerManager.Instance.currentEnemyToFight = null;
            PlayerManager.Instance.currentBattleReward = new BattleReward();
            PlayerManager.Instance.ClearCurrentHiddenBossBattleContext();
        }
    }

    private static void RebuildMidBossQueue()
    {
        currentBlockMidBossQueue.Clear();

        IReadOnlyList<BossEncounterData> midBosses = Config.MidBosses;
        if (midBosses == null)
            return;

        foreach (BossEncounterData boss in midBosses)
        {
            if (boss != null)
                currentBlockMidBossQueue.Add(boss);
        }

        for (int i = 0; i < currentBlockMidBossQueue.Count; i++)
        {
            int swapIndex = Random.Range(i, currentBlockMidBossQueue.Count);
            BossEncounterData temp = currentBlockMidBossQueue[i];
            currentBlockMidBossQueue[i] = currentBlockMidBossQueue[swapIndex];
            currentBlockMidBossQueue[swapIndex] = temp;
        }
    }
}
