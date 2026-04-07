using System.Collections.Generic;
using UnityEngine;
using System.Linq; 

public class ExplorationManager : MonoBehaviour
{
    public static ExplorationManager Instance;

    [Header("모든 시설 데이터 창고")]
    // 아까 만든 ScriptableObject 파일들을 유니티 에디터에서 여기에 다 끌어다 넣을 거야
    public List<FacilityData> allFacilities;

    [Header("동적 데이터 (저장될 내용들)")]
    // 키(ID), 값(Rank)으로 시설별 랭크를 기억해 (예: "shop" -> 2레벨)
    public Dictionary<string, int> facilityRanks = new Dictionary<string, int>();
    // 키(ID), 값(해금여부)으로 조력자 등장 여부를 기억해
    public Dictionary<string, bool> operatorUnlocked = new Dictionary<string, bool>();

    [Header("현재 상태")]
    public FacilityData lastVisitedFacility; // 마지막으로 방문한 시설! (여기에 변수로 쏙 담아두면 끝)

    private void Awake()
    {
        if (Instance == null) Instance = this;
        // (나중에는 여기서 PlayerPrefs나 JSON으로 저장된 랭크/해금 데이터를 불러올 거야)
    }

    // 랜덤으로 3개의 시설을 뽑아주는 핵심 함수!
    public List<FacilityData> GetRandomFacilities(int count = 3)
    {
        // 1. 모든 시설 리스트를 무작위로 섞는다. (OrderBy와 Random.value 사용)
        // 2. 그중에서 앞에서부터 count(3)개만 쏙 뽑아온다.
        List<FacilityData> randomPick = allFacilities.OrderBy(x => Random.value).Take(count).ToList();

        return randomPick;
    }

    // 특정 시설의 현재 랭크를 물어보는 함수
    public int GetFacilityRank(string id)
    {
        if (facilityRanks.ContainsKey(id)) return facilityRanks[id];
        return 0; // 기본 랭크는 0
    }
}