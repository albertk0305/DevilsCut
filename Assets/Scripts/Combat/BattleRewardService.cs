using UnityEngine;

public static class BattleRewardService
{
    public static VictoryRewardGrantResult GrantReward(PlayerManager playerManager, BattleReward reward)
    {
        VictoryRewardGrantResult result = new VictoryRewardGrantResult();

        if (playerManager == null)
        {
            Debug.LogError("BattleRewardService: PlayerManager is null.");
            return result;
        }

        if (reward == null)
        {
            Debug.LogWarning("BattleRewardService: reward is null.");
            result.levelUpResult = LevelUpService.ProcessLevelUps(playerManager.stats);
            return result;
        }

        ModifiedBattleRewardResult modifiedReward = ApplyVictoryRewardModifiers(playerManager, reward);

        result.rewardModifierResult = modifiedReward;
        result.expGranted = modifiedReward != null ? modifiedReward.finalExp : 0;
        result.goldGranted = modifiedReward != null ? modifiedReward.finalGold : 0;
        result.keysGranted = 0;

        int beforeLevel = playerManager.stats.level;
        int beforeMaxHp = playerManager.stats.maxHp;
        int beforeCurrentHp = playerManager.stats.currentHp;
        int beforeStrength = playerManager.stats.strength;

        playerManager.stats.currentExp += result.expGranted;
        playerManager.stats.currentGold += result.goldGranted;
        result.levelUpResult = LevelUpService.ProcessLevelUps(playerManager.stats);

        if (result.levelUpResult != null && result.levelUpResult.HasLevelUp)
        {
            DevLog.Log($"[VictoryReward] LevelUp applied to PlayerManager.stats: Lv.{beforeLevel} -> {playerManager.stats.level}, maxHp {beforeMaxHp} -> {playerManager.stats.maxHp}, currentHp {beforeCurrentHp} -> {playerManager.stats.currentHp}, strength {beforeStrength} -> {playerManager.stats.strength}");
        }

        // Key reward is still handled by ExplorationManager battle progress.
        return result;
    }

    public static ModifiedBattleRewardResult ApplyVictoryRewardModifiers(PlayerManager playerManager, BattleReward reward)
    {
        if (reward == null)
            return SupporterBattleRewardModifierService.ApplySupporterRewardModifiers(playerManager, 0, 0);

        return SupporterBattleRewardModifierService.ApplySupporterRewardModifiers(playerManager, reward.exp, reward.gold);
    }
}

public class VictoryRewardGrantResult
{
    public int expGranted;
    public int goldGranted;
    public int keysGranted;
    public ModifiedBattleRewardResult rewardModifierResult;
    public LevelUpResult levelUpResult;
}
