using System.Collections.Generic;
using UnityEngine;

public static class InfiniteBattlePlayerApplier
{
    public static bool ApplyForNewRun(GameClearRecordData record, out int effectiveMaxHp)
    {
        effectiveMaxHp = 0;

        PlayerManager playerManager = PlayerManager.Instance;
        if (playerManager == null || record == null || record.playerGrowth == null)
            return false;

        ApplyPlayerGrowth(playerManager, record.playerGrowth);
        playerManager.currentEnemyToFight = null;
        playerManager.currentBattleReward = new BattleReward();
        playerManager.currentBattleType = default;
        playerManager.currentBattlePhase = 0;
        playerManager.pendingAdvanceBattleTurn = false;
        playerManager.pendingBattleType = default;
        playerManager.pendingBattlePhase = 0;
        playerManager.ClearCurrentHiddenBossBattleContext();

        effectiveMaxHp = playerManager.RecoverCurrentHpToEffectiveMax();
        return true;
    }

    public static void ApplyCurrentHp(int currentHp)
    {
        PlayerManager playerManager = PlayerManager.Instance;
        if (playerManager == null)
            return;

        PlayerStats effectiveStats = playerManager.GetItemModifiedStats();
        int maxHp = effectiveStats != null ? Mathf.Max(1, effectiveStats.maxHp) : Mathf.Max(1, playerManager.stats.maxHp);
        playerManager.stats.currentHp = Mathf.Clamp(currentHp, 0, maxHp);
    }

    private static void ApplyPlayerGrowth(PlayerManager playerManager, PlayerGrowthSaveData playerGrowth)
    {
        playerManager.stats = new PlayerStats
        {
            level = Mathf.Max(1, playerGrowth.level),
            maxExp = Mathf.Max(0, playerGrowth.maxExp),
            currentExp = Mathf.Max(0, playerGrowth.currentExp),
            maxHp = Mathf.Max(1, playerGrowth.maxHp),
            currentHp = Mathf.Max(0, playerGrowth.currentHp),
            ActionPoints = Mathf.Max(0, playerGrowth.actionPoints),
            breakResistance = Mathf.Max(0, playerGrowth.breakResistance),
            maxBreakGauge = Mathf.Max(0f, playerGrowth.maxBreakGauge),
            strength = Mathf.Max(1, playerGrowth.strength),
            defense = Mathf.Max(1, playerGrowth.defense),
            speed = Mathf.Max(1, playerGrowth.speed),
            luck = Mathf.Max(1, playerGrowth.luck),
            currentGold = Mathf.Max(0, playerGrowth.currentGold),
            rejectedSupporterCount = Mathf.Max(0, playerGrowth.rejectedSupporterCount),
            finalDamageAmp = 0f,
            finalDamageReduction = 0f,
            critRate = 0f,
            critDamage = 1.5f,
            lifeSteal = 0f,
            trueDamageConversion = 0f,
            bonusAccuracy = 0f,
            bonusEvasion = 0f,
            healingReceivedAmp = 0f
        };

        ApplyInventory(playerManager, playerGrowth);
        ApplySkills(playerManager, playerGrowth);
        ApplySupporters(playerManager, playerGrowth);
        ApplyKarinItems(playerManager, playerGrowth);
        ApplyHiddenBossClears(playerManager, playerGrowth);
    }

    private static void ApplyInventory(PlayerManager playerManager, PlayerGrowthSaveData playerGrowth)
    {
        playerManager.inventory.Clear();

        ItemDatabase itemDatabase = SaveManager.Instance != null ? SaveManager.Instance.itemDatabase : null;
        if (itemDatabase == null || itemDatabase.allItems == null || playerGrowth.inventory == null)
            return;

        foreach (SavedOwnedItem savedItem in playerGrowth.inventory)
        {
            if (savedItem == null || string.IsNullOrEmpty(savedItem.itemID))
                continue;

            EquipmentItemData item = FindItem(itemDatabase, savedItem.itemID);
            if (item != null)
                playerManager.inventory.Add(new OwnedItem(item, Mathf.Clamp(savedItem.starLevel, 1, 3)));
        }
    }

    private static void ApplySkills(PlayerManager playerManager, PlayerGrowthSaveData playerGrowth)
    {
        playerManager.unlockedSkills.Clear();

        SkillDatabase skillDatabase = SaveManager.Instance != null ? SaveManager.Instance.skillDatabase : null;
        if (skillDatabase == null || playerGrowth.skills == null)
            return;

        foreach (SavedSkillState savedSkill in playerGrowth.skills)
        {
            if (savedSkill == null)
                continue;

            SkillData sourceSkill = skillDatabase.GetByID(savedSkill.skillID);
            if (sourceSkill == null)
                sourceSkill = skillDatabase.GetByNameKeyFallback(savedSkill.skillID);

            if (sourceSkill == null)
                continue;

            SkillData runtimeSkill = Object.Instantiate(sourceSkill);
            runtimeSkill.skillLevel = Mathf.Max(1, savedSkill.skillLevel);
            runtimeSkill.currentEvolution = savedSkill.currentEvolution;
            playerManager.unlockedSkills.Add(runtimeSkill);
        }
    }

