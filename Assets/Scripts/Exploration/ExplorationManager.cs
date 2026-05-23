using System.Collections.Generic;
using UnityEngine;
using System.Linq;

// =========================================================
// [추가] 게임의 현재 흐름 상태를 정의합니다.
// =========================================================
public enum GamePhase { BossSelection, Exploration, GeneralBattle, BossBattle, GameClear }

// UI 슬롯과 자연스럽게 연동하기 위한 더미 노드 데이터
public class BossSelectionNodeData : ExplorationNodeData { public BossEncounterData bossData; }
public class PhaseBattleNodeData : DangerNodeData { public BossEncounterData bossData; public bool isBossBattle; }

public class ExplorationManager : MonoBehaviour
{
    public static ExplorationManager Instance;

    [Header("모든 시설 데이터 창고")]
    public List<ExplorationNodeData> allNodes;

    [Header("동적 데이터 (저장될 내용들)")]
    public Dictionary<string, int> facilityRanks = new Dictionary<string, int>();
    public FacilityData lastVisitedFacility;
    public Sprite lastVisitedNodeImage;

    // =========================================================
    // [추가] 게임 진행 (페이즈 및 턴) 트래킹 변수들
    // =========================================================
    [Header("게임 흐름 제어 (Phase & Turn)")]
    public GamePhase currentPhase = GamePhase.BossSelection;
    public int currentCycle = 1; // 1~7: 중간보스, 8: 최종보스, 9: 진최종보스
    public int currentTurnInPhase = 0; // 탐색(0~5), 일반전투(0~2) 진행도

    [Header("재화 및 진행도")]
    public int currentKeys = 0;

    [Header("UI 아이콘 세팅")]
    public Sprite bossSelectionEventIcon;

    [Header("보스 데이터 세팅")]
    public List<BossEncounterData> remainingMidBosses; // 7명의 중간보스 리스트
    public BossEncounterData finalBoss;
    public BossEncounterData trueFinalBoss;
    public BossEncounterData currentTargetBoss; // 유저가 이번 페이즈에 픽한 보스

    private readonly List<ExplorationNodeData> currentOptions = new List<ExplorationNodeData>();
    public IReadOnlyList<ExplorationNodeData> CurrentOptions
    {
        get
        {
            EnsureCurrentOptions();
            return currentOptions;
        }
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        RestoreStateFromPlayerManagerIfNeeded();
        ApplyPendingBattleProgressIfNeeded();
        EnsureCurrentOptions();
        SaveStateToPlayerManager();
    }

    // =========================================================
    // [핵심] 현재 상태에 맞춰 화면에 뿌려줄 3개의 슬롯 데이터를 만듭니다.
    // 빈 슬롯은 null을 넣어 UI가 위치를(대칭을) 잡을 수 있게 합니다.
    // =========================================================
    public IReadOnlyList<ExplorationNodeData> GenerateCurrentOptions()
    {
        currentOptions.Clear();
        currentOptions.AddRange(BuildOptionsForCurrentPhase());

        if (SaveManager.Instance != null)
            SaveManager.Instance.AutoSaveContinue();
        return currentOptions;
    }

    private void EnsureCurrentOptions()
    {
        if (currentOptions.Count != 3)
            GenerateCurrentOptions();
    }

    // 기존 호출부 호환용. 이미 확정된 선택지가 있으면 새로 랜덤을 돌리지 않습니다.
    public List<ExplorationNodeData> GetCurrentOptions()
    {
        EnsureCurrentOptions();
        return new List<ExplorationNodeData>(currentOptions);
    }

