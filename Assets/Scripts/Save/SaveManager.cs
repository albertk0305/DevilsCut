using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance;

    private const string StorySceneName = "Story";
    private const string ExplorationSceneName = "Exploration";

    [Header("Databases")]
    public ItemDatabase itemDatabase;
    public SkillDatabase skillDatabase;
    public KarinItemDatabase karinItemDatabase;
    public BossDatabase bossDatabase;
    public SupporterDatabase supporterDatabase;
    public ExplorationNodeDatabase explorationNodeDatabase;

    private bool isLoading;
    private bool suppressAutoSave;
    private bool pendingContinueLoad;
    private const int MaxClearRecordCount = 20;

    private string ContinueSavePath => Path.Combine(Application.persistentDataPath, "continue_save.json");
    private string TempSavePath => Path.Combine(Application.persistentDataPath, "continue_save.json.tmp");
    private string BackupSavePath => Path.Combine(Application.persistentDataPath, "continue_save.json.bak");
    private string ClearRecordsPath => Path.Combine(Application.persistentDataPath, "clear_records.json");
    private string TempClearRecordsPath => Path.Combine(Application.persistentDataPath, "clear_records.json.tmp");
    private string BackupClearRecordsPath => Path.Combine(Application.persistentDataPath, "clear_records.json.bak");

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
                sceneName = ExplorationSceneName,
                dialogueID = "",
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

    public bool SaveContinueData()
    {
        if (PlayerManager.Instance == null)
        {
            DevLog.LogWarning("[Save] Continue save failed: PlayerManager missing.");
            return false;
        }

        suppressAutoSave = true;
        try
        {
            ContinueSaveData data = BuildContinueSaveData();
            if (data == null || data.player == null || data.exploration == null)
            {
                DevLog.LogWarning("[Save] Continue save failed: save data is incomplete.");
                return false;
            }

            data.sceneName = ExplorationSceneName;
            data.dialogueID = "";
            string json = JsonUtility.ToJson(data, true);
            WriteContinueSaveSafely(json);
            DevLog.Log($"[Save] Continue save complete: {ContinueSavePath}");
            return true;
        }
        catch (Exception ex)
        {
            DevLog.LogWarning($"[Save] Continue save failed: {ex.Message}");
            return false;
        }
        finally
        {
            suppressAutoSave = false;
        }
    }

    public bool SaveContinueDataForDialogue(string sceneName, string dialogueID)
    {
        if (PlayerManager.Instance == null)
        {
            DevLog.LogWarning("[Save] Dialogue continue save failed: PlayerManager missing.");
            return false;
        }

        suppressAutoSave = true;
        try
        {
            ContinueSaveData data = BuildContinueSaveData();
            if (data == null || data.player == null || data.exploration == null)
            {
                DevLog.LogWarning("[Save] Dialogue continue save failed: save data is incomplete.");
                return false;
            }

            data.sceneName = string.IsNullOrEmpty(sceneName) ? StorySceneName : sceneName;
            data.dialogueID = dialogueID;

            string json = JsonUtility.ToJson(data, true);
            WriteContinueSaveSafely(json);
            DevLog.Log($"[Save] Dialogue continue save complete: sceneName={data.sceneName}, dialogueID={data.dialogueID}");
            return true;
        }
        catch (Exception ex)
        {
            DevLog.LogWarning($"[Save] Dialogue continue save failed: {ex.Message}");
            return false;
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

    public bool RequestLoadContinueOnNextExplorationStart()
    {
        if (!HasContinueSave())
        {
            pendingContinueLoad = false;
            DevLog.LogWarning("[Save] Continue load request failed: save file not found.");
            return false;
        }

        pendingContinueLoad = true;
        return true;
    }

    public bool ConsumePendingContinueLoadRequest()
    {
        if (!pendingContinueLoad)
            return false;

        pendingContinueLoad = false;
        return true;
    }

    public void CancelPendingContinueLoadRequest()
    {
        pendingContinueLoad = false;
    }

    public bool TryPrepareContinueLoad(out string sceneName)
    {
        sceneName = "";

        if (!HasContinueSave())
        {
            pendingContinueLoad = false;
            DevLog.LogWarning("[Save] Continue load request failed: save file not found.");
            return false;
        }

        if (!TryLoadContinueStartData(out ContinueSaveData data))
            return false;

        sceneName = string.IsNullOrEmpty(data.sceneName) ? ExplorationSceneName : data.sceneName;
        if (sceneName != ExplorationSceneName)
        {
            pendingContinueLoad = false;
            if (!ApplyContinueSaveData(data))
                return false;

            if (!string.IsNullOrEmpty(data.dialogueID))
                DialogueRuntimeContext.SetPendingDialogueID(data.dialogueID);

            DevLog.Log($"[Save] Continue dialogue load prepared: sceneName={sceneName}, dialogueID={data.dialogueID}");
            return true;
        }

        sceneName = ExplorationSceneName;
        return RequestLoadContinueOnNextExplorationStart();
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

    private bool TryLoadContinueStartData(out ContinueSaveData data)
    {
        if (TryLoadFromPath(ContinueSavePath, out data))
            return true;

        if (File.Exists(ContinueSavePath))
            DevLog.LogWarning("[Save] Primary continue save failed. Trying backup.");

        return TryLoadFromPath(BackupSavePath, out data);
    }

    public void DeleteContinueSave()
    {
        DeleteFileIfExists(ContinueSavePath);
        DeleteFileIfExists(TempSavePath);
        DeleteFileIfExists(BackupSavePath);
        DevLog.Log("[Save] Continue save deleted.");
    }

    public void DeleteCurrentSave()
    {
        DeleteContinueSave();
    }

    public void HandleGameClear(string endingID)
    {
        // Future extension point:
        // SaveClearRecord(endingID);
        // UnlockGallery(endingID);
        // UnlockNewGamePlus();

        DeleteCurrentSave();
        DevLog.Log($"[Save] Game clear handled. endingID={endingID}");
    }

    public List<ClearRecordSaveData> LoadClearRecords()
    {
        if (TryLoadClearRecordCollectionFromPath(ClearRecordsPath, out ClearRecordCollectionSaveData collection))
            return collection.records ?? new List<ClearRecordSaveData>();

        if (File.Exists(ClearRecordsPath))
            DevLog.LogWarning("[Save] Primary clear records failed. Trying backup.");

        if (TryLoadClearRecordCollectionFromPath(BackupClearRecordsPath, out collection))
        {
            DevLog.LogWarning($"[Save] Backup clear records loaded: {BackupClearRecordsPath}");
            return collection.records ?? new List<ClearRecordSaveData>();
        }

        return new List<ClearRecordSaveData>();
    }

    public bool SaveClearRecords(List<ClearRecordSaveData> records)
    {
        List<ClearRecordSaveData> normalizedRecords = records ?? new List<ClearRecordSaveData>();
        TrimClearRecords(normalizedRecords);

        ClearRecordCollectionSaveData collection = new ClearRecordCollectionSaveData
        {
            version = 1,
            records = normalizedRecords
        };

        try
        {
            string json = JsonUtility.ToJson(collection, true);
            WriteFileSafely(json, ClearRecordsPath, TempClearRecordsPath, BackupClearRecordsPath);
            DevLog.Log($"[Save] Clear records saved: {ClearRecordsPath}");
            return true;
        }
        catch (Exception ex)
        {
            DevLog.LogWarning($"[Save] Clear records save failed: {ex.Message}");
            return false;
        }
    }

    public bool AddClearRecord(string resultType)
    {
        if (PlayerManager.Instance == null)
        {
            DevLog.LogWarning("[Save] Clear record add failed: PlayerManager missing.");
            return false;
        }

        PlayerGrowthSaveData playerData = BuildPlayerGrowthSaveData(PlayerManager.Instance);
        int finalCycle = 0;
        int clearTurnOrScore = 0;

        if (ExplorationManager.Instance != null)
        {
            finalCycle = ExplorationManager.Instance.currentCycle;
            clearTurnOrScore = ExplorationManager.Instance.currentKeys;
        }
        else if (PlayerManager.Instance.hasSavedExplorationState)
        {
            finalCycle = PlayerManager.Instance.savedExplorationCycle;
        }

        ClearRecordSaveData record = new ClearRecordSaveData
        {
            version = 1,
            recordID = Guid.NewGuid().ToString("N"),
            savedAt = DateTime.UtcNow.ToString("o"),
            playerName = playerData.playerName,
            resultType = resultType,
            finalCycle = finalCycle,
            finalLevel = playerData.level,
            finalGold = playerData.currentGold,
            rejectedSupporterCount = playerData.rejectedSupporterCount,
            player = playerData,
            reachedCycle = finalCycle,
            defeatedBossCount = Mathf.Max(0, finalCycle - 1),
            clearTurnOrScore = clearTurnOrScore
        };

        List<ClearRecordSaveData> records = LoadClearRecords();
        records.Insert(0, record);
        TrimClearRecords(records);

        return SaveClearRecords(records);
    }

    public bool DeleteClearRecord(string recordID)
    {
        if (string.IsNullOrEmpty(recordID))
            return false;

        List<ClearRecordSaveData> records = LoadClearRecords();
        int removedCount = records.RemoveAll(record => record != null && record.recordID == recordID);

        if (removedCount <= 0)
            return false;

        return SaveClearRecords(records);
    }

    public void DeleteAllClearRecords()
    {
        DeleteFileIfExists(ClearRecordsPath);
        DeleteFileIfExists(TempClearRecordsPath);
        DeleteFileIfExists(BackupClearRecordsPath);
        DevLog.Log("[Save] Clear records deleted.");
    }

    private void WriteContinueSaveSafely(string json)
    {
        WriteFileSafely(json, ContinueSavePath, TempSavePath, BackupSavePath);
    }

    private void WriteFileSafely(string json, string savePath, string tempPath, string backupPath)
    {
        Directory.CreateDirectory(Application.persistentDataPath);

        if (File.Exists(tempPath))
            File.Delete(tempPath);

        File.WriteAllText(tempPath, json);

        if (File.Exists(backupPath))
            File.Delete(backupPath);

        if (File.Exists(savePath))
            File.Copy(savePath, backupPath);

        if (File.Exists(savePath))
            File.Delete(savePath);

        File.Move(tempPath, savePath);

        if (File.Exists(tempPath))
            File.Delete(tempPath);
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

    private bool TryLoadClearRecordCollectionFromPath(string path, out ClearRecordCollectionSaveData collection)
    {
        collection = null;

        if (!File.Exists(path))
            return false;

        try
        {
            string json = File.ReadAllText(path);
            collection = JsonUtility.FromJson<ClearRecordCollectionSaveData>(json);

            if (collection == null)
            {
                DevLog.LogWarning($"[Save] Invalid clear records data: {path}");
                return false;
            }

            if (collection.records == null)
                collection.records = new List<ClearRecordSaveData>();

            return true;
        }
        catch (Exception ex)
        {
            DevLog.LogWarning($"[Save] Failed to read clear records '{path}': {ex.Message}");
            return false;
        }
    }

    private void TrimClearRecords(List<ClearRecordSaveData> records)
    {
        if (records == null)
            return;

        records.RemoveAll(record => record == null);

        if (records.Count > MaxClearRecordCount)
            records.RemoveRange(MaxClearRecordCount, records.Count - MaxClearRecordCount);
    }

    private ContinueSaveData BuildContinueSaveData()
    {
        return new ContinueSaveData
        {
            version = 1,
            savedAt = DateTime.UtcNow.ToString("o"),
            sceneName = SceneManager.GetActiveScene().name,
            dialogueID = "",
            player = BuildPlayerGrowthSaveData(PlayerManager.Instance),
            exploration = BuildCurrentExplorationContinueSaveData()
        };
    }

    private ExplorationContinueSaveData BuildCurrentExplorationContinueSaveData()
    {
        if (ExplorationManager.Instance != null)
        {
            return BuildExplorationContinueSaveData(
                ExplorationManager.Instance,
                ExplorationManager.Instance.GetCurrentOptionsForSave());
        }

        return BuildInitialExplorationContinueSaveData(PlayerManager.Instance);
    }

    private ExplorationContinueSaveData BuildInitialExplorationContinueSaveData(PlayerManager playerManager)
    {
        List<string> remainingMidBossIDs = BuildRemainingMidBossIDsFromPlayerManager(playerManager);

        if (remainingMidBossIDs.Count == 0 && (playerManager == null || !playerManager.hasSavedExplorationState))
            remainingMidBossIDs = BuildInitialRemainingMidBossIDs();

        ExplorationContinueSaveData data = new ExplorationContinueSaveData
        {
            currentPhase = playerManager != null && playerManager.hasSavedExplorationState
                ? playerManager.savedExplorationPhase
                : GamePhase.BossSelection,
            currentCycle = playerManager != null && playerManager.hasSavedExplorationState
                ? Mathf.Max(1, playerManager.savedExplorationCycle)
                : 1,
            currentTurnInPhase = playerManager != null && playerManager.hasSavedExplorationState
                ? Mathf.Max(0, playerManager.savedExplorationTurnInPhase)
                : 0,
            currentKeys = playerManager != null && playerManager.hasSavedExplorationState
                ? Mathf.Max(0, playerManager.savedExplorationKeys)
                : 0,
            currentTargetBossID = playerManager != null
                && playerManager.hasSavedExplorationState
                && playerManager.savedCurrentTargetBoss != null
                ? playerManager.savedCurrentTargetBoss.bossID
                : null,
            remainingMidBossIDs = remainingMidBossIDs,
            lastVisitedFacilityID = playerManager != null
                && playerManager.hasSavedExplorationState
                && playerManager.savedLastVisitedFacility != null
                ? playerManager.savedLastVisitedFacility.nodeID
                : null,
            lastVisitedNodeID = playerManager != null
                && playerManager.hasSavedExplorationState
                && playerManager.savedLastVisitedFacility != null
                ? playerManager.savedLastVisitedFacility.nodeID
                : null,
            currentOptions = BuildInitialBossSelectionOptions(remainingMidBossIDs)
        };

        if (playerManager != null && playerManager.savedFacilityRanks != null)
        {
            foreach (PlayerFacilityRankRecord rank in playerManager.savedFacilityRanks)
            {
                if (rank == null || string.IsNullOrEmpty(rank.facilityID))
                    continue;

                data.facilityRanks.Add(new SavedFacilityRank
                {
                    facilityID = rank.facilityID,
                    rank = Mathf.Clamp(rank.rank, 0, 3)
                });
            }
        }

        return data;
    }

    private List<string> BuildRemainingMidBossIDsFromPlayerManager(PlayerManager playerManager)
    {
        List<string> bossIDs = new List<string>();

        if (playerManager == null ||
            !playerManager.hasSavedExplorationState ||
            !playerManager.hasSavedRemainingMidBosses ||
            playerManager.savedRemainingMidBosses == null)
        {
            return bossIDs;
        }

        foreach (BossEncounterData boss in playerManager.savedRemainingMidBosses)
        {
            if (boss != null && !string.IsNullOrEmpty(boss.bossID))
                bossIDs.Add(boss.bossID);
        }

        return bossIDs;
    }

    private List<string> BuildInitialRemainingMidBossIDs()
    {
        List<string> bossIDs = new List<string>();
        if (bossDatabase == null || bossDatabase.allBosses == null)
            return bossIDs;

        List<BossEncounterData> storyBosses = new List<BossEncounterData>();
        foreach (BossEncounterData boss in bossDatabase.allBosses)
        {
            if (boss != null && boss.bossID != HiddenBossConstants.BaitoHiddenBossID)
                storyBosses.Add(boss);
        }

        int midBossCount = storyBosses.Count > 7
            ? Mathf.Max(0, storyBosses.Count - 2)
            : storyBosses.Count;
        midBossCount = Mathf.Min(7, midBossCount);

        for (int i = 0; i < midBossCount; i++)
        {
            BossEncounterData boss = storyBosses[i];
            if (boss != null && !string.IsNullOrEmpty(boss.bossID))
                bossIDs.Add(boss.bossID);
        }

        return bossIDs;
    }

    private List<SavedExplorationOption> BuildInitialBossSelectionOptions(List<string> remainingMidBossIDs)
    {
        List<SavedExplorationOption> options = new List<SavedExplorationOption>();
        for (int i = 0; i < 3; i++)
        {
            SavedExplorationOption option = new SavedExplorationOption
            {
                slotIndex = i,
                optionType = "None"
            };

            if (remainingMidBossIDs != null && i < remainingMidBossIDs.Count)
            {
                option.optionType = "BossSelection";
                option.bossID = remainingMidBossIDs[i];
            }

            options.Add(option);
        }

        return options;
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

        Dictionary<string, SavedSupporterState> savedSupportersById = new Dictionary<string, SavedSupporterState>();

        foreach (SupporterData supporter in playerManager.unlockedSupporters)
        {
            if (supporter == null)
                continue;

            SavedSupporterState savedSupporter = new SavedSupporterState
            {
                supporterID = supporter.supporterID,
                unlocked = true,
                active = supporter == playerManager.activeSupporter,
                choiceState = SupporterChoiceState.Recruited,
                passiveLevel = supporter.passiveLevel,
                startSkillLevel = supporter.startSkillLevel,
                battleSkillLevel = supporter.battleSkillLevel
            };

            data.supporters.Add(savedSupporter);
            if (!string.IsNullOrEmpty(savedSupporter.supporterID))
                savedSupportersById[savedSupporter.supporterID] = savedSupporter;
        }

        foreach (SupporterChoiceRecord record in playerManager.supporterChoiceRecords)
        {
            if (record == null || string.IsNullOrEmpty(record.supporterID))
                continue;

            if (record.state == SupporterChoiceState.Undecided)
                continue;

            if (savedSupportersById.TryGetValue(record.supporterID, out SavedSupporterState savedSupporter))
            {
                if (savedSupporter.unlocked)
                    savedSupporter.choiceState = SupporterChoiceState.Recruited;
                else
                    savedSupporter.choiceState = record.state;

                continue;
            }

            data.supporters.Add(new SavedSupporterState
            {
                supporterID = record.supporterID,
                unlocked = false,
                active = false,
                choiceState = record.state,
                passiveLevel = 1,
                startSkillLevel = 1,
                battleSkillLevel = 1
            });
        }

        foreach (string hiddenBossID in playerManager.clearedHiddenBossIDs ?? new List<string>())
        {
            if (!string.IsNullOrEmpty(hiddenBossID) && !data.clearedHiddenBossIDs.Contains(hiddenBossID))
                data.clearedHiddenBossIDs.Add(hiddenBossID);
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
        playerManager.supporterChoiceRecords.Clear();
        playerManager.activeSupporter = null;
        playerManager.clearedHiddenBossIDs.Clear();
        foreach (SavedSupporterState savedSupporter in data.supporters ?? new List<SavedSupporterState>())
        {
            SupporterData supporter = supporterDatabase != null ? supporterDatabase.GetByID(savedSupporter.supporterID) : null;
            if (supporter == null)
            {
                DevLog.LogWarning($"[Save] Supporter not found: {savedSupporter.supporterID}");
                continue;
            }

            SupporterChoiceState choiceState = NormalizeSupporterChoiceState(savedSupporter);
            playerManager.SetSupporterChoiceState(savedSupporter.supporterID, choiceState);

            if (choiceState == SupporterChoiceState.Rejected)
                continue;

            if (choiceState != SupporterChoiceState.Recruited)
                continue;

            SupporterData runtimeSupporter = Instantiate(supporter);
            runtimeSupporter.passiveLevel = Mathf.Clamp(savedSupporter.passiveLevel, 1, 3);
            runtimeSupporter.startSkillLevel = Mathf.Clamp(savedSupporter.startSkillLevel, 1, 3);
            runtimeSupporter.battleSkillLevel = Mathf.Clamp(savedSupporter.battleSkillLevel, 1, 3);

            playerManager.unlockedSupporters.Add(runtimeSupporter);
            if (savedSupporter.active)
                playerManager.activeSupporter = runtimeSupporter;
        }

        foreach (string hiddenBossID in data.clearedHiddenBossIDs ?? new List<string>())
        {
            if (!string.IsNullOrEmpty(hiddenBossID))
                playerManager.MarkHiddenBossCleared(hiddenBossID);
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

    private SupporterChoiceState NormalizeSupporterChoiceState(SavedSupporterState savedSupporter)
    {
        if (savedSupporter == null)
            return SupporterChoiceState.Undecided;

        if (savedSupporter.choiceState == SupporterChoiceState.Undecided && savedSupporter.unlocked)
            return SupporterChoiceState.Recruited;

        return savedSupporter.choiceState;
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
