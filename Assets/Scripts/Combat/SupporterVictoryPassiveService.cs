using System.Collections.Generic;
using UnityEngine;

public class SupporterPassiveRewardResult
{
    public string message;

    public SupporterPassiveRewardResult(string message)
    {
        this.message = message;
    }
}

public class LeviathanGiftResult
{
    public EquipmentItemData giftItem;
    public List<ItemMergeResult> mergeResults;
    public string message;

    public LeviathanGiftResult(EquipmentItemData giftItem, List<ItemMergeResult> mergeResults, string message)
    {
        this.giftItem = giftItem;
        this.mergeResults = mergeResults;
        this.message = message;
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
        return new LeviathanGiftResult(giftItem, mergeResults, null);
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
                results.Add(new SupporterPassiveRewardResult($"\uC544\uC2A4\uBAA8\uB370\uC6B0\uC2A4\uC758 \uD328\uC2DC\uBE0C \uBC1C\uB3D9!\n\uCD5C\uB300 \uCCB4\uB825\uC774 {hpGain} \uC99D\uAC00\uD588\uC2B5\uB2C8\uB2E4."));
                break;
            case 1:
                playerManager.stats.breakResistance += statGain;
                results.Add(new SupporterPassiveRewardResult($"\uC544\uC2A4\uBAA8\uB370\uC6B0\uC2A4\uC758 \uD328\uC2DC\uBE0C \uBC1C\uB3D9!\n\uBE0C\uB808\uC774\uD06C \uC800\uD56D\uC774 {statGain} \uC99D\uAC00\uD588\uC2B5\uB2C8\uB2E4."));
                break;
            case 2:
                playerManager.stats.maxBreakGauge += statGain;
                results.Add(new SupporterPassiveRewardResult($"\uC544\uC2A4\uBAA8\uB370\uC6B0\uC2A4\uC758 \uD328\uC2DC\uBE0C \uBC1C\uB3D9!\n\uCD5C\uB300 \uBE0C\uB808\uC774\uD06C \uAC8C\uC774\uC9C0\uAC00 {statGain} \uC99D\uAC00\uD588\uC2B5\uB2C8\uB2E4."));
                break;
            default:
                playerManager.stats.ActionPoints += statGain;
                results.Add(new SupporterPassiveRewardResult($"\uC544\uC2A4\uBAA8\uB370\uC6B0\uC2A4\uC758 \uD328\uC2DC\uBE0C \uBC1C\uB3D9!\nAP\uAC00 {statGain} \uC99D\uAC00\uD588\uC2B5\uB2C8\uB2E4."));
                break;
        }
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
        results.Add(new SupporterPassiveRewardResult($"\uBC14\uC54C\uC81C\uBD91\uC758 \uD328\uC2DC\uBE0C \uBC1C\uB3D9!\n\uCCB4\uB825\uC774 {actualHeal} \uD68C\uBCF5\uB418\uC5C8\uC2B5\uB2C8\uB2E4."));
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
                results.Add(new SupporterPassiveRewardResult($"\uB8E8\uC2DC\uD37C\uC758 \uD328\uC2DC\uBE0C \uBC1C\uB3D9!\n\uD798\uC774 {statGain} \uC99D\uAC00\uD588\uC2B5\uB2C8\uB2E4."));
                break;
            case 1:
                playerManager.stats.defense += statGain;
                results.Add(new SupporterPassiveRewardResult($"\uB8E8\uC2DC\uD37C\uC758 \uD328\uC2DC\uBE0C \uBC1C\uB3D9!\n\uBC29\uC5B4\uB825\uC774 {statGain} \uC99D\uAC00\uD588\uC2B5\uB2C8\uB2E4."));
                break;
            case 2:
                playerManager.stats.speed += statGain;
                results.Add(new SupporterPassiveRewardResult($"\uB8E8\uC2DC\uD37C\uC758 \uD328\uC2DC\uBE0C \uBC1C\uB3D9!\n\uC18D\uB3C4\uAC00 {statGain} \uC99D\uAC00\uD588\uC2B5\uB2C8\uB2E4."));
                break;
            default:
                playerManager.stats.luck += statGain;
                results.Add(new SupporterPassiveRewardResult($"\uB8E8\uC2DC\uD37C\uC758 \uD328\uC2DC\uBE0C \uBC1C\uB3D9!\n\uC6B4\uC774 {statGain} \uC99D\uAC00\uD588\uC2B5\uB2C8\uB2E4."));
                break;
        }
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
}
