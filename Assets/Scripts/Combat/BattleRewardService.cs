using UnityEngine;

public static class BattleRewardService
{
    public static void GrantReward(PlayerManager playerManager, BattleReward reward)
    {
        if (playerManager == null)
        {
            Debug.LogError("BattleRewardService: PlayerManager가 null입니다.");
            return;
        }

        if (reward == null)
        {
            Debug.LogWarning("BattleRewardService: 지급할 보상이 없습니다.");
            return;
        }

        playerManager.stats.currentExp += reward.exp;
        playerManager.stats.currentGold += reward.gold;

        // TODO: keys, item reward, level up 처리 연결
    }
}