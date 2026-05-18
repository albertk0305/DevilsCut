using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; 
using TMPro; 

//탐색 씬 UI 제어 코드
public class ExplorationUI : MonoBehaviour
{
    [Header("캐릭터 & 상태 UI")]
    public Sprite playerNormal;       // 기본 표정
    public Sprite playerReady;        // 선택 시 준비 표정
    public Sprite playerWorried;      // 체력 저하 시 걱정 표정
    public Slider hpSlider;           // 캐릭터 머리 위 체력바
    public TextMeshProUGUI karinDialogueText;
    public Sprite karinNormal;       // 카린 기본 표정
    public Sprite karinReady;        // 카린 준비 표정
    public Sprite karinWorried;
    public Image karinImage;

    [Header("좌측 & 하단 (고정 및 단일 슬롯)")]
    public Image playerImage;         // 주인공
    public Image companionImage;      // 동행 조력자
    public Image guideImage;          // 내비 가이드
    public Image lastFacilityImage;   // 마지막 방문 시설

    [Header("우측 (랜덤 시설 3개 슬롯)")]
    // [신규 추가] 명시적으로 끄고 켤 슬롯의 최상위 부모 오브젝트
    public GameObject[] randomSlotRoots;

    [Header("우측 (랜덤 시설 3개 슬롯)")]
    public Image[] randomFacilityImages;
    public Image[] randomOperatorImages;
    public TextMeshProUGUI[] randomRankTexts;

    [Header("기본 운영자 (Baito)")]
    public Sprite baitoNormal; // Baito 기본 표정
    public Sprite baitoSmile;  // Baito 웃는 표정

    [Header("선택 팝업 UI")]
    public GameObject confirmPopup; // 화면 중앙의 예/아니오 팝업창 묶음

    [Header("상단 재화 UI")]
    public TextMeshProUGUI goldText;

    [Header("진척도 및 열쇠 UI")]
    public TextMeshProUGUI keyCountText; // 열쇠 개수 (X0)

    public GameObject explorationProgressParent; // 탐색 진행도 부모 객체
    public GameObject[] explorationProgressIcons; // 7개의 탐색 진행 아이콘 (순서대로 넣으세요)

    public GameObject battleProgressParent; // 전투 진행도 부모 객체
    public GameObject[] battleProgressIcons; // 4개의 전투 진행 아이콘 (순서대로 넣으세요)

    public GameObject statusCanvas;
    public GameObject settingsCanvas;

    private List<ExplorationNodeData> currentOptions = new List<ExplorationNodeData>(); // 현재 선택지들
    private int selectedIndex = -1; // 현재 클릭된 시설의 번호 (0, 1, 2)

    void Start()
    {
        InitializeSceneUI();
    }

