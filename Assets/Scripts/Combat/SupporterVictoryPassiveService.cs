using System.Collections.Generic;
using UnityEngine;

public class SupporterPassiveRewardResult
{
    public string message;
    public SupporterData supporterData;

    public SupporterPassiveRewardResult(string message)
        : this(message, null)
    {
    }

    public SupporterPassiveRewardResult(string message, SupporterData supporterData)
    {
        this.message = message;
        this.supporterData = supporterData;
    }
}

public class LeviathanGiftResult
{
    public EquipmentItemData giftItem;
    public List<ItemMergeResult> mergeResults;
    public string message;
    public SupporterData supporterData;

    public LeviathanGiftResult(EquipmentItemData giftItem, List<ItemMergeResult> mergeResults, string message)
        : this(giftItem, mergeResults, message, null)
    {
    }

    public LeviathanGiftResult(EquipmentItemData giftItem, List<ItemMergeResult> mergeResults, string message, SupporterData supporterData)
    {
        this.giftItem = giftItem;
        this.mergeResults = mergeResults;
        this.message = message;
        this.supporterData = supporterData;
    }
}

public static class SupporterVictoryPassiveService
{
    private const string AsmodeusSupporterId = "asmodeus";
    private const string BeelzebubSupporterId = "baalzebub";
    private const string LuciferSupporterId = "lucifer";
    private const string LeviathanSupporterId = "leviathan";

    public static List<SupporterPassiveRewardResult> ResolvePostRewardPassives(PlayerManager playerManager)
    {
        List<SupporterPassiveRewardResult> results = new List<SupporterPassiveRewardResult>();

        if (playerManager == null)
            return results;

        ResolveAsmodeusPassive(playerManager, results);
        ResolveBeelzebubPassive(playerManager, results);
        ResolveLuciferPassive(playerManager, results);

        return results;
    }

    public static LeviathanGiftResult TryResolveLeviathanGift(PlayerManager playerManager, ItemDatabase itemDatabase)
    {
        if (playerManager == null || itemDatabase == null || itemDatabase.allItems == null)
            return null;

        SupporterData leviathan = FindUnlockedSupporter(playerManager, LeviathanSupporterId);
        if (leviathan == null || leviathan.passiveLevel <= 0)
            return null;

        int passiveLevel = Mathf.Clamp(leviathan.passiveLevel, 1, 3);
        float triggerChance = GetLeviathanTriggerChance(passiveLevel);

        if (Random.value >= triggerChance)
            return null;

        EquipmentItemData giftItem = SelectRandomLeviathanGiftItem(playerManager, itemDatabase);
        if (giftItem == null)
            return null;

        List<ItemMergeResult> mergeResults = playerManager.AcquireItemAndGetMergeResults(giftItem);
        return new LeviathanGiftResult(giftItem, mergeResults, null, leviathan);
    }

    private static void ResolveAsmodeusPassive(PlayerManager playerManager, List<SupporterPassiveRewardResult> results)
    {
        SupporterData asmodeus = FindUnlockedSupporter(playerManager, AsmodeusSupporterId);
        if (asmodeus == null || asmodeus.passiveLevel <= 0)
            return;

        int passiveLevel = Mathf.Clamp(asmodeus.passiveLevel, 1, 3);
        float triggerChance = GetAsmodeusTriggerChance(passiveLevel);

        if (Random.value >= triggerChance)
            return;

        int statGain = passiveLevel;
        int hpGain = 91 * passiveLevel;
        int statIndex = Random.Range(0, 4);

        switch (statIndex)
        {
            case 0:
                playerManager.stats.maxHp += hpGain;
                playerManager.stats.currentHp += hpGain;
                results.Add(new SupporterPassiveRewardResult(FormatLocalizedText("combat_victory_asmodeus_max_hp_gain_format", "아스모데우스의 패시브 발동!\n최대 체력이 {0} 증가했습니다.", hpGain)));
                break;
            case 1:
                playerManager.stats.breakResistance += statGain;
                results.Add(new SupporterPassiveRewardResult(FormatLocalizedText("combat_victory_asmodeus_break_resistance_gain_format", "아스모데우스의 패시브 발동!\n브레이크 저항이 {0} 증가했습니다.", statGain)));
                break;
            case 2:
                playerManager.stats.maxBreakGauge += statGain;
                results.Add(new SupporterPassiveRewardResult(FormatLocalizedText("combat_victory_asmodeus_max_break_gain_format", "아스모데우스의 패시브 발동!\n최대 브레이크 게이지가 {0} 증가했습니다.", statGain)));
                break;
            default:
                playerManager.stats.ActionPoints += statGain;
                results.Add(new SupporterPassiveRewardResult(FormatLocalizedText("combat_victory_asmodeus_ap_gain_format", "아스모데우스의 패시브 발동!\nAP가 {0} 증가했습니다.", statGain)));
                break;
        }

        AttachSupporterToLastResult(results, asmodeus);
    }

    private static SupporterData FindUnlockedSupporter(PlayerManager playerManager, string supporterId)
    {
        if (playerManager.unlockedSupporters == null || string.IsNullOrEmpty(supporterId))
            return null;

        foreach (SupporterData supporter in playerManager.unlockedSupporters)
        {
            if (supporter != null && supporter.supporterID == supporterId)
                return supporter;
        }

        return null;
    }