    private List<ExplorationNodeData> BuildOptionsForCurrentPhase()
    {
        List<ExplorationNodeData> options = new List<ExplorationNodeData> { null, null, null };

        switch (currentPhase)
        {
            case GamePhase.BossSelection:
                List<BossEncounterData> candidates = GetBossCandidates();

                // 대칭 배치 로직
                if (candidates.Count >= 3)
                {
                    options[0] = CreateBossNode(candidates[0]);
                    options[1] = CreateBossNode(candidates[1]);
                    options[2] = CreateBossNode(candidates[2]);
                }
                else if (candidates.Count == 2)
                {
                    options[0] = CreateBossNode(candidates[0]); // 왼쪽
                    options[2] = CreateBossNode(candidates[1]); // 오른쪽
                }
                else if (candidates.Count == 1)
                {
                    options[1] = CreateBossNode(candidates[0]); // 가운데
                }
                break;

            case GamePhase.Exploration:
                // 기존 탐색: 랜덤 3개
                var randoms = allNodes.OrderBy(x => Random.value).Take(3).ToList();
                for (int i = 0; i < randoms.Count; i++) options[i] = randoms[i];
                break;

            case GamePhase.GeneralBattle:
                if (currentTargetBoss == null)
                {
                    DevLog.LogWarning("[경고] GeneralBattle 진입했으나 currentTargetBoss가 null입니다!");
                    break;
                }
                // 일반 전투: 가운데(1번)에만 부하 몬스터 노드 배치
                var minionNode = ScriptableObject.CreateInstance<PhaseBattleNodeData>();
                minionNode.bossData = currentTargetBoss;
                minionNode.isBossBattle = false;
                minionNode.enemyToSpawn = currentTargetBoss.minionEnemy;
                minionNode.nodeImage = currentTargetBoss.nodeIcon;
                options[1] = minionNode;
                break;

            case GamePhase.BossBattle:
                if (currentTargetBoss == null)
                {
                    DevLog.LogWarning("[경고] BossBattle 진입했으나 currentTargetBoss가 null입니다!");
                    break;
                }
                // 보스 전투: 가운데(1번)에만 보스 노드 배치
                var bossNode = ScriptableObject.CreateInstance<PhaseBattleNodeData>();
                bossNode.bossData = currentTargetBoss;
                bossNode.isBossBattle = true;
                bossNode.enemyToSpawn = currentTargetBoss.bossEnemy;
                bossNode.nodeImage = currentTargetBoss.nodeIcon;
                options[1] = bossNode;
                break;
        }

        return options;
    }

    private List<BossEncounterData> GetBossCandidates()
    {
        if (currentCycle <= 7)
        {
            return remainingMidBosses.OrderBy(x => Random.value).Take(3).ToList();
        }
        else if (currentCycle == 8) return finalBoss != null ? new List<BossEncounterData> { finalBoss } : new List<BossEncounterData>();
        else return trueFinalBoss != null ? new List<BossEncounterData> { trueFinalBoss } : new List<BossEncounterData>();
    }

    private BossSelectionNodeData CreateBossNode(BossEncounterData data)
    {
        // 방어 코드: 데이터가 비어있으면 노드도 만들지 않고 null 반환 (검은 화면 방지!)
        if (data == null)
        {
            DevLog.LogWarning($"[경고] 보스 데이터가 비어있습니다! 인스펙터를 확인해주세요.");
            return null;
        }

        var node = ScriptableObject.CreateInstance<BossSelectionNodeData>();
        node.bossData = data;
        node.nodeImage = bossSelectionEventIcon != null ? bossSelectionEventIcon : data.nodeIcon;
        return node;
    }

    // =========================================================
    // [핵심] 유저가 버튼을 눌러 다음 단계로 넘어갈 때 턴을 진행시킵니다.
    // =========================================================
    public void SelectTargetBoss(BossEncounterData selected)
    {
        currentTargetBoss = selected;
        currentPhase = GamePhase.Exploration;
        currentTurnInPhase = 0;
        DevLog.Log($"[사이클 {currentCycle}] 목표 보스 '{selected.bossName}' 선택 완료. 탐색 페이즈 돌입!");
        GenerateCurrentOptions();
    }

