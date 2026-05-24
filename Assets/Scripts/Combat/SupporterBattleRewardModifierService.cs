using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class ModifiedBattleRewardResult
{
    public int baseExp;
    public int finalExp;
    public int expBonus;
    public int baseGold;
    public int finalGold;
    public int goldBonus;
    public readonly List<string> bonusMessages = new List<string>();

    public string BuildFinalRewardLine()
    {
        return $"EXP {FormatRewardAmount(finalExp, expBonus)} / Gold {FormatRewardAmount(finalGold, goldBonus)} \uD68D\uB4DD!";
    }

    public string BuildBonusMessage()
    {
        if (bonusMessages.Count == 0)
            return "";

        StringBuilder builder = new StringBuilder();

        foreach (string message in bonusMessages)
        {
            if (!string.IsNullOrEmpty(message))
                builder.AppendLine(message);
        }

        builder.Append(BuildFinalRewardLine());
        return builder.ToString();
    }

    private static string FormatRewardAmount(int amount, int bonus)
    {
        if (bonus > 0)
            return $"{amount} (+{bonus})";

        return amount.ToString();
    }
}

public static class SupporterBattleRewardModifierService
{
    private const string MammonSupporterId = "mammon";
    private const string SatanSupporterId = "satan";

    public static ModifiedBattleRewardResult ApplySupporterRewardModifiers(PlayerManager playerManager, int baseExp, int baseGold)
    {
        ModifiedBattleRewardResult result = new ModifiedBattleRewardResult
        {
            baseExp = Mathf.Max(0, baseExp),
            baseGold = Mathf.Max(0, baseGold)
        };

        result.expBonus = CalculateExpBonus(playerManager, result.baseExp);
        result.goldBonus = CalculateGoldBonus(playerManager, result.baseGold);
        result.finalExp = result.baseExp + result.expBonus;
        result.finalGold = result.baseGold + result.goldBonus;

        if (result.goldBonus > 0)
            result.bonusMessages.Add("\uB9C8\uBAAC\uC758 \uD328\uC2DC\uBE0C\uB85C \uACE8\uB4DC \uBCF4\uC0C1 \uC99D\uAC00!");

        if (result.expBonus > 0)
            result.bonusMessages.Add("\uC0AC\uD0C4\uC758 \uD328\uC2DC\uBE0C\uB85C \uACBD\uD5D8\uCE58 \uBCF4\uC0C1 \uC99D\uAC00!");

        return result;
    }

    private static int CalculateExpBonus(PlayerManager playerManager, int baseExp)
    {
        SupporterData satan = FindUnlockedSupporter(playerManager, SatanSupporterId);
        if (satan == null || satan.passiveLevel <= 0 || baseExp <= 0)
            return 0;

        return Mathf.FloorToInt(baseExp * GetRewardBonusRatio(satan.passiveLevel));
    }

    private static int CalculateGoldBonus(PlayerManager playerManager, int baseGold)
    {
        SupporterData mammon = FindUnlockedSupporter(playerManager, MammonSupporterId);
        if (mammon == null || mammon.passiveLevel <= 0 || baseGold <= 0)
            return 0;

        return Mathf.FloorToInt(baseGold * GetRewardBonusRatio(mammon.passiveLevel));
    }

    private static SupporterData FindUnlockedSupporter(PlayerManager playerManager, string supporterId)
    {
        if (playerManager == null || playerManager.unlockedSupporters == null || string.IsNullOrEmpty(supporterId))
            return null;

        foreach (SupporterData supporter in playerManager.unlockedSupporters)
        {
            if (supporter != null && supporter.supporterID == supporterId)
                return supporter;
        }

        return null;
    }

    private static float GetRewardBonusRatio(int passiveLevel)
    {
        switch (Mathf.Clamp(passiveLevel, 1, 3))
        {
            case 1:
                return 0.10f;
            case 2:
                return 0.20f;
            default:
                return 0.35f;
        }
    }
}