    private static void ResolveBeelzebubPassive(PlayerManager playerManager, List<SupporterPassiveRewardResult> results)
    {
        SupporterData beelzebub = FindUnlockedSupporter(playerManager, BeelzebubSupporterId);
        if (beelzebub == null || beelzebub.passiveLevel <= 0)
            return;

        int passiveLevel = Mathf.Clamp(beelzebub.passiveLevel, 1, 3);
        float healRatio = GetBeelzebubHealRatio(passiveLevel);
        int finalMaxHp = playerManager.GetItemModifiedStats().maxHp;

        if (finalMaxHp <= 0)
            return;

        if (playerManager.stats.currentHp >= finalMaxHp)
        {
            playerManager.stats.currentHp = Mathf.Min(playerManager.stats.currentHp, finalMaxHp);
            return;
        }

        int requestedHeal = Mathf.FloorToInt(finalMaxHp * healRatio);
        int actualHeal = Mathf.Min(requestedHeal, finalMaxHp - playerManager.stats.currentHp);

        if (actualHeal <= 0)
            return;

        playerManager.stats.currentHp = Mathf.Clamp(playerManager.stats.currentHp + actualHeal, 0, finalMaxHp);
        results.Add(new SupporterPassiveRewardResult(FormatLocalizedText("combat_victory_baalzebub_heal_format", "바알제붑의 패시브 발동!\n체력이 {0} 회복되었습니다.", actualHeal)));
        AttachSupporterToLastResult(results, beelzebub);
    }

    private static float GetBeelzebubHealRatio(int passiveLevel)
    {
        switch (passiveLevel)
        {
            case 1:
                return 0.05f;
            case 2:
                return 0.10f;
            default:
                return 0.20f;
        }
    }

    private static void ResolveLuciferPassive(PlayerManager playerManager, List<SupporterPassiveRewardResult> results)
    {
        SupporterData lucifer = FindUnlockedSupporter(playerManager, LuciferSupporterId);
        if (lucifer == null || lucifer.passiveLevel <= 0)
            return;

        int passiveLevel = Mathf.Clamp(lucifer.passiveLevel, 1, 3);
        float triggerChance = GetLuciferTriggerChance(passiveLevel);

        if (Random.value >= triggerChance)
            return;

        int statGain = passiveLevel;
        int statIndex = Random.Range(0, 4);

        switch (statIndex)
        {
            case 0:
                playerManager.stats.strength += statGain;
                results.Add(new SupporterPassiveRewardResult(FormatLocalizedText("combat_victory_lucifer_strength_gain_format", "루시퍼의 패시브 발동!\n힘이 {0} 증가했습니다.", statGain)));
                break;
            case 1:
                playerManager.stats.defense += statGain;
                results.Add(new SupporterPassiveRewardResult(FormatLocalizedText("combat_victory_lucifer_defense_gain_format", "루시퍼의 패시브 발동!\n방어력이 {0} 증가했습니다.", statGain)));
                break;
            case 2:
                playerManager.stats.speed += statGain;
                results.Add(new SupporterPassiveRewardResult(FormatLocalizedText("combat_victory_lucifer_speed_gain_format", "루시퍼의 패시브 발동!\n속도가 {0} 증가했습니다.", statGain)));
                break;
            default:
                playerManager.stats.luck += statGain;
                results.Add(new SupporterPassiveRewardResult(FormatLocalizedText("combat_victory_lucifer_luck_gain_format", "루시퍼의 패시브 발동!\n운이 {0} 증가했습니다.", statGain)));
                break;
        }

        AttachSupporterToLastResult(results, lucifer);
    }

    private static void AttachSupporterToLastResult(List<SupporterPassiveRewardResult> results, SupporterData supporterData)
    {
        if (results == null || results.Count == 0)
            return;

        results[results.Count - 1].supporterData = supporterData;
    }

    private static float GetLuciferTriggerChance(int passiveLevel)
    {
        switch (passiveLevel)
        {
            case 1:
                return 0.10f;
            case 2:
                return 0.15f;
            default:
                return 0.25f;
        }
    }

    private static EquipmentItemData SelectRandomLeviathanGiftItem(PlayerManager playerManager, ItemDatabase itemDatabase)
    {
        List<EquipmentItemData> candidates = new List<EquipmentItemData>();

        foreach (EquipmentItemData item in itemDatabase.allItems)
        {
            if (IsLeviathanGiftCandidateAvailable(playerManager, item))
                candidates.Add(item);
        }

        if (candidates.Count == 0)
            return null;

        return candidates[Random.Range(0, candidates.Count)];
    }

    private static bool IsLeviathanGiftCandidateAvailable(PlayerManager playerManager, EquipmentItemData item)
    {
        if (item == null || string.IsNullOrEmpty(item.itemID))
            return false;

        if (item.grade != ItemGrade.Common)
            return false;

        return GetStarEquivalent(playerManager, item.itemID) < 9;
    }

    private static int GetStarEquivalent(PlayerManager playerManager, string itemId)
    {
        if (playerManager == null || playerManager.inventory == null || string.IsNullOrEmpty(itemId))
            return 0;

        int starEquivalent = 0;

        foreach (OwnedItem owned in playerManager.inventory)
        {
            if (owned == null || owned.data == null || owned.data.itemID != itemId)
                continue;

            if (owned.starLevel <= 1)
                starEquivalent += 1;
            else if (owned.starLevel == 2)
                starEquivalent += 3;
            else
                starEquivalent += 9;
        }

        return starEquivalent;
    }

    private static float GetLeviathanTriggerChance(int passiveLevel)
    {
        switch (passiveLevel)
        {
            case 1:
                return 0.10f;
            case 2:
                return 0.20f;
            default:
                return 0.35f;
        }
    }

    private static float GetAsmodeusTriggerChance(int passiveLevel)
    {
        switch (passiveLevel)
        {
            case 1:
                return 0.10f;
            case 2:
                return 0.15f;
            default:
                return 0.25f;
        }
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
