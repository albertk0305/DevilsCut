using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; 
using TMPro; 

//탐색 씬 UI 제어 코드
public class ExplorationUI : MonoBehaviour
{
    [Header("좌측 & 하단 (고정 및 단일 슬롯)")]
    public Image playerImage;         // 주인공
    public Image companionImage;      // 동행 조력자
    public Image guideImage;          // 내비 가이드
    public Image lastFacilityImage;   // 마지막 방문 시설

    [Header("우측 (랜덤 시설 3개 슬롯)")]
    public Image[] randomFacilityImages;
    public Image[] randomOperatorImages;
    public TextMeshProUGUI[] randomRankTexts;

    [Header("기본 운영자 (Baito)")]
    public Sprite baitoNormal; // Baito 기본 표정
    public Sprite baitoSmile;  // Baito 웃는 표정

    [Header("선택 팝업 UI")]
    public GameObject confirmPopup; // 화면 중앙의 예/아니오 팝업창 묶음

    [Header("전체 메뉴 UI")]
    public GameObject statusCanvas;
    public GameObject settingsCanvas;

    private List<ExplorationNodeData> currentOptions = new List<ExplorationNodeData>(); // 현재 선택지들
    private int selectedIndex = -1; // 현재 클릭된 시설의 번호 (0, 1, 2)

    void Start()
    {
        InitializeSceneUI();
    }

    public void InitializeSceneUI()
    {
        // 1. 주인공 & 내비 가이드 세팅
        // (보통 유니티 인스펙터 창에서 Image 컴포넌트에 기본 스프라이트를 넣어두면 코드로 안 건드려도 알아서 잘 떠 있어!)

        // 2. 동행 조력자 세팅 (임시 로직)
        // 나중에 PartyManager 같은 게 생기면 거기서 현재 파티원 이미지를 가져오면 돼.
        // companionImage.sprite = PartyManager.Instance.GetCurrentCompanionSprite();

        // [추가됨] 씬이 시작될 때 메뉴창(StatusCanvas)을 확실하게 꺼둡니다.
        if (statusCanvas != null) statusCanvas.SetActive(false);
        if (settingsCanvas != null) settingsCanvas.SetActive(false);

        // 시작/초기화할 때 팝업을 확실하게 비활성화 (방어 코드)
        if (confirmPopup != null) confirmPopup.SetActive(false);
        selectedIndex = -1; // 선택 상태도 초기화

        // 이전 사용 시설 이미지 띄우기
        FacilityData lastFacility = ExplorationManager.Instance.lastVisitedFacility;
        if (lastFacility != null && lastFacility.nodeImage != null)
        {
            lastFacilityImage.sprite = lastFacility.nodeImage;
            lastFacilityImage.gameObject.SetActive(true);
        }
        else lastFacilityImage.gameObject.SetActive(false);

        // 3개 무작위 뽑기 및 화면 적용
        currentOptions = ExplorationManager.Instance.GetRandomNodes(3);

        for (int i = 0; i < currentOptions.Count; i++)
        {
            ExplorationNodeData data = currentOptions[i];

            // 1. 공통 처리: 어떤 노드든 버튼 이미지는 띄운다.
            if (randomFacilityImages[i] != null) randomFacilityImages[i].sprite = data.nodeImage;

            // 2. 타입별 처리: 만약 이 데이터가 '시설(FacilityData)'이라면?
            if (data is FacilityData facilityData)
            {
                int currentRank = ExplorationManager.Instance.GetFacilityRank(facilityData.nodeID);

                // 랭크 텍스트 켜기
                if (randomRankTexts[i] != null)
                {
                    randomRankTexts[i].gameObject.SetActive(true);
                    randomRankTexts[i].text = currentRank.ToString();
                }

                // 운영자 이미지 켜기 및 세팅
                if (randomOperatorImages[i] != null)
                {
                    randomOperatorImages[i].gameObject.SetActive(true);
                    if (currentRank > 0 && facilityData.operatorImage != null)
                        randomOperatorImages[i].sprite = facilityData.operatorImage;
                    else
                        randomOperatorImages[i].sprite = baitoNormal;
                }
            }
            // 만약 시설이 아니라 '이벤트'나 '위험(전투)' 칸이라면?
            else
            {
                // 랭크와 운영자 이미지를 아예 꺼버립니다! (버튼 아이콘만 남게 됨)
                if (randomRankTexts[i] != null) randomRankTexts[i].gameObject.SetActive(false);
                if (randomOperatorImages[i] != null) randomOperatorImages[i].gameObject.SetActive(false);
            }
        }
    }

