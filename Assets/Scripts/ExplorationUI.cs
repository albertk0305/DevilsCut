using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; 
using TMPro; 

public class ExplorationUI : MonoBehaviour
{
    [Header("좌측 & 하단 (고정 및 단일 슬롯)")]
    public Image playerImage;         // 주인공
    public Image companionImage;      // 동행 조력자
    public Image guideImage;          // 내비 가이드
    public Image lastFacilityImage;   // 마지막 방문 시설

    [Header("우측 (랜덤 시설 3개 슬롯)")]
    // 인스펙터에서 크기를 3으로 맞추고 각각 연결해 줄 배열들이야
    public Image[] randomFacilityImages;
    public Image[] randomOperatorImages;
    public TextMeshProUGUI[] randomRankTexts;

    [Header("기본 운영자 (Baito)")]
    public Sprite baitoNormal; // Baito 기본 표정
    public Sprite baitoSmile;  // Baito 웃는 표정

    [Header("선택 팝업 UI")]
    public GameObject confirmPopup; // 화면 중앙의 예/아니오 팝업창 묶음

    private List<FacilityData> currentOptions = new List<FacilityData>(); // 현재 화면에 뜬 3개 시설
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

        // 시작/초기화할 때 팝업을 확실하게 비활성화 (방어 코드)
        if (confirmPopup != null) confirmPopup.SetActive(false);
        selectedIndex = -1; // 선택 상태도 초기화

        // 이전 사용 시설 이미지 띄우기
        FacilityData lastFacility = ExplorationManager.Instance.lastVisitedFacility;
        if (lastFacility != null && lastFacility.facilityImage != null)
        {
            lastFacilityImage.sprite = lastFacility.facilityImage;
            lastFacilityImage.gameObject.SetActive(true);
        }
        else lastFacilityImage.gameObject.SetActive(false);

        // 3개 무작위 뽑기 및 화면 적용
        currentOptions = ExplorationManager.Instance.GetRandomFacilities(3);

        for (int i = 0; i < currentOptions.Count; i++)
        {
            FacilityData data = currentOptions[i];
            int currentRank = ExplorationManager.Instance.GetFacilityRank(data.facilityID);

            if (randomFacilityImages[i] != null) randomFacilityImages[i].sprite = data.facilityImage;
            if (randomRankTexts[i] != null) randomRankTexts[i].text = currentRank.ToString();

            // [핵심] 랭크에 따른 운영자 기본 이미지 세팅
            if (randomOperatorImages[i] != null)
            {
                randomOperatorImages[i].gameObject.SetActive(true);
                if (currentRank > 0 && data.operatorImage != null)
                {
                    randomOperatorImages[i].sprite = data.operatorImage; // 해금된 조력자
                }
                else
                {
                    randomOperatorImages[i].sprite = baitoNormal; // 미해금: Baito
                }
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
        FacilityData selectedData = currentOptions[slotIndex];
        int currentRank = ExplorationManager.Instance.GetFacilityRank(selectedData.facilityID);

        if (currentRank > 0 && selectedData.operatorSmileImage != null)
        {
            randomOperatorImages[slotIndex].sprite = selectedData.operatorSmileImage;
        }
        else
        {
            randomOperatorImages[slotIndex].sprite = baitoSmile;
        }

        confirmPopup.SetActive(true);
    }

    // 클릭했던 시설의 운영자의 표정을 기본 상태로 되돌리는 함수
    private void ResetSelectedOperatorFace()
    {
        if (selectedIndex == -1) return; // 선택된 게 없으면 패스

        FacilityData prevData = currentOptions[selectedIndex];
        int prevRank = ExplorationManager.Instance.GetFacilityRank(prevData.facilityID);

        if (prevRank > 0 && prevData.operatorImage != null)
            randomOperatorImages[selectedIndex].sprite = prevData.operatorImage;
        else
            randomOperatorImages[selectedIndex].sprite = baitoNormal;
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

        FacilityData targetData = currentOptions[selectedIndex];

        // 탐색 매니저에게 "이 시설 방문했어!" 라고 알려주기
        ExplorationManager.Instance.lastVisitedFacility = targetData;

        DevLog.Log($"{targetData.facilityID} 씬으로 이동합니다...");

        // TODO: 해당 시설 씬으로 이동하는 로직 (나중에 씬 이름 규칙 정하면 주석 해제)
        // SceneManager.LoadScene(targetData.facilityID + "Scene"); 
    }
}