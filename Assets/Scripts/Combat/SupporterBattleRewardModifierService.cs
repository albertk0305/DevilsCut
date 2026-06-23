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
        return FormatLocalizedText(
            "combat_victory_reward_result_format",
            "EXP {0} / Gold {1} 획득!",
            FormatRewardAmount(finalExp, expBonus),
            FormatRewardAmount(finalGold, goldBonus));
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

    private static string GetLocalizedText(string key, string fallback)
    {
        if (!string.IsNullOrEmpty(key) && LocalizationManager.Instance != null)
        {
            string localized = LocalizationManager.Instance.GetText(key);
            if (!string.IsNullOrEmpty(localized) && localized != key)
                return localized;
        }

        if (!string.IsNullOrEmpty(fallback))
            return fallback;

        return key ?? "";
    }

    public static string GetLocalizedRewardText(string key, string fallback)
    {
        return GetLocalizedText(key, fallback);
    }

    private static string FormatLocalizedText(string key, string fallback, params object[] args)
    {
        string format = GetLocalizedText(key, fallback);
        try
        {
            return KoreanParticleFormatter.Format(format, args);
        }
        catch (System.FormatException)
        {
            try
            {
                return KoreanParticleFormatter.Format(fallback, args);
            }
            catch (System.FormatException)
            {
                return fallback ?? "";
            }
        }
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
            result.bonusMessages.Add(ModifiedBattleRewardResult.GetLocalizedRewardText("combat_victory_mammon_gold_bonus", "마몬의 패시브로 골드 보상 증가!"));

        if (result.expBonus > 0)
            result.bonusMessages.Add(ModifiedBattleRewardResult.GetLocalizedRewardText("combat_victory_satan_exp_bonus", "사탄의 패시브로 경험치 보상 증가!"));

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
