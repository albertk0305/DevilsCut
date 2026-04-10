using System.Collections.Generic;
using UnityEngine;
using System.Linq;

//탐색 씬 제어 코드
public class ExplorationManager : MonoBehaviour
{
    public static ExplorationManager Instance;

    [Header("모든 시설 데이터 창고")]
    public List<ExplorationNodeData> allNodes;

    [Header("동적 데이터 (저장될 내용들)")]
    public Dictionary<string, int> facilityRanks = new Dictionary<string, int>();

    [Header("현재 상태")]
    public FacilityData lastVisitedFacility;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public List<ExplorationNodeData> GetRandomNodes(int count = 3)
    {
        // [수정된 부분] 밑에 남아있던 return randomPick; 을 지웠습니다!
        return allNodes.OrderBy(x => Random.value).Take(count).ToList();
    }

    public int GetFacilityRank(string id)
    {
        if (facilityRanks.ContainsKey(id)) return facilityRanks[id];
        return 0;
    }
}