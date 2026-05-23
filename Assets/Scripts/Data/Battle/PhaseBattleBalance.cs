using System;

[Serializable]
public class PhaseBattleBalance
{
    public int phase;

    public BattleReward generalBattleReward;
    public BattleReward bossBattleReward;
    public BattleReward finalBossReward;

    public EnemyStatBlock generalEnemyStats;
    public EnemyStatBlock bossEnemyStats;
    public EnemyStatBlock finalBossStats;

    public EnemyStatBlock GetStats(BattleType battleType)
    {
        switch (battleType)
        {
            case BattleType.Boss:
                return bossEnemyStats;
            case BattleType.FinalBoss:
                return finalBossStats;
            default:
                return generalEnemyStats;
        }
    }

    public BattleReward GetReward(BattleType battleType)
    {
        switch (battleType)
        {
            case BattleType.Boss:
                return bossBattleReward;
            case BattleType.FinalBoss:
                return finalBossReward;
            default:
                return generalBattleReward;
        }
    }
}