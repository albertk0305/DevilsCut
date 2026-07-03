using UnityEngine;

public static class InfiniteBattleEncounterBuilder
{
    public static bool PrepareCurrentFloorEncounter()
    {
        if (!InfiniteBattleRunContext.IsRunPrepared)
            return false;

        PlayerManager playerManager = PlayerManager.Instance;
        if (playerManager == null)
        {
            DevLog.LogWarning("[InfiniteBattle] Cannot prepare encounter: PlayerManager missing.");
            return false;
        }

        BossEncounterData boss = InfiniteBattleRunContext.GetNextBossForCurrentFloor();
        if (boss == null || boss.bossEnemy == null)
        {
            DevLog.LogWarning($"[InfiniteBattle] Cannot prepare floor {InfiniteBattleRunContext.CurrentFloor}: boss data missing.");
            return false;
        }

        EnemyStatBlock scaledStats = BuildScaledStats(InfiniteBattleRunContext.Config, InfiniteBattleRunContext.CurrentFloor);
        EnemyData runtimeEnemy = EnemyInstanceFactory.CreateRuntimeEnemy(boss.bossEnemy, scaledStats);
        if (runtimeEnemy == null)
            return false;

        playerManager.currentEnemyToFight = runtimeEnemy;
        playerManager.currentBattleReward = new BattleReward();
        playerManager.currentBattleType = BattleType.FinalBoss;
        playerManager.currentBattlePhase = InfiniteBattleRunContext.CurrentFloor;
        playerManager.pendingAdvanceBattleTurn = false;
        playerManager.suppressPendingBattleProgress = true;

        DevLog.Log($"[InfiniteBattle] Prepared floor {InfiniteBattleRunContext.CurrentFloor}: boss={boss.bossID}, hp={runtimeEnemy.maxHp}, str={runtimeEnemy.strength}");
        return true;
    }

    private static EnemyStatBlock BuildScaledStats(InfiniteBattleConfig config, int floor)
    {
        EnemyStatBlock floor1Stats = config != null ? config.Floor1Stats : null;
        if (floor1Stats == null)
            floor1Stats = new EnemyStatBlock();

        float growthPercent = config != null ? config.GrowthPercentPerFloor : 0f;
        float multiplier = Mathf.Pow(1f + growthPercent / 100f, Mathf.Max(0, floor - 1));

        return new EnemyStatBlock
        {
            level = ScaleInt(floor1Stats.level, multiplier),
            maxHp = ScaleInt(floor1Stats.maxHp, multiplier),
            actionPoints = ScaleInt(floor1Stats.actionPoints, multiplier),
            breakResistance = ScaleInt(floor1Stats.breakResistance, multiplier),
            maxBreakGauge = ScaleFloat(floor1Stats.maxBreakGauge, multiplier),
            strength = ScaleInt(floor1Stats.strength, multiplier),
            defense = ScaleInt(floor1Stats.defense, multiplier),
            speed = ScaleInt(floor1Stats.speed, multiplier),
            luck = ScaleInt(floor1Stats.luck, multiplier)
        };
    }

    private static int ScaleInt(int floor1Value, float multiplier)
    {
        return Mathf.Max(0, Mathf.RoundToInt(floor1Value * multiplier));
    }

    private static float ScaleFloat(float floor1Value, float multiplier)
    {
        return Mathf.Max(0f, floor1Value * multiplier);
    }
}
