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
    private string ClearDataDirectoryPath => Path.Combine(Application.persistentDataPath, "clear_data");
    private string ClearDataRecordsDirectoryPath => Path.Combine(ClearDataDirectoryPath, "records");
    private string ClearDataIndexPath => Path.Combine(ClearDataDirectoryPath, "clear_data_index.json");
    private string TempClearDataIndexPath => Path.Combine(ClearDataDirectoryPath, "clear_data_index.json.tmp");
    private string BackupClearDataIndexPath => Path.Combine(ClearDataDirectoryPath, "clear_data_index.json.bak");

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

    public bool TrySaveGameClearRecord(out string clearId)
    {
        clearId = "";

        if (PlayerManager.Instance == null)
        {
            DevLog.LogWarning("[Save] Game clear record save failed: PlayerManager missing.");
            return false;
        }

        try
        {
            ClearDataIndex index = LoadClearDataIndex();
            int clearNumber = Mathf.Max(1, index.nextClearNumber);
            clearId = BuildClearId(clearNumber);
            string clearedAt = DateTime.UtcNow.ToString("o");
            PlayerGrowthSaveData playerGrowth = BuildPlayerGrowthSaveData(PlayerManager.Instance);
            GameClearRecordData record = new GameClearRecordData
            {
                schemaVersion = 1,
                clearNumber = clearNumber,
                clearId = clearId,
                clearedAt = clearedAt,
                playerGrowth = playerGrowth
            };

            WriteGameClearRecord(record);

            index.nextClearNumber = clearNumber + 1;
            index.totalIssuedCount += 1;
            index.totalClearCount = index.totalIssuedCount;
            index.totalSavedCount += 1;
            index.records.Add(BuildClearRecordSummary(record, playerGrowth));
            WriteClearDataIndex(index);

            DevLog.Log($"[Save] Game clear record saved: clearId={clearId}");
            return true;
        }
        catch (Exception ex)
        {
            DevLog.LogWarning($"[Save] Game clear record save failed: {ex.Message}");
            return false;
        }
    }

    public bool TryDiscardGameClearRecord(out string clearId)
    {
        clearId = "";

        try
        {
            ClearDataIndex index = LoadClearDataIndex();
            int clearNumber = Mathf.Max(1, index.nextClearNumber);
            clearId = BuildClearId(clearNumber);

            index.nextClearNumber = clearNumber + 1;
            index.totalIssuedCount += 1;
            index.totalClearCount = index.totalIssuedCount;
            if (index.discardedClearIds == null)
                index.discardedClearIds = new List<string>();

            index.discardedClearIds.Add(clearId);
            WriteClearDataIndex(index);

            DevLog.Log($"[Save] Game clear record discarded: clearId={clearId}");
            return true;
        }
        catch (Exception ex)
        {
            DevLog.LogWarning($"[Save] Game clear discard failed: {ex.Message}");
            return false;
        }
    }

    public List<ClearRecordSummary> LoadClearRecordSummaries()
    {
        ClearDataIndex index = LoadClearDataIndex();
        List<ClearRecordSummary> summaries = new List<ClearRecordSummary>();
        foreach (ClearRecordSummary summary in index.records)
        {
            if (summary != null && !string.IsNullOrEmpty(summary.clearId))
                summaries.Add(summary);
        }

        return summaries;
    }

    public bool HasAnyClearRecords()
    {
        ClearDataIndex index = LoadClearDataIndex();
        return index.records != null && index.records.Count > 0;
    }

    public GameClearRecordData LoadGameClearRecord(string clearId)
    {
        if (!TryParseClearNumber(clearId, out int clearNumber))
            return null;

        string path = GetGameClearRecordPath(clearNumber);
        if (!File.Exists(path))
            return null;

        try
        {
            string json = File.ReadAllText(path);
            return JsonUtility.FromJson<GameClearRecordData>(json);
        }
        catch (Exception ex)
        {
            DevLog.LogWarning($"[Save] Failed to read game clear record '{clearId}': {ex.Message}");
            return null;
        }
    }

    public bool UpdateGameClearRecord(GameClearRecordData record)
    {
        if (record == null || string.IsNullOrEmpty(record.clearId))
            return false;

        if (!TryParseClearNumber(record.clearId, out int clearNumber))
            return false;

        if (record.clearNumber != clearNumber)
        {
            DevLog.LogWarning($"[Save] Game clear record update failed: clearId and clearNumber mismatch. clearId={record.clearId}, clearNumber={record.clearNumber}");
            return false;
        }

        try
        {
            WriteGameClearRecord(record);
            DevLog.Log($"[Save] Game clear record updated: clearId={record.clearId}");
            return true;
        }
        catch (Exception ex)
        {
            DevLog.LogWarning($"[Save] Game clear record update failed: clearId={record.clearId}, {ex.Message}");
            return false;
        }
    }

    public bool UpdateInfiniteBattleBestFloor(string clearId, int clearedFloorCount)
    {
        if (string.IsNullOrEmpty(clearId))
            return false;

        int normalizedFloor = Mathf.Max(0, clearedFloorCount);
        GameClearRecordData record = LoadGameClearRecord(clearId);
        if (record == null)
            return false;

        int bestFloor = Mathf.Max(record.infiniteBattleBestFloor, normalizedFloor);
        bool shouldWriteRecord = bestFloor > record.infiniteBattleBestFloor;

        record.infiniteBattleBestFloor = bestFloor;

        try
        {
            if (shouldWriteRecord)
                WriteGameClearRecord(record);

            ClearDataIndex index = LoadClearDataIndex();
            if (index.records != null)
            {
                foreach (ClearRecordSummary summary in index.records)
                {
                    if (summary != null && summary.clearId == clearId)
                    {
                        summary.infiniteBattleBestFloor = Mathf.Max(summary.infiniteBattleBestFloor, bestFloor);
                        break;
                    }
                }
            }

            WriteClearDataIndex(index);
            DevLog.Log($"[InfiniteBattle] Best floor updated. clearId={clearId}, bestFloor={bestFloor}");
            return true;
        }
        catch (Exception ex)
        {
            DevLog.LogWarning($"[InfiniteBattle] Best floor update failed. clearId={clearId}, {ex.Message}");
            return false;
        }
    }

    public bool DeleteGameClearRecord(string clearId)
    {
        if (string.IsNullOrEmpty(clearId))
            return false;

        try
        {
            ClearDataIndex index = LoadClearDataIndex();
            ClearRecordSummary summary = null;
            for (int i = 0; i < index.records.Count; i++)
            {
                ClearRecordSummary candidate = index.records[i];
                if (candidate != null && candidate.clearId == clearId)
                {
                    summary = candidate;
                    index.records.RemoveAt(i);
                    break;
                }
            }

            if (summary == null)
                return false;

            index.totalSavedCount = Mathf.Max(0, index.records.Count);
            WriteClearDataIndex(index);

            string recordPath = GetGameClearRecordPath(summary.clearNumber);
            DeleteFileIfExists(recordPath);
            DeleteFileIfExists(recordPath + ".tmp");
            DeleteFileIfExists(recordPath + ".bak");

            DevLog.Log($"[Save] Game clear record deleted: clearId={clearId}");
            return true;
        }
        catch (Exception ex)
        {
            DevLog.LogWarning($"[Save] Game clear record delete failed: clearId={clearId}, {ex.Message}");
            return false;
        }
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

    private ClearDataIndex LoadClearDataIndex()
    {
        ClearDataIndex index = null;

        if (File.Exists(ClearDataIndexPath))
        {
            try
            {
                string json = File.ReadAllText(ClearDataIndexPath);
                index = JsonUtility.FromJson<ClearDataIndex>(json);
            }
            catch (Exception ex)
            {
                DevLog.LogWarning($"[Save] Failed to read clear data index. Trying backup. {ex.Message}");
            }
        }

        if (index == null && File.Exists(BackupClearDataIndexPath))
        {
            try
            {
                string json = File.ReadAllText(BackupClearDataIndexPath);
                index = JsonUtility.FromJson<ClearDataIndex>(json);
                DevLog.LogWarning($"[Save] Backup clear data index loaded: {BackupClearDataIndexPath}");
            }
            catch (Exception ex)
            {
                DevLog.LogWarning($"[Save] Failed to read backup clear data index: {ex.Message}");
            }
        }

        bool rebuiltFromRecords = index == null;
        index = NormalizeClearDataIndex(index);
        return ReconcileClearDataIndex(index, rebuiltFromRecords);
    }

    private ClearDataIndex NormalizeClearDataIndex(ClearDataIndex index)
    {
        if (index == null)
            index = new ClearDataIndex();

        if (index.schemaVersion <= 0)
            index.schemaVersion = 1;

        if (index.records == null)
            index.records = new List<ClearRecordSummary>();

        if (index.discardedClearIds == null)
            index.discardedClearIds = new List<string>();

        int minimumNextClearNumber = Mathf.Max(1, index.totalIssuedCount + 1);
        foreach (ClearRecordSummary summary in index.records)
        {
            if (summary != null)
                minimumNextClearNumber = Mathf.Max(minimumNextClearNumber, summary.clearNumber + 1);
        }

        foreach (string discardedClearId in index.discardedClearIds)
        {
            if (TryParseClearNumber(discardedClearId, out int clearNumber))
                minimumNextClearNumber = Mathf.Max(minimumNextClearNumber, clearNumber + 1);
        }

        if (index.nextClearNumber < minimumNextClearNumber)
            index.nextClearNumber = minimumNextClearNumber;

        index.totalSavedCount = Mathf.Max(index.totalSavedCount, index.records.Count);
        index.totalIssuedCount = Mathf.Max(index.totalIssuedCount, index.nextClearNumber - 1);
        index.totalClearCount = index.totalIssuedCount;
        return index;
    }

    private ClearDataIndex ReconcileClearDataIndex(ClearDataIndex index, bool rebuiltFromRecords)
    {
        bool changed = false;
        int removedMissingCount = 0;
        int removedDuplicateCount = 0;
        int recoveredOrphanCount = 0;
        int refreshedSummaryCount = 0;
        int skippedCorruptCount = 0;
        List<ClearRecordSummary> reconciledRecords = new List<ClearRecordSummary>();
        HashSet<string> knownClearIds = new HashSet<string>();

        foreach (ClearRecordSummary summary in index.records ?? new List<ClearRecordSummary>())
        {
            if (summary == null || string.IsNullOrEmpty(summary.clearId) || !TryParseClearNumber(summary.clearId, out int clearNumber))
            {
                changed = true;
                removedMissingCount++;
                continue;
            }

            if (knownClearIds.Contains(summary.clearId))
            {
                changed = true;
                removedDuplicateCount++;
                continue;
            }

            string recordPath = GetGameClearRecordPath(clearNumber);
            if (!File.Exists(recordPath))
            {
                changed = true;
                removedMissingCount++;
                continue;
            }

            if (!TryLoadGameClearRecordFromPath(recordPath, out GameClearRecordData record))
            {
                changed = true;
                skippedCorruptCount++;
                continue;
            }

            ClearRecordSummary refreshedSummary = BuildClearRecordSummary(record, record.playerGrowth);
            if (!AreClearRecordSummariesEqual(summary, refreshedSummary))
            {
                changed = true;
                refreshedSummaryCount++;
            }

            reconciledRecords.Add(refreshedSummary);
            knownClearIds.Add(refreshedSummary.clearId);
        }

        foreach (string recordPath in GetGameClearRecordFilePaths())
        {
            if (!TryLoadGameClearRecordFromPath(recordPath, out GameClearRecordData record))
            {
                skippedCorruptCount++;
                continue;
            }

            if (knownClearIds.Contains(record.clearId))
                continue;

            reconciledRecords.Add(BuildClearRecordSummary(record, record.playerGrowth));
            knownClearIds.Add(record.clearId);
            recoveredOrphanCount++;
            changed = true;
        }

        reconciledRecords.Sort((a, b) =>
        {
            int aNumber = a != null ? a.clearNumber : int.MaxValue;
            int bNumber = b != null ? b.clearNumber : int.MaxValue;
            return aNumber.CompareTo(bNumber);
        });

        if (!AreClearRecordSummaryListsEqual(index.records, reconciledRecords))
            changed = true;

        index.records = reconciledRecords;
        index = NormalizeClearDataIndex(index);

        if (changed)
        {
            DevLog.LogWarning(
                $"[Save] Clear data index reconciled. rebuiltFromRecords={rebuiltFromRecords}, removedMissing={removedMissingCount}, removedDuplicate={removedDuplicateCount}, recoveredOrphan={recoveredOrphanCount}, refreshedSummary={refreshedSummaryCount}, skippedCorrupt={skippedCorruptCount}");

            try
            {
                WriteClearDataIndex(index);
            }
            catch (Exception ex)
            {
                DevLog.LogWarning($"[Save] Failed to write reconciled clear data index: {ex.Message}");
            }
        }

        return index;
    }

    private List<string> GetGameClearRecordFilePaths()
    {
        List<string> paths = new List<string>();
        if (!Directory.Exists(ClearDataRecordsDirectoryPath))
            return paths;

        try
        {
            foreach (string path in Directory.GetFiles(ClearDataRecordsDirectoryPath, "clear_record_*.json"))
            {
                string fileName = Path.GetFileName(path);
                if (!string.IsNullOrEmpty(fileName)
                    && fileName.StartsWith("clear_record_")
                    && Path.GetExtension(fileName) == ".json")
                {
                    paths.Add(path);
                }
            }
        }
        catch (Exception ex)
        {
            DevLog.LogWarning($"[Save] Failed to scan clear record files: {ex.Message}");
        }

        return paths;
    }

    private bool TryLoadGameClearRecordFromPath(string path, out GameClearRecordData record)
    {
        record = null;
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
            return false;

        try
        {
            string json = File.ReadAllText(path);
            record = JsonUtility.FromJson<GameClearRecordData>(json);
            if (!IsValidGameClearRecord(record))
            {
                DevLog.LogWarning($"[Save] Invalid clear record skipped: {path}");
                record = null;
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            DevLog.LogWarning($"[Save] Corrupt clear record skipped: {path}, {ex.Message}");
            return false;
        }
    }

    private bool IsValidGameClearRecord(GameClearRecordData record)
    {
        if (record == null || string.IsNullOrEmpty(record.clearId) || record.clearNumber <= 0)
            return false;

        if (!TryParseClearNumber(record.clearId, out int clearNumber))
            return false;

        return clearNumber == record.clearNumber;
    }

    private bool AreClearRecordSummaryListsEqual(List<ClearRecordSummary> left, List<ClearRecordSummary> right)
    {
        int leftCount = left != null ? left.Count : 0;
        int rightCount = right != null ? right.Count : 0;
        if (leftCount != rightCount)
            return false;

        for (int i = 0; i < leftCount; i++)
        {
            if (!AreClearRecordSummariesEqual(left[i], right[i]))
                return false;
        }

        return true;
    }

    private bool AreClearRecordSummariesEqual(ClearRecordSummary left, ClearRecordSummary right)
    {
        if (left == null || right == null)
            return left == right;

        return left.clearNumber == right.clearNumber
            && left.clearId == right.clearId
            && left.clearedAt == right.clearedAt
            && left.infiniteBattleBestFloor == right.infiniteBattleBestFloor
            && left.level == right.level
            && left.maxHp == right.maxHp
            && left.currentHp == right.currentHp
            && left.actionPoints == right.actionPoints
            && left.strength == right.strength
            && left.defense == right.defense
            && left.speed == right.speed
            && left.luck == right.luck
            && left.currentGold == right.currentGold
            && left.rejectedSupporterCount == right.rejectedSupporterCount
            && left.ownedSupporterCount == right.ownedSupporterCount
            && left.ownedItemCount == right.ownedItemCount;
    }

    private void WriteGameClearRecord(GameClearRecordData record)
    {
        if (record == null)
            throw new InvalidOperationException("Game clear record is null.");

        string path = GetGameClearRecordPath(record.clearNumber);
        string tempPath = path + ".tmp";
        string backupPath = path + ".bak";
        string json = JsonUtility.ToJson(record, true);
        WriteFileSafely(json, path, tempPath, backupPath);
    }

    private void WriteClearDataIndex(ClearDataIndex index)
    {
        if (index == null)
            throw new InvalidOperationException("Clear data index is null.");

        string json = JsonUtility.ToJson(index, true);
        WriteFileSafely(json, ClearDataIndexPath, TempClearDataIndexPath, BackupClearDataIndexPath);
    }

    private ClearRecordSummary BuildClearRecordSummary(GameClearRecordData record, PlayerGrowthSaveData playerGrowth)
    {
        return new ClearRecordSummary
        {
            clearNumber = record.clearNumber,
            clearId = record.clearId,
            clearedAt = record.clearedAt,
            infiniteBattleBestFloor = record.infiniteBattleBestFloor,
            level = playerGrowth != null ? playerGrowth.level : 0,
            maxHp = playerGrowth != null ? playerGrowth.maxHp : 0,
            currentHp = playerGrowth != null ? playerGrowth.currentHp : 0,
            actionPoints = playerGrowth != null ? playerGrowth.actionPoints : 0,
            strength = playerGrowth != null ? playerGrowth.strength : 0,
            defense = playerGrowth != null ? playerGrowth.defense : 0,
            speed = playerGrowth != null ? playerGrowth.speed : 0,
            luck = playerGrowth != null ? playerGrowth.luck : 0,
            currentGold = playerGrowth != null ? playerGrowth.currentGold : 0,
            rejectedSupporterCount = playerGrowth != null ? playerGrowth.rejectedSupporterCount : 0,
            ownedSupporterCount = playerGrowth != null && playerGrowth.supporters != null ? playerGrowth.supporters.Count : 0,
            ownedItemCount = playerGrowth != null && playerGrowth.inventory != null ? playerGrowth.inventory.Count : 0
        };
    }

    private string GetGameClearRecordPath(int clearNumber)
    {
        return Path.Combine(ClearDataRecordsDirectoryPath, $"clear_record_{clearNumber:D6}.json");
    }

    private string BuildClearId(int clearNumber)
    {
        return $"clear_{clearNumber:D6}";
    }

    private bool TryParseClearNumber(string clearId, out int clearNumber)
    {
        clearNumber = 0;
        if (string.IsNullOrEmpty(clearId) || !clearId.StartsWith("clear_"))
            return false;

        return int.TryParse(clearId.Substring("clear_".Length), out clearNumber);
    }

    private void WriteContinueSaveSafely(string json)
    {
        WriteFileSafely(json, ContinueSavePath, TempSavePath, BackupSavePath);
    }

    private void WriteFileSafely(string json, string savePath, string tempPath, string backupPath)
    {
        string directoryPath = Path.GetDirectoryName(savePath);
        if (!string.IsNullOrEmpty(directoryPath))
            Directory.CreateDirectory(directoryPath);

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

        RequestWebGLSaveSync(savePath);
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
        {
            File.Delete(path);
            RequestWebGLSaveSync(path);
        }
    }

    private void RequestWebGLSaveSync(string reason)
    {
        WebGLSaveSync.RequestSync(reason);
    }
}