    private static void ApplySupporters(PlayerManager playerManager, PlayerGrowthSaveData playerGrowth)
    {
        playerManager.unlockedSupporters.Clear();
        playerManager.supporterChoiceRecords.Clear();
        playerManager.activeSupporter = null;

        SupporterDatabase supporterDatabase = SaveManager.Instance != null ? SaveManager.Instance.supporterDatabase : null;
        if (supporterDatabase == null || supporterDatabase.allSupporters == null || playerGrowth.supporters == null)
            return;

        foreach (SavedSupporterState savedSupporter in playerGrowth.supporters)
        {
            if (savedSupporter == null || string.IsNullOrEmpty(savedSupporter.supporterID))
                continue;

            SupporterChoiceState choiceState = NormalizeSupporterChoiceState(savedSupporter);
            playerManager.supporterChoiceRecords.Add(new SupporterChoiceRecord
            {
                supporterID = savedSupporter.supporterID,
                state = choiceState
            });

            if (!savedSupporter.unlocked || choiceState != SupporterChoiceState.Recruited)
                continue;

            SupporterData sourceSupporter = supporterDatabase.GetByID(savedSupporter.supporterID);
            if (sourceSupporter == null)
                continue;

            SupporterData runtimeSupporter = Object.Instantiate(sourceSupporter);
            runtimeSupporter.passiveLevel = Mathf.Clamp(savedSupporter.passiveLevel, 1, 3);
            runtimeSupporter.startSkillLevel = Mathf.Clamp(savedSupporter.startSkillLevel, 1, 3);
            runtimeSupporter.battleSkillLevel = Mathf.Clamp(savedSupporter.battleSkillLevel, 1, 3);
            playerManager.unlockedSupporters.Add(runtimeSupporter);

            if (savedSupporter.active)
                playerManager.activeSupporter = runtimeSupporter;
        }
    }

    private static void ApplyKarinItems(PlayerManager playerManager, PlayerGrowthSaveData playerGrowth)
    {
        playerManager.ownedKarinItems.Clear();
        playerManager.equippedKarinItem = null;

        KarinItemDatabase karinItemDatabase = SaveManager.Instance != null ? SaveManager.Instance.karinItemDatabase : null;
        if (karinItemDatabase == null || karinItemDatabase.allItems == null || playerGrowth.ownedKarinItemIDs == null)
            return;

        foreach (string itemID in playerGrowth.ownedKarinItemIDs)
        {
            if (string.IsNullOrEmpty(itemID))
                continue;

            KarinItemData item = karinItemDatabase.GetByID(itemID);
            if (item != null && !playerManager.ownedKarinItems.Contains(item))
                playerManager.ownedKarinItems.Add(item);
        }

        if (!string.IsNullOrEmpty(playerGrowth.equippedKarinItemID))
            playerManager.equippedKarinItem = karinItemDatabase.GetByID(playerGrowth.equippedKarinItemID);
    }

    private static void ApplyHiddenBossClears(PlayerManager playerManager, PlayerGrowthSaveData playerGrowth)
    {
        playerManager.clearedHiddenBossIDs.Clear();
        if (playerGrowth.clearedHiddenBossIDs == null)
            return;

        foreach (string hiddenBossID in playerGrowth.clearedHiddenBossIDs)
        {
            if (!string.IsNullOrEmpty(hiddenBossID) && !playerManager.clearedHiddenBossIDs.Contains(hiddenBossID))
                playerManager.clearedHiddenBossIDs.Add(hiddenBossID);
        }
    }

    private static EquipmentItemData FindItem(ItemDatabase itemDatabase, string itemID)
    {
        foreach (EquipmentItemData item in itemDatabase.allItems)
        {
            if (item != null && item.itemID == itemID)
                return item;
        }

        return null;
    }

    private static SupporterChoiceState NormalizeSupporterChoiceState(SavedSupporterState savedSupporter)
    {
        if (savedSupporter.choiceState != SupporterChoiceState.Undecided)
            return savedSupporter.choiceState;

        return savedSupporter.unlocked ? SupporterChoiceState.Recruited : SupporterChoiceState.Undecided;
    }
}