    private void OnDestroy()
    {
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged -= UpdateKarinDialogue;
        }
    }

    public void InitializeSceneUI()
    {
        // [추가됨] 언어 변경 이벤트(방송) 구독 시작!
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged += UpdateKarinDialogue;
        }

        // [추가됨] 씬이 시작될 때 메뉴창(StatusCanvas)을 확실하게 꺼둡니다.
        if (statusCanvas != null) statusCanvas.SetActive(false);
        if (settingsCanvas != null) settingsCanvas.SetActive(false);

        // 시작/초기화할 때 팝업을 확실하게 비활성화 (방어 코드)
        if (confirmPopup != null) confirmPopup.SetActive(false);
        selectedIndex = -1; // 선택 상태도 초기화

        // 2. 체력바 설정 및 캐릭터 이미지 업데이트
        UpdateHPBar();
        UpdateCharacterStates();
        UpdateGoldUI();

        // 3. 이전 시설 및 랜덤 노드 설정 로직 (기존과 동일)
        SetupNodes();
    }

    private void SetupNodes()
    {
        FacilityData lastFacility = ExplorationManager.Instance.lastVisitedFacility;
        if (lastFacility != null && lastFacility.nodeImage != null)
        {
            lastFacilityImage.sprite = lastFacility.nodeImage;
            lastFacilityImage.gameObject.SetActive(true);
        }
        else lastFacilityImage.gameObject.SetActive(false);

        // 3개 무작위 뽑기 및 화면 적용
        currentOptions = ExplorationManager.Instance.GetCurrentOptions();

        for (int i = 0; i < 3; i++)
        {
            ExplorationNodeData data = currentOptions[i];

            // 1. 데이터가 null이면 그 슬롯(버튼) 전체를 꺼버립니다. (대칭용)
            if (data == null)
            {
                if (randomSlotRoots != null && randomSlotRoots.Length > i && randomSlotRoots[i] != null)
                {
                    randomSlotRoots[i].SetActive(false);
                }
                continue;
            }

            // 슬롯 활성화
            if (randomSlotRoots != null && randomSlotRoots.Length > i && randomSlotRoots[i] != null)
            {
                randomSlotRoots[i].SetActive(true);
            }

            if (randomFacilityImages[i] != null)
            {
                randomFacilityImages[i].sprite = data.nodeImage;
            }

            // 2. 각 노드 타입별 UI 세팅 분기
            if (data is FacilityData facilityData)
            {
                int currentRank = ExplorationManager.Instance.GetFacilityRank(facilityData.nodeID);
                if (randomRankTexts[i] != null) { randomRankTexts[i].gameObject.SetActive(true); randomRankTexts[i].text = currentRank.ToString(); }
                if (randomOperatorImages[i] != null)
                {
                    randomOperatorImages[i].gameObject.SetActive(true);
                    randomOperatorImages[i].sprite = (currentRank > 0 && facilityData.operatorImage != null) ? facilityData.operatorImage : baitoNormal;
                }
            }
            else if (data is BossSelectionNodeData bossData)
            {
                // 보스 선택창: 랭크 끄고, 운영자 자리에 보스 기본 SD를 띄웁니다!
                if (randomRankTexts[i] != null) randomRankTexts[i].gameObject.SetActive(false);
                if (randomOperatorImages[i] != null)
                {
                    randomOperatorImages[i].gameObject.SetActive(true);
                    randomOperatorImages[i].sprite = bossData.bossData.defaultSD;
                }
            }
            else if (data is PhaseBattleNodeData battleData)
            {
                // 전투: 랭크 끄고, 보스전일 때만 운영자(조력자 위치)에 보스 SD를 띄웁니다!
                if (randomRankTexts[i] != null) randomRankTexts[i].gameObject.SetActive(false);
                if (randomOperatorImages[i] != null)
                {
                    randomOperatorImages[i].gameObject.SetActive(battleData.isBossBattle);
                    if (battleData.isBossBattle) randomOperatorImages[i].sprite = battleData.bossData.defaultSD;
                }
            }
            else // 일반 위험/이벤트 노드
            {
                if (randomRankTexts[i] != null) randomRankTexts[i].gameObject.SetActive(false);
                if (randomOperatorImages[i] != null) randomOperatorImages[i].gameObject.SetActive(false);
            }
        }
        UpdateProgressUI();
    }

    // 시설 선택 UI 버튼에서 OnClick()으로 연결할 함수 (인자값으로 0, 1, 2를 넘겨줄 거에요)
    public void OnClickFacilitySlot(int slotIndex)
    {
        if (slotIndex >= currentOptions.Count || currentOptions[slotIndex] == null) return;

        if (selectedIndex != -1 && selectedIndex != slotIndex) ResetSelectedOperatorFace();

        selectedIndex = slotIndex;
        ExplorationNodeData selectedData = currentOptions[slotIndex];

        // 클릭 시 표정을 찡그리거나/준비 자세로 바꾸는 로직
        if (selectedData is FacilityData facilityData)
        {
            int currentRank = ExplorationManager.Instance.GetFacilityRank(facilityData.nodeID);
            randomOperatorImages[slotIndex].sprite = (currentRank > 0 && facilityData.operatorSmileImage != null) ? facilityData.operatorSmileImage : baitoSmile;
        }
        else if (selectedData is BossSelectionNodeData bossSelData)
        {
            randomOperatorImages[slotIndex].sprite = bossSelData.bossData.readySD; // 보스 선택 시 Ready SD!
        }
        else if (selectedData is PhaseBattleNodeData battleData && battleData.isBossBattle)
        {
            randomOperatorImages[slotIndex].sprite = battleData.bossData.readySD; // 보스전 돌입 전 Ready SD!
        }

        confirmPopup.SetActive(true);
        UpdateCharacterStates();
    }

    // 클릭했던 시설의 운영자의 표정을 기본 상태로 되돌리는 함수
    private void ResetSelectedOperatorFace()
    {
        if (selectedIndex == -1 || currentOptions[selectedIndex] == null) return;

        ExplorationNodeData prevData = currentOptions[selectedIndex];

        if (prevData is FacilityData facilityData)
        {
            int prevRank = ExplorationManager.Instance.GetFacilityRank(facilityData.nodeID);
            randomOperatorImages[selectedIndex].sprite = (prevRank > 0 && facilityData.operatorImage != null) ? facilityData.operatorImage : baitoNormal;
        }
        else if (prevData is BossSelectionNodeData bossSelData)
        {
            randomOperatorImages[selectedIndex].sprite = bossSelData.bossData.defaultSD; // 보스 기본 SD 원상복구
        }
        else if (prevData is PhaseBattleNodeData battleData && battleData.isBossBattle)
        {
            randomOperatorImages[selectedIndex].sprite = battleData.bossData.defaultSD;
        }
    }

    // 팝업에서 'Cancel(취소)' 버튼을 눌렀을 때
    public void OnClickCancel()
    {
        confirmPopup.SetActive(false); // 팝업 닫기
        ResetSelectedOperatorFace();       // 웃는 표정 원상복구
        selectedIndex = -1;
        UpdateCharacterStates();
    }

    private void UpdateCharacterStates()
    {
        if (PlayerManager.Instance == null) return;

        float hpPercent = (float)PlayerManager.Instance.stats.currentHp / PlayerManager.Instance.stats.maxHp;
        bool isLowHP = hpPercent <= 0.3f;
        bool isConfirming = selectedIndex != -1;

        // 1. 이미지 교체 (우선순위: 걱정 > 준비 > 일반)
        if (isLowHP)
        {
            playerImage.sprite = playerWorried;
            // 카린 걱정 이미지가 있다면 띄우고, 안 넣었으면 기본 표정으로 방어!
            karinImage.sprite = karinWorried;
        }
        else if (isConfirming)
        {
            playerImage.sprite = playerReady;
            karinImage.sprite = karinReady;
        }
        else
        {
            playerImage.sprite = playerNormal;
            karinImage.sprite = karinNormal;
        }
        SupporterData activeSupporter = PlayerManager.Instance.activeSupporter;
        if (activeSupporter != null)
        {
            companionImage.gameObject.SetActive(true);
            if (isLowHP) companionImage.sprite = activeSupporter.worriedSDImage;
            else if (isConfirming) companionImage.sprite = activeSupporter.readySDImage;
            else companionImage.sprite = activeSupporter.sdImage;
        }
        else companionImage.gameObject.SetActive(false);

        // 2. [핵심] 카린 대사 업데이트
        UpdateKarinDialogue();
    }

    private void UpdateKarinDialogue()
    {
        if (selectedIndex == -1)
        {
            // 아무것도 선택하지 않았을 때 대사
            karinDialogueText.text = LocalizationManager.Instance.GetText("msg_karin_exploration_idle");
        }
        else
        {
            ExplorationNodeData data = currentOptions[selectedIndex];

            if (data is FacilityData facility)
            {
                int rank = ExplorationManager.Instance.GetFacilityRank(facility.nodeID);

                if (rank > 0)
                {
                    // 운영자가 해금된 경우: "그 시설은 {0}가 운영 중이고 시설 랭크는 {1}네요."
                    string fmt = LocalizationManager.Instance.GetText("msg_facility_info_format");
                    string opName = LocalizationManager.Instance.GetText(facility.operatorName); // 운영자 이름 Key 번역
                    karinDialogueText.text = string.Format(fmt, opName, rank);
                }
                else
                {
                    // 운영자가 해금되지 않은 경우
                    karinDialogueText.text = LocalizationManager.Instance.GetText("msg_operator_not_unlocked");
                }
            }
            else if (data is BossSelectionNodeData)
            {
                // 예: "이 녀석을 다음 타겟으로 정한 거군요."
                karinDialogueText.text = LocalizationManager.Instance.GetText("msg_boss_selected");
            }
            else if (data is PhaseBattleNodeData pBattle)
            {
                if (pBattle.isBossBattle)
                {
                    // 예: "드디어 보스전이에요. 준비 단단히 하세요!"
                    karinDialogueText.text = LocalizationManager.Instance.GetText("msg_boss_battle_ready");
                }
                else
                {
                    // 예: "전투가 곧 시작돼요. 조심하세요."
                    karinDialogueText.text = LocalizationManager.Instance.GetText("msg_general_battle_ready");
                }
            }
        }
    }

    // 팝업에서 'Confirm(확인)' 버튼을 눌렀을 때
    public void OnClickConfirm()
    {
        if (selectedIndex == -1) return;
        ExplorationNodeData targetData = currentOptions[selectedIndex];
        confirmPopup.SetActive(false);

        if (targetData is BossSelectionNodeData bossSelect)
        {
            // 보스 픽! 턴을 진행하고 다시 화면을 새로고침합니다.
            ExplorationManager.Instance.SelectTargetBoss(bossSelect.bossData);
            selectedIndex = -1;
            SetupNodes();
            UpdateCharacterStates();
            return; // 씬 전환 안 함
        }
        else if (targetData is FacilityData facility)
        {
            ExplorationManager.Instance.lastVisitedFacility = facility;
            ExplorationManager.Instance.AdvanceExplorationTurn(); // 시설 탐색 1턴 소모!
            // SceneManager.LoadScene(facility.nodeID + "Scene"); 
        }
        else if (targetData is PhaseBattleNodeData pBattle)
        {
            // 전투 우체통에 적군 배달
            PlayerManager.Instance.currentEnemyToFight = pBattle.enemyToSpawn;

            // [중요!] 나중에 CombatManager에서 전투 승리 후 아래 코드를 호출해 주어야 다음 턴/페이즈로 넘어갑니다.
            // ExplorationManager.Instance.AdvanceBattleTurn(pBattle.isBossBattle);

            UnityEngine.SceneManagement.SceneManager.LoadScene("CombatScene");
        }
    }

    // 체력바 업데이트 함수
    private void UpdateHPBar()
    {
        if (hpSlider != null && PlayerManager.Instance != null)
        {
            float currentHp = PlayerManager.Instance.stats.currentHp;
            float maxHp = PlayerManager.Instance.stats.maxHp;

            // 슬라이더의 가치를 0~1 사이로 맞춤
            hpSlider.value = currentHp / maxHp;
        }
    }

    public void RefreshUI()
    {
        UpdateHPBar();           // 장비 교체로 체력이 변경되었을 수 있으니 갱신!
        UpdateCharacterStates(); // 서포터 교체가 있었을 수 있으니 이미지 갱신!
        UpdateGoldUI();
    }
    private void UpdateGoldUI()
    {
        // 텍스트 컴포넌트가 연결되어 있고 PlayerManager가 살아있다면
        if (goldText != null && PlayerManager.Instance != null)
        {
            // "N0" 포맷은 1000 -> 1,000 처럼 천 단위 콤마를 찍어줍니다.
            goldText.text = PlayerManager.Instance.stats.currentGold.ToString("N0");
        }
    }

    private void UpdateProgressUI()
    {
        if (ExplorationManager.Instance == null) return;

        // 1. 열쇠 텍스트 업데이트
        if (keyCountText != null)
            keyCountText.text = $"X{ExplorationManager.Instance.currentKeys}";

        GamePhase phase = ExplorationManager.Instance.currentPhase;
        int turn = ExplorationManager.Instance.currentTurnInPhase;

        // 2. 탐색 페이즈 (보스 선택 ~ 6턴 시설 이용)
        if (phase == GamePhase.BossSelection || phase == GamePhase.Exploration)
        {
            if (explorationProgressParent != null) explorationProgressParent.SetActive(true);
            if (battleProgressParent != null) battleProgressParent.SetActive(false); // 탐색 중엔 전투 진행도 숨김

            int activeCount = (phase == GamePhase.BossSelection) ? 1 : (2 + turn);

            if (explorationProgressIcons != null)
            {
                for (int i = 0; i < explorationProgressIcons.Length; i++)
                {
                    if (explorationProgressIcons[i] != null)
                        explorationProgressIcons[i].SetActive(i < activeCount);
                }
            }
        }
        // 3. 전투 페이즈 (일반 전투 3번 ~ 보스전)
        else if (phase == GamePhase.GeneralBattle || phase == GamePhase.BossBattle)
        {
            //  [수정] 전투 중에도 탐색 UI 부모를 끄지 않고 유지합니다!
            if (explorationProgressParent != null) explorationProgressParent.SetActive(true);
            if (battleProgressParent != null) battleProgressParent.SetActive(true);

            //  [추가] 탐색 UI의 7개 아이콘은 꽉 채워진(모두 true) 상태로 둡니다.
            if (explorationProgressIcons != null)
            {
                for (int i = 0; i < explorationProgressIcons.Length; i++)
                {
                    if (explorationProgressIcons[i] != null)
                        explorationProgressIcons[i].SetActive(true);
                }
            }

            // 일반 전투 중이면 (턴 수 + 1)개 점등, 보스전이면 4개 모두 점등
            // 예: 첫 번째 일반전투(Turn 0) -> 1개 / 마지막 일반전투(Turn 2) -> 3개
            int activeCount = (phase == GamePhase.BossBattle) ? 4 : (1 + turn);

            if (battleProgressIcons != null)
            {
                for (int i = 0; i < battleProgressIcons.Length; i++)
                {
                    if (battleProgressIcons[i] != null)
                        battleProgressIcons[i].SetActive(i < activeCount);
                }
            }
        }
    }
}