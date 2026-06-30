using UnityEngine;

public class CombatEncounterBuilder : MonoBehaviour
{
    [SerializeField] private BattleBalanceDatabase battleBalanceDatabase;

    public bool PrepareEncounter(EnemyData enemyTemplate, BattleType battleType, int phase)
    {
        if (battleBalanceDatabase == null)
        {
            Debug.LogError("CombatEncounterBuilder: BattleBalanceDatabase가 연결되지 않았습니다.");
            return false;
        }

        if (enemyTemplate == null)
        {
            Debug.LogError("CombatEncounterBuilder: enemyTemplate이 null입니다.");
            return false;
        }

        PhaseBattleBalance phaseBalance = battleBalanceDatabase.GetPhaseBalance(phase);
        if (phaseBalance == null)
            return false;

        EnemyStatBlock baseStats = phaseBalance.GetStats(battleType);
        BattleReward reward = phaseBalance.GetReward(battleType);

        EnemyData runtimeEnemy = EnemyInstanceFactory.CreateRuntimeEnemy(enemyTemplate, baseStats);
        if (runtimeEnemy == null)
            return false;

        PlayerManager playerManager = PlayerManager.Instance;
        if (playerManager == null)
        {
            Debug.LogError("CombatEncounterBuilder: PlayerManager.Instance가 없습니다.");
            return false;
        }

        playerManager.currentEnemyToFight = runtimeEnemy;
        playerManager.currentBattleReward = reward;
        playerManager.currentBattleType = battleType;
        playerManager.currentBattlePhase = phase;

        return true;
    }

    public bool PrepareHiddenBossEncounter(BossEncounterData bossEncounter, string hiddenBossID)
    {
        if (bossEncounter == null)
        {
            Debug.LogError("CombatEncounterBuilder: hidden boss encounter is null.");
            return false;
        }

        bool prepared = PrepareEncounter(bossEncounter.bossEnemy, BattleType.FinalBoss, HiddenBossConstants.BaitoPhase);
        if (!prepared)
            return false;

        PlayerManager.Instance.BeginHiddenBossBattle(hiddenBossID, bossEncounter);
        return true;
    }
}
