using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance;

    [Header("Databases")]
    public ItemDatabase itemDatabase;
    public SkillDatabase skillDatabase;
    public KarinItemDatabase karinItemDatabase;
    public BossDatabase bossDatabase;
    public SupporterDatabase supporterDatabase;
    public ExplorationNodeDatabase explorationNodeDatabase;

    private bool isLoading;
    private bool suppressAutoSave;

    private string ContinueSavePath => Path.Combine(Application.persistentDataPath, "continue_save.json");
    private string TempSavePath => Path.Combine(Application.persistentDataPath, "continue_save.json.tmp");
    private string BackupSavePath => Path.Combine(Application.persistentDataPath, "continue_save.json.bak");

    public bool IsAutoSaveSuppressed => isLoading || suppressAutoSave;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(this);
        }
    }

    public void AutoSaveContinue()
    {
        if (IsAutoSaveSuppressed)
            return;

        if (PlayerManager.Instance == null || ExplorationManager.Instance == null)
            return;

        if (CombatManager.Instance != null || SceneManager.GetActiveScene().name == "Battle")
            return;

        suppressAutoSave = true;
        try
        {
            List<SavedExplorationOption> savedOptions = ExplorationManager.Instance.GetCurrentOptionsForSave();
            if (savedOptions == null || savedOptions.Count == 0)
                return;

            ContinueSaveData data = new ContinueSaveData
            {
                version = 1,
                savedAt = DateTime.UtcNow.ToString("o"),
                player = BuildPlayerGrowthSaveData(PlayerManager.Instance),
                exploration = BuildExplorationContinueSaveData(ExplorationManager.Instance, savedOptions)
            };

            string json = JsonUtility.ToJson(data, true);
            WriteContinueSaveSafely(json);
            DevLog.Log($"[Save] Continue auto save complete: {ContinueSavePath}");
        }
        catch (Exception ex)
        {
            DevLog.LogWarning($"[Save] Continue auto save failed: {ex.Message}");
        }
        finally
        {
            suppressAutoSave = false;
        }
    }

    public bool HasContinueSave()
    {
        return File.Exists(ContinueSavePath) || File.Exists(BackupSavePath);
    }

    public bool TryLoadContinueSave()
    {
        if (!HasContinueSave())
            return false;

        isLoading = true;
        suppressAutoSave = true;

        try
        {
            if (TryLoadFromPath(ContinueSavePath, out ContinueSaveData data) && ApplyContinueSaveData(data))
            {
                DevLog.Log($"[Save] Continue save loaded: {ContinueSavePath}");
                return true;
            }

            DevLog.LogWarning("[Save] Primary continue save failed. Trying backup.");

            if (TryLoadFromPath(BackupSavePath, out data) && ApplyContinueSaveData(data))
            {
                DevLog.LogWarning($"[Save] Backup continue save loaded: {BackupSavePath}");
                return true;
            }

            return false;
        }
        finally
        {
            suppressAutoSave = false;
            isLoading = false;
        }
    }

    public void DeleteContinueSave()
    {
        DeleteFileIfExists(ContinueSavePath);
        DeleteFileIfExists(TempSavePath);
        DeleteFileIfExists(BackupSavePath);
        DevLog.Log("[Save] Continue save deleted.");
    }

    private void WriteContinueSaveSafely(string json)
    {
        Directory.CreateDirectory(Application.persistentDataPath);

        if (File.Exists(TempSavePath))
            File.Delete(TempSavePath);

        File.WriteAllText(TempSavePath, json);

        if (File.Exists(BackupSavePath))
            File.Delete(BackupSavePath);

        if (File.Exists(ContinueSavePath))
            File.Copy(ContinueSavePath, BackupSavePath);

        if (File.Exists(ContinueSavePath))
            File.Delete(ContinueSavePath);

        File.Move(TempSavePath, ContinueSavePath);

        if (File.Exists(TempSavePath))
            File.Delete(TempSavePath);
    }

    private bool TryLoadFromPath(string path, out ContinueSaveData data)
    {
        data = null;

        if (!File.Exists(path))
            return false;

        try
        {
            string json = File.ReadAllText(path);
            data = JsonUtility.FromJson<ContinueSaveData>(json);

            if (data == null || data.player == null)
            {
                DevLog.LogWarning($"[Save] Invalid continue save data: {path}");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            DevLog.LogWarning($"[Save] Failed to read continue save '{path}': {ex.Message}");
            return false;
        }
    }

    private ContinueSaveData BuildContinueSaveData()
    {
        return new ContinueSaveData
        {
            version = 1,
            savedAt = DateTime.UtcNow.ToString("o"),
            player = BuildPlayerGrowthSaveData(PlayerManager.Instance),
            exploration = BuildExplorationContinueSaveData(
                ExplorationManager.Instance,
                ExplorationManager.Instance.GetCurrentOptionsForSave())
        };
    }

    private PlayerGrowthSaveData BuildPlayerGrowthSaveData(PlayerManager playerManager)
    {
        PlayerStats stats = playerManager.stats;
        PlayerGrowthSaveData data = new PlayerGrowthSaveData
        {
            playerName = "Sherry",
            level = stats.level,
            maxExp = stats.maxExp,
            currentExp = stats.currentExp,
            maxHp = stats.maxHp,
            currentHp = stats.currentHp,
            actionPoints = stats.ActionPoints,
            breakResistance = stats.breakResistance,
            maxBreakGauge = stats.maxBreakGauge,
            strength = stats.strength,
            defense = stats.defense,
            speed = stats.speed,
            luck = stats.luck,
            currentGold = stats.currentGold,
            rejectedSupporterCount = stats.rejectedSupporterCount
        };

        foreach (OwnedItem item in playerManager.inventory)
        {
            if (item == null || item.data == null)
                continue;

            if (string.IsNullOrEmpty(item.data.itemID))
                DevLog.LogWarning($"[Save] Equipment itemID is empty: {item.data.name}");

            data.inventory.Add(new SavedOwnedItem
            {
                itemID = item.data.itemID,
                starLevel = item.starLevel
            });
        }

        foreach (SkillData skill in playerManager.unlockedSkills)
        {
            if (skill == null)
                continue;

            string skillID = skill.skillID;
            if (string.IsNullOrEmpty(skillID))
            {
                skillID = skill.skillNameKey;
                DevLog.LogWarning($"[Save] skillID is empty. Using skillNameKey fallback: {skillID}");
            }

            data.skills.Add(new SavedSkillState
            {
                skillID = skillID,
                skillLevel = skill.skillLevel,
                currentEvolution = skill.currentEvolution,
                unlocked = true
            });
        }

        foreach (SupporterData supporter in playerManager.unlockedSupporters)
        {
            if (supporter == null)
                continue;

            data.supporters.Add(new SavedSupporterState
            {
                supporterID = supporter.supporterID,
                unlocked = true,
                active = supporter == playerManager.activeSupporter,
                passiveLevel = supporter.passiveLevel,
                startSkillLevel = supporter.startSkillLevel,
                battleSkillLevel = supporter.battleSkillLevel
            });
        }

        foreach (KarinItemData item in playerManager.ownedKarinItems)
        {
            if (item == null)
                continue;

            data.ownedKarinItemIDs.Add(item.itemID);
        }

        data.equippedKarinItemID = playerManager.equippedKarinItem != null
            ? playerManager.equippedKarinItem.itemID
            : null;

        return data;
    }

    private ExplorationContinueSaveData BuildExplorationContinueSaveData(
        ExplorationManager explorationManager,
        List<SavedExplorationOption> savedOptions)
    {
        ExplorationContinueSaveData data = new ExplorationContinueSaveData
        {
            currentPhase = explorationManager.currentPhase,
            currentCycle = explorationManager.currentCycle,
            currentTurnInPhase = explorationManager.currentTurnInPhase,
            currentKeys = explorationManager.currentKeys,
            currentTargetBossID = explorationManager.GetCurrentTargetBossIDForSave(),
            remainingMidBossIDs = explorationManager.GetRemainingMidBossIDsForSave(),
            lastVisitedFacilityID = explorationManager.lastVisitedFacility != null ? explorationManager.lastVisitedFacility.nodeID : null,
            lastVisitedNodeID = explorationManager.lastVisitedFacility != null ? explorationManager.lastVisitedFacility.nodeID : null,
            currentOptions = savedOptions
        };

        foreach (KeyValuePair<string, int> rank in explorationManager.facilityRanks)
        {
            data.facilityRanks.Add(new SavedFacilityRank
            {
                facilityID = rank.Key,
                rank = rank.Value
            });
        }

        return data;
    }

    private bool ApplyContinueSaveData(ContinueSaveData data)
    {
        if (PlayerManager.Instance == null)
        {
            DevLog.LogWarning("[Save] Load failed: PlayerManager missing.");
            return false;
        }

        ApplyPlayerGrowthSaveData(PlayerManager.Instance, data.player);

        if (ExplorationManager.Instance != null && data.exploration != null)
        {
            if (!ApplyExplorationContinueSaveData(ExplorationManager.Instance, data.exploration))
                return false;

            RefreshExplorationUIIfPresent();
        }

        return true;
    }

    private void ApplyPlayerGrowthSaveData(PlayerManager playerManager, PlayerGrowthSaveData data)
    {
        PlayerStats stats = playerManager.stats;
        stats.level = Mathf.Max(1, data.level);
        stats.maxExp = Mathf.Max(1, data.maxExp);
        stats.currentExp = Mathf.Max(0, data.currentExp);
        stats.maxHp = Mathf.Max(1, data.maxHp);
        stats.currentHp = Mathf.Clamp(data.currentHp, 0, stats.maxHp);
        stats.ActionPoints = Mathf.Max(0, data.actionPoints);
        stats.breakResistance = Mathf.Max(0, data.breakResistance);
        stats.maxBreakGauge = Mathf.Max(0f, data.maxBreakGauge);
        stats.strength = Mathf.Max(0, data.strength);
        stats.defense = Mathf.Max(0, data.defense);
        stats.speed = Mathf.Max(0, data.speed);
        stats.luck = Mathf.Max(0, data.luck);
        stats.currentGold = Mathf.Max(0, data.currentGold);
        stats.rejectedSupporterCount = Mathf.Max(0, data.rejectedSupporterCount);

        playerManager.inventory.Clear();
        foreach (SavedOwnedItem savedItem in data.inventory ?? new List<SavedOwnedItem>())
        {
            EquipmentItemData item = FindEquipmentItem(savedItem.itemID);
            if (item == null)
            {
                DevLog.LogWarning($"[Save] Equipment item not found: {savedItem.itemID}");
                continue;
            }

            playerManager.inventory.Add(new OwnedItem(item, Mathf.Clamp(savedItem.starLevel, 1, 3)));
        }

        playerManager.unlockedSkills.Clear();
        foreach (SavedSkillState savedSkill in data.skills ?? new List<SavedSkillState>())
        {
            SkillData original = FindSkill(savedSkill.skillID);
            if (original == null)
            {
                DevLog.LogWarning($"[Save] Skill not found: {savedSkill.skillID}");
                continue;
            }

            SkillData runtimeSkill = Instantiate(original);
            runtimeSkill.skillLevel = Mathf.Max(1, savedSkill.skillLevel);
            runtimeSkill.currentEvolution = savedSkill.currentEvolution;
            playerManager.unlockedSkills.Add(runtimeSkill);
        }

        playerManager.unlockedSupporters.Clear();
        playerManager.activeSupporter = null;
        foreach (SavedSupporterState savedSupporter in data.supporters ?? new List<SavedSupporterState>())
        {
            SupporterData supporter = supporterDatabase != null ? supporterDatabase.GetByID(savedSupporter.supporterID) : null;
            if (supporter == null)
            {
                DevLog.LogWarning($"[Save] Supporter not found: {savedSupporter.supporterID}");
                continue;
            }

            SupporterData runtimeSupporter = Instantiate(supporter);
            runtimeSupporter.passiveLevel = Mathf.Clamp(savedSupporter.passiveLevel, 1, 3);
            runtimeSupporter.startSkillLevel = Mathf.Clamp(savedSupporter.startSkillLevel, 1, 3);
            runtimeSupporter.battleSkillLevel = Mathf.Clamp(savedSupporter.battleSkillLevel, 1, 3);

            playerManager.unlockedSupporters.Add(runtimeSupporter);
            if (savedSupporter.active)
                playerManager.activeSupporter = runtimeSupporter;
        }

        playerManager.ownedKarinItems.Clear();
        foreach (string itemID in data.ownedKarinItemIDs ?? new List<string>())
        {
            KarinItemData item = karinItemDatabase != null ? karinItemDatabase.GetByID(itemID) : null;
            if (item == null)
            {
                DevLog.LogWarning($"[Save] Karin item not found: {itemID}");
                continue;
            }

            playerManager.ownedKarinItems.Add(item);
        }

        playerManager.equippedKarinItem = null;
        if (!string.IsNullOrEmpty(data.equippedKarinItemID) && karinItemDatabase != null)
        {
            playerManager.equippedKarinItem = karinItemDatabase.GetByID(data.equippedKarinItemID);
            if (playerManager.equippedKarinItem == null)
                DevLog.LogWarning($"[Save] Equipped Karin item not found: {data.equippedKarinItemID}");
        }
    }

    private bool ApplyExplorationContinueSaveData(ExplorationManager explorationManager, ExplorationContinueSaveData data)
    {
        explorationManager.currentPhase = data.currentPhase;
        explorationManager.currentCycle = Mathf.Max(1, data.currentCycle);
        explorationManager.currentTurnInPhase = Mathf.Max(0, data.currentTurnInPhase);
        explorationManager.currentKeys = Mathf.Max(0, data.currentKeys);

        bool restored = true;
        if (!explorationManager.RestoreBossProgressFromSave(data.currentTargetBossID, data.remainingMidBossIDs, bossDatabase))
        {
            DevLog.LogWarning("[Save] Boss progress restore failed.");
            restored = false;
        }

        explorationManager.facilityRanks.Clear();
        foreach (SavedFacilityRank rank in data.facilityRanks ?? new List<SavedFacilityRank>())
        {
            if (!string.IsNullOrEmpty(rank.facilityID))
                explorationManager.facilityRanks[rank.facilityID] = Mathf.Max(0, rank.rank);
        }

        ExplorationNodeData lastFacilityNode = !string.IsNullOrEmpty(data.lastVisitedFacilityID) && explorationNodeDatabase != null
            ? explorationNodeDatabase.GetByID(data.lastVisitedFacilityID)
            : null;
        explorationManager.lastVisitedFacility = lastFacilityNode as FacilityData;

        ExplorationNodeData lastNode = !string.IsNullOrEmpty(data.lastVisitedNodeID) && explorationNodeDatabase != null
            ? explorationNodeDatabase.GetByID(data.lastVisitedNodeID)
            : lastFacilityNode;
        explorationManager.lastVisitedNodeImage = lastNode != null ? lastNode.nodeImage : null;

        if (!explorationManager.RestoreCurrentOptionsFromSave(data.currentOptions, bossDatabase, explorationNodeDatabase))
        {
            DevLog.LogWarning("[Save] Current options restore failed.");
            restored = false;
        }

        return restored;
    }

    private EquipmentItemData FindEquipmentItem(string itemID)
    {
        if (itemDatabase == null || itemDatabase.allItems == null)
            return null;

        foreach (EquipmentItemData item in itemDatabase.allItems)
        {
            if (item != null && item.itemID == itemID)
                return item;
        }

        return null;
    }

    private SkillData FindSkill(string skillID)
    {
        if (skillDatabase == null)
            return null;

        SkillData skill = skillDatabase.GetByID(skillID);
        return skill != null ? skill : skillDatabase.GetByNameKeyFallback(skillID);
    }

    private void RefreshExplorationUIIfPresent()
    {
        ExplorationUI ui = FindFirstObjectByType<ExplorationUI>();
        if (ui != null)
            ui.RefreshAfterContinueLoad();
    }

    private void DeleteFileIfExists(string path)
    {
        if (File.Exists(path))
            File.Delete(path);
    }
}