    public void AdvanceExplorationTurn()
    {
        currentTurnInPhase++;
        if (currentTurnInPhase >= 6) // 6턴 꽉 채웠으면
        {
            currentPhase = GamePhase.GeneralBattle;
            currentTurnInPhase = 0;
            DevLog.Log("탐색 6턴 종료. 일반 전투 페이즈 돌입!");
        }
        GenerateCurrentOptions();
        ExplorationManager.Instance.SaveStateToPlayerManager();
    }

    public void AdvanceBattleTurn(bool isBoss)
    {
        if (!isBoss)
        {
            currentTurnInPhase++;

            if (currentTurnInPhase >= 3)
            {
                currentPhase = GamePhase.BossBattle;
                currentTurnInPhase = 0;
                DevLog.Log("일반 전투 3회 완료. 보스 전투 돌입!");
            }
        }
        else
        {
            if (IsCurrentBattleMidBoss())
            {
                currentKeys++;
                DevLog.Log($"[중간보스 보상] Key +1, 현재 Key: {currentKeys}");
            }

            // [수정] 7사이클까지만 중간보스 리스트 차감
            if (currentCycle <= 7 && remainingMidBosses != null)
            {
                if (currentTargetBoss != null && !string.IsNullOrEmpty(currentTargetBoss.bossID))
                    remainingMidBosses.RemoveAll(b => b != null && b.bossID == currentTargetBoss.bossID);
                else
                    remainingMidBosses.Remove(currentTargetBoss);
            }

            currentCycle++;
            currentTargetBoss = null;
            currentPhase = GamePhase.BossSelection;
            currentTurnInPhase = 0;
            DevLog.Log($"보스 처치! 다음 사이클({currentCycle})로 넘어갑니다.");
        }

        GenerateCurrentOptions();
        SaveStateToPlayerManager();
    }

    public List<SavedExplorationOption> GetCurrentOptionsForSave()
    {
        EnsureCurrentOptions();

        List<SavedExplorationOption> savedOptions = new List<SavedExplorationOption>();

        for (int i = 0; i < currentOptions.Count; i++)
        {
            ExplorationNodeData option = currentOptions[i];
            SavedExplorationOption saved = new SavedExplorationOption
            {
                slotIndex = i,
                optionType = "None"
            };

            if (option is FacilityData facility)
            {
                saved.optionType = "Facility";
                saved.nodeID = facility.nodeID;
            }
            else if (option is BossSelectionNodeData bossSelection)
            {
                saved.optionType = "BossSelection";
                saved.bossID = bossSelection.bossData != null ? bossSelection.bossData.bossID : null;
            }
            else if (option is PhaseBattleNodeData battleData)
            {
                saved.optionType = battleData.isBossBattle ? "BossBattle" : "GeneralBattle";
                saved.bossID = battleData.bossData != null ? battleData.bossData.bossID : null;
                saved.battleType = battleData.isBossBattle ? BattleType.Boss : BattleType.General;
                saved.isBossBattle = battleData.isBossBattle;
            }

            savedOptions.Add(saved);
        }

        return savedOptions;
    }

    public bool RestoreCurrentOptionsFromSave(List<SavedExplorationOption> savedOptions, BossDatabase bossDatabase, ExplorationNodeDatabase nodeDatabase)
    {
        if (savedOptions == null || savedOptions.Count != 3)
        {
            DevLog.LogWarning("[Save] currentOptions 복원 실패: 저장된 선택지가 3개가 아닙니다.");
            return false;
        }

        List<ExplorationNodeData> restoredOptions = new List<ExplorationNodeData> { null, null, null };
        bool[] restoredSlots = new bool[3];

        foreach (SavedExplorationOption savedOption in savedOptions)
        {
            if (savedOption == null)
            {
                DevLog.LogWarning("[Save] currentOptions 복원 실패: 비어있는 선택지 데이터가 있습니다.");
                return false;
            }

            int slotIndex = savedOption.slotIndex;
            if (slotIndex < 0 || slotIndex >= 3)
            {
                DevLog.LogWarning($"[Save] currentOptions 복원 실패: 잘못된 슬롯 인덱스 {slotIndex}.");
                return false;
            }

            if (restoredSlots[slotIndex])
            {
                DevLog.LogWarning($"[Save] currentOptions 복원 실패: 중복 슬롯 인덱스 {slotIndex}.");
                return false;
            }

            ExplorationNodeData restoredNode = CreateOptionFromSave(savedOption, bossDatabase, nodeDatabase);
            if (savedOption.optionType != "None" && restoredNode == null)
            {
                DevLog.LogWarning($"[Save] currentOptions 복원 실패: {savedOption.optionType} 슬롯 {slotIndex} 복원 불가.");
                return false;
            }

            restoredOptions[slotIndex] = restoredNode;
            restoredSlots[slotIndex] = true;
        }

        for (int i = 0; i < restoredSlots.Length; i++)
        {
            if (!restoredSlots[i])
            {
                DevLog.LogWarning($"[Save] currentOptions 복원 실패: 슬롯 {i} 데이터가 없습니다.");
                return false;
            }
        }

        currentOptions.Clear();
        currentOptions.AddRange(restoredOptions);
        return true;
    }

    private ExplorationNodeData CreateOptionFromSave(SavedExplorationOption savedOption, BossDatabase bossDatabase, ExplorationNodeDatabase nodeDatabase)
    {
        switch (savedOption.optionType)
        {
            case "None":
                return null;

            case "Facility":
                if (nodeDatabase == null)
                {
                    DevLog.LogWarning("[Save] Facility 선택지 복원 실패: ExplorationNodeDatabase가 없습니다.");
                    return null;
                }
                return nodeDatabase.GetByID(savedOption.nodeID);

            case "BossSelection":
            {
                BossEncounterData boss = FindBossForSave(savedOption.bossID, bossDatabase);
                if (boss == null)
                    return null;

                return CreateBossNode(boss);
            }

            case "GeneralBattle":
            case "BossBattle":
            {
                BossEncounterData boss = FindBossForSave(savedOption.bossID, bossDatabase);
                if (boss == null)
                    return null;

                bool isBossBattle = savedOption.optionType == "BossBattle";
                PhaseBattleNodeData battleNode = ScriptableObject.CreateInstance<PhaseBattleNodeData>();
                battleNode.bossData = boss;
                battleNode.isBossBattle = isBossBattle;
                battleNode.enemyToSpawn = isBossBattle ? boss.bossEnemy : boss.minionEnemy;
                battleNode.nodeImage = boss.nodeIcon;
                return battleNode;
            }

            default:
                DevLog.LogWarning($"[Save] 알 수 없는 선택지 타입: {savedOption.optionType}");
                return null;
        }
    }

    private BossEncounterData FindBossForSave(string bossID, BossDatabase bossDatabase)
    {
        if (bossDatabase == null)
        {
            DevLog.LogWarning("[Save] 보스 복원 실패: BossDatabase가 없습니다.");
            return null;
        }

        return bossDatabase.GetByID(bossID);
    }

    public string GetCurrentTargetBossIDForSave()
    {
        return currentTargetBoss != null ? currentTargetBoss.bossID : null;
    }

    public List<string> GetRemainingMidBossIDsForSave()
    {
        List<string> bossIDs = new List<string>();

        if (remainingMidBosses == null)
            return bossIDs;

        foreach (BossEncounterData boss in remainingMidBosses)
        {
            if (boss != null)
                bossIDs.Add(boss.bossID);
        }

        return bossIDs;
    }

    public bool RestoreBossProgressFromSave(string currentTargetBossID, List<string> remainingMidBossIDs, BossDatabase bossDatabase)
    {
        if (bossDatabase == null)
        {
            DevLog.LogWarning("[Save] 보스 진행도 복원 실패: BossDatabase가 없습니다.");
            return false;
        }

        BossEncounterData restoredTargetBoss = null;
        if (!string.IsNullOrEmpty(currentTargetBossID))
        {
            restoredTargetBoss = bossDatabase.GetByID(currentTargetBossID);
            if (restoredTargetBoss == null)
            {
                DevLog.LogWarning($"[Save] currentTargetBoss 복원 실패: {currentTargetBossID}");
                return false;
            }
        }

        List<BossEncounterData> restoredRemainingBosses = new List<BossEncounterData>();
        if (remainingMidBossIDs != null)
        {
            foreach (string bossID in remainingMidBossIDs)
            {
                BossEncounterData boss = bossDatabase.GetByID(bossID);
                if (boss == null)
                {
                    DevLog.LogWarning($"[Save] remainingMidBoss 복원 실패: {bossID}");
                    return false;
                }

                restoredRemainingBosses.Add(boss);
            }
        }

        currentTargetBoss = restoredTargetBoss;
        remainingMidBosses = restoredRemainingBosses;
        return true;
    }
    public int GetFacilityRank(string id)
    {
        if (facilityRanks.ContainsKey(id)) return facilityRanks[id];
        return 0;
    }

    private void ApplyPendingBattleProgressIfNeeded()
    {
        PlayerManager playerManager = PlayerManager.Instance;

        if (playerManager == null)
            return;

        if (!playerManager.pendingAdvanceBattleTurn)
            return;

        bool isBossBattle =
            playerManager.pendingBattleType == BattleType.Boss ||
            playerManager.pendingBattleType == BattleType.FinalBoss;

        playerManager.pendingAdvanceBattleTurn = false;

        AdvanceBattleTurn(isBossBattle);
    }

    public void SaveStateToPlayerManager()
    {
        PlayerManager playerManager = PlayerManager.Instance;

        if (playerManager == null)
            return;

        playerManager.hasSavedExplorationState = true;
        playerManager.savedExplorationPhase = currentPhase;
        playerManager.savedExplorationCycle = currentCycle;
        playerManager.savedExplorationTurnInPhase = currentTurnInPhase;
        playerManager.savedExplorationKeys = currentKeys;
        playerManager.savedCurrentTargetBoss = currentTargetBoss;
        playerManager.savedLastVisitedNodeImage = lastVisitedNodeImage;
        playerManager.savedLastVisitedFacility = lastVisitedFacility;
    }

    private void RestoreStateFromPlayerManagerIfNeeded()
    {
        PlayerManager playerManager = PlayerManager.Instance;

        if (playerManager == null)
            return;

        if (!playerManager.hasSavedExplorationState)
            return;

        currentPhase = playerManager.savedExplorationPhase;
        currentCycle = playerManager.savedExplorationCycle;
        currentTurnInPhase = playerManager.savedExplorationTurnInPhase;
        currentKeys = playerManager.savedExplorationKeys;
        currentTargetBoss = playerManager.savedCurrentTargetBoss;
        lastVisitedNodeImage = playerManager.savedLastVisitedNodeImage;
        lastVisitedFacility = playerManager.savedLastVisitedFacility;
    }

    private bool IsSameBoss(BossEncounterData a, BossEncounterData b)
    {
        if (a == null || b == null)
            return false;

        if (!string.IsNullOrEmpty(a.bossID) && !string.IsNullOrEmpty(b.bossID))
            return a.bossID == b.bossID;

        return a == b;
    }
    private bool IsCurrentBattleMidBoss()
    {
        if (currentTargetBoss == null)
            return false;

        if (IsSameBoss(currentTargetBoss, finalBoss))
            return false;

        if (IsSameBoss(currentTargetBoss, trueFinalBoss))
            return false;

        return true;
    }
}