    // 시설 선택 UI 버튼에서 OnClick()으로 연결할 함수 (인자값으로 0, 1, 2를 넘겨줄 거에요)
    public void OnClickFacilitySlot(int slotIndex)
    {
        if (slotIndex >= currentOptions.Count) return;

        if (selectedIndex != -1 && selectedIndex != slotIndex)
        {
            ResetSelectedOperatorFace();
        }

        selectedIndex = slotIndex;
        ExplorationNodeData selectedData = currentOptions[slotIndex];

        if (selectedData is FacilityData facilityData)
        {
            int currentRank = ExplorationManager.Instance.GetFacilityRank(facilityData.nodeID);

            if (currentRank > 0 && facilityData.operatorSmileImage != null)
                randomOperatorImages[slotIndex].sprite = facilityData.operatorSmileImage;
            else
                randomOperatorImages[slotIndex].sprite = baitoSmile;
        }

        confirmPopup.SetActive(true);
    }

    // 클릭했던 시설의 운영자의 표정을 기본 상태로 되돌리는 함수
    private void ResetSelectedOperatorFace()
    {
        if (selectedIndex == -1) return; // 선택된 게 없으면 패스

        ExplorationNodeData prevData = currentOptions[selectedIndex];

        // [수정됨] 이전에 선택했던 노드가 '시설'이었을 때만 표정을 원상 복구합니다.
        if (prevData is FacilityData facilityData)
        {
            int prevRank = ExplorationManager.Instance.GetFacilityRank(facilityData.nodeID);

            if (prevRank > 0 && facilityData.operatorImage != null)
                randomOperatorImages[selectedIndex].sprite = facilityData.operatorImage;
            else
                randomOperatorImages[selectedIndex].sprite = baitoNormal;
        }
    }

    // 팝업에서 'Cancel(취소)' 버튼을 눌렀을 때
    public void OnClickCancel()
    {
        confirmPopup.SetActive(false); // 팝업 닫기
        ResetSelectedOperatorFace();       // 웃는 표정 원상복구
        selectedIndex = -1;
    }

    // 팝업에서 'Confirm(확인)' 버튼을 눌렀을 때
    public void OnClickConfirm()
    {
        if (selectedIndex == -1) return;

        ExplorationNodeData targetData = currentOptions[selectedIndex];

        // [수정됨] 선택한 데이터의 타입에 따라 다른 로그와 행동을 준비합니다.
        if (targetData is FacilityData facility)
        {
            ExplorationManager.Instance.lastVisitedFacility = facility;
            DevLog.Log($"[시설] {facility.nodeID} 씬으로 이동합니다...");
            // SceneManager.LoadScene(facility.nodeID + "Scene"); 
        }
        else if (targetData is EventNodeData eventNode)
        {
            DevLog.Log($"[이벤트] {eventNode.nodeID} 발생! (이벤트 씬 로드)");
            // SceneManager.LoadScene("EventScene"); 
        }
        else if (targetData is DangerNodeData dangerNode)
        {
            DevLog.Log($"[전투] LV.{dangerNode.enemyLevel} 적 출현! (전투 씬 로드)");
            // SceneManager.LoadScene("CombatScene"); 
        }

        confirmPopup.SetActive(false);
    }
}