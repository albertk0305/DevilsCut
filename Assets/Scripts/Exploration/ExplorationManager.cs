using System.Collections.Generic;
using UnityEngine;
using System.Linq;

// =========================================================
// [추가] 게임의 현재 흐름 상태를 정의합니다.
// =========================================================
public enum GamePhase { BossSelection, Exploration, GeneralBattle, BossBattle, GameClear }

[System.Serializable]
public class BossEncounterData
{
    public string bossName;
    public EnemyData minionEnemy; // 해당 보스의 일반 몹 (부하)
    public EnemyData bossEnemy;   // 보스 본인
    public Sprite nodeIcon;       // 선택지에 띄울 시설용(전투) 아이콘
    public Sprite defaultSD;      // 조력자 위치에 띄울 기본 SD
    public Sprite readySD;        // 클릭 시 띄울 준비 SD
}

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

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    // =========================================================
    // [핵심] 현재 상태에 맞춰 화면에 뿌려줄 3개의 슬롯 데이터를 만듭니다.
    // 빈 슬롯은 null을 넣어 UI가 위치를(대칭을) 잡을 수 있게 합니다.
    // =========================================================
    public List<ExplorationNodeData> GetCurrentOptions()
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
            // [수정] 7사이클까지만 중간보스 리스트 차감
            if (currentCycle <= 7) remainingMidBosses.Remove(currentTargetBoss);

            // 열쇠 획득은 CombatManager에서 ExplorationManager.Instance.currentKeys++; 로 올려주시면 됩니다!

            currentCycle++;
            currentTargetBoss = null;
            currentPhase = GamePhase.BossSelection;
            currentTurnInPhase = 0;
            DevLog.Log($"보스 처치! 다음 사이클({currentCycle})로 넘어갑니다.");
        }
    }

    public int GetFacilityRank(string id)
    {
        if (facilityRanks.ContainsKey(id)) return facilityRanks[id];
        return 0;
    }
}