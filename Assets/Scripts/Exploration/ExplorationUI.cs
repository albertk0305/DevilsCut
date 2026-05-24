using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class ExplorationUI : MonoBehaviour
{
    [Header("캐릭터 & 상태 UI")]
    public Sprite playerNormal;
    public Sprite playerReady;
    public Sprite playerWorried;
    public Slider hpSlider;
    public TextMeshProUGUI karinDialogueText;
    public Sprite karinNormal;
    public Sprite karinReady;
    public Sprite karinWorried;
    public Image karinImage;

    [Header("좌측 & 하단 (고정 및 단일 슬롯)")]
    public Image playerImage;
    public Image companionImage;
    public Image guideImage;
    public Image lastFacilityImage;

    [Header("우측 (랜덤 시설 3개 슬롯)")]
    public GameObject[] randomSlotRoots;

    [Header("우측 (랜덤 시설 3개 슬롯)")]
    public Image[] randomFacilityImages;
    public Image[] randomOperatorImages;
    public TextMeshProUGUI[] randomRankTexts;

    [Header("기본 운영자 (Baito)")]
    public Sprite baitoNormal;
    public Sprite baitoSmile;

    [Header("선택 팝업 UI")]
    public GameObject confirmPopup;

    [Header("상단 재화 UI")]
    public TextMeshProUGUI goldText;

    [Header("진척도 및 열쇠 UI")]
    public TextMeshProUGUI keyCountText;

    public GameObject explorationProgressParent;
    public GameObject[] explorationProgressIcons;

    public GameObject battleProgressParent;
    public GameObject[] battleProgressIcons;

    public GameObject statusCanvas;
    public GameObject settingsCanvas;

    private List<ExplorationNodeData> currentOptions = new List<ExplorationNodeData>();
    private int selectedIndex = -1;

    [SerializeField] private CombatEncounterBuilder encounterBuilder;

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
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged += UpdateKarinDialogue;
        }

        if (statusCanvas != null) statusCanvas.SetActive(false);
        if (settingsCanvas != null) settingsCanvas.SetActive(false);

        if (confirmPopup != null) confirmPopup.SetActive(false);
        selectedIndex = -1;

        UpdateHPBar();
        UpdateCharacterStates();
        UpdateGoldUI();

        SetupNodes();
    }

    private void SetupNodes()
    {
        Sprite lastNodeImage = ExplorationManager.Instance.lastVisitedNodeImage;

        if (lastNodeImage != null)
        {
            lastFacilityImage.sprite = lastNodeImage;
            lastFacilityImage.gameObject.SetActive(true);
        }
        else
        {
            lastFacilityImage.gameObject.SetActive(false);
        }

        currentOptions = new List<ExplorationNodeData>(ExplorationManager.Instance.CurrentOptions);

        for (int i = 0; i < 3; i++)
        {
            ExplorationNodeData data = currentOptions[i];

            if (data == null)
            {
                if (randomSlotRoots != null && randomSlotRoots.Length > i && randomSlotRoots[i] != null)
                {
                    randomSlotRoots[i].SetActive(false);
                }
                continue;
            }

            if (randomSlotRoots != null && randomSlotRoots.Length > i && randomSlotRoots[i] != null)
            {
                randomSlotRoots[i].SetActive(true);
            }

            if (randomFacilityImages[i] != null)
            {
                randomFacilityImages[i].sprite = data.nodeImage;
            }

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
                if (randomRankTexts[i] != null) randomRankTexts[i].gameObject.SetActive(false);
                if (randomOperatorImages[i] != null)
                {
                    randomOperatorImages[i].gameObject.SetActive(true);
                    randomOperatorImages[i].sprite = bossData.bossData.defaultSD;
                }
            }
            else if (data is PhaseBattleNodeData battleData)
            {
                if (randomRankTexts[i] != null) randomRankTexts[i].gameObject.SetActive(false);
                if (randomOperatorImages[i] != null)
                {
                    randomOperatorImages[i].gameObject.SetActive(battleData.isBossBattle);
                    if (battleData.isBossBattle) randomOperatorImages[i].sprite = battleData.bossData.defaultSD;
                }
            }
            else
            {
                if (randomRankTexts[i] != null) randomRankTexts[i].gameObject.SetActive(false);
                if (randomOperatorImages[i] != null) randomOperatorImages[i].gameObject.SetActive(false);
            }
        }
        UpdateProgressUI();
    }

    public void OnClickFacilitySlot(int slotIndex)
    {
        if (slotIndex >= currentOptions.Count || currentOptions[slotIndex] == null) return;

        if (selectedIndex != -1 && selectedIndex != slotIndex) ResetSelectedOperatorFace();

        selectedIndex = slotIndex;
        ExplorationNodeData selectedData = currentOptions[slotIndex];

        if (selectedData is FacilityData facilityData)
        {
            int currentRank = ExplorationManager.Instance.GetFacilityRank(facilityData.nodeID);
            randomOperatorImages[slotIndex].sprite = (currentRank > 0 && facilityData.operatorSmileImage != null) ? facilityData.operatorSmileImage : baitoSmile;
        }
        else if (selectedData is BossSelectionNodeData bossSelData)
        {
            randomOperatorImages[slotIndex].sprite = bossSelData.bossData.readySD;
        }
        else if (selectedData is PhaseBattleNodeData battleData && battleData.isBossBattle)
        {
            randomOperatorImages[slotIndex].sprite = battleData.bossData.readySD;
        }

        confirmPopup.SetActive(true);
        UpdateCharacterStates();
    }

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
            randomOperatorImages[selectedIndex].sprite = bossSelData.bossData.defaultSD;
        }
        else if (prevData is PhaseBattleNodeData battleData && battleData.isBossBattle)
        {
            randomOperatorImages[selectedIndex].sprite = battleData.bossData.defaultSD;
        }
    }

    public void OnClickCancel()
    {
        confirmPopup.SetActive(false);
        ResetSelectedOperatorFace();
        selectedIndex = -1;
        UpdateCharacterStates();
    }

    private void UpdateCharacterStates()
    {
        if (PlayerManager.Instance == null) return;

        float hpPercent = (float)PlayerManager.Instance.stats.currentHp / PlayerManager.Instance.stats.maxHp;
        bool isLowHP = hpPercent <= 0.3f;
        bool isConfirming = selectedIndex != -1;

        if (isLowHP)
        {
            playerImage.sprite = playerWorried;
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

        UpdateKarinDialogue();
    }

    private void UpdateKarinDialogue()
    {
        if (selectedIndex == -1)
        {
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
                    string fmt = LocalizationManager.Instance.GetText("msg_facility_info_format");
                    string opName = LocalizationManager.Instance.GetText(facility.operatorName);
                    karinDialogueText.text = string.Format(fmt, opName, rank);
                }
                else
                {
                    karinDialogueText.text = LocalizationManager.Instance.GetText("msg_operator_not_unlocked");
                }
            }
            else if (data is BossSelectionNodeData)
            {
                karinDialogueText.text = LocalizationManager.Instance.GetText("msg_boss_selected");
            }
            else if (data is PhaseBattleNodeData pBattle)
            {
                if (pBattle.isBossBattle)
                {
                    karinDialogueText.text = LocalizationManager.Instance.GetText("msg_boss_battle_ready");
                }
                else
                {
                    karinDialogueText.text = LocalizationManager.Instance.GetText("msg_general_battle_ready");
                }
            }
        }
    }

    public void OnClickConfirm()
    {
        if (selectedIndex == -1) return;
        ExplorationNodeData targetData = currentOptions[selectedIndex];
        confirmPopup.SetActive(false);

        if (targetData is BossSelectionNodeData bossSelect)
        {
            ExplorationManager.Instance.SelectTargetBoss(bossSelect.bossData);
            ExplorationManager.Instance.lastVisitedNodeImage = bossSelect.nodeImage;
            ExplorationManager.Instance.SaveStateToPlayerManager();
            selectedIndex = -1;
            SetupNodes();
            UpdateCharacterStates();
            return;
        }
        else if (targetData is FacilityData facility)
        {
            ExplorationManager.Instance.lastVisitedFacility = facility;
            ExplorationManager.Instance.lastVisitedNodeImage = facility.nodeImage;
            ExplorationManager.Instance.AdvanceExplorationTurn();
            selectedIndex = -1;
            SetupNodes();
            UpdateCharacterStates();
            // SceneManager.LoadScene(facility.nodeID + "Scene"); 
        }
        else if (targetData is PhaseBattleNodeData pBattle)
        {
            BattleType battleType = pBattle.isBossBattle ? BattleType.Boss : BattleType.General;
            int phase = ExplorationManager.Instance.currentCycle;

            if (encounterBuilder == null)
            {
                Debug.LogError("ExplorationUI: CombatEncounterBuilder가 연결되지 않았습니다.");
                return;
            }

            bool prepared = encounterBuilder.PrepareEncounter(pBattle.enemyToSpawn, battleType, phase);
            if (!prepared)
                return;
            ExplorationManager.Instance.lastVisitedNodeImage = pBattle.nodeImage;
            ExplorationManager.Instance.SaveStateToPlayerManager();
            SceneManager.LoadScene("Battle");
        }
    }

    private void UpdateHPBar()
    {
        if (hpSlider != null && PlayerManager.Instance != null)
        {
            float currentHp = PlayerManager.Instance.stats.currentHp;
            float maxHp = PlayerManager.Instance.stats.maxHp;

            hpSlider.value = currentHp / maxHp;
        }
    }

    public void RefreshAfterContinueLoad()
    {
        selectedIndex = -1;
        if (confirmPopup != null) confirmPopup.SetActive(false);
        UpdateHPBar();
        UpdateGoldUI();
        SetupNodes();
        UpdateCharacterStates();
    }
    public void RefreshUI()
    {
        UpdateHPBar();
        UpdateCharacterStates();
        UpdateGoldUI();
    }
    private void UpdateGoldUI()
    {
        if (goldText != null && PlayerManager.Instance != null)
        {
            goldText.text = PlayerManager.Instance.stats.currentGold.ToString("N0");
        }
    }

    private void UpdateProgressUI()
    {
        if (ExplorationManager.Instance == null) return;

        if (keyCountText != null)
            keyCountText.text = $"X{ExplorationManager.Instance.currentKeys}";

        GamePhase phase = ExplorationManager.Instance.currentPhase;
        int turn = ExplorationManager.Instance.currentTurnInPhase;

        if (phase == GamePhase.BossSelection || phase == GamePhase.Exploration)
        {
            if (explorationProgressParent != null) explorationProgressParent.SetActive(true);
            if (battleProgressParent != null) battleProgressParent.SetActive(false);

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
        else if (phase == GamePhase.GeneralBattle || phase == GamePhase.BossBattle)
        {
            if (explorationProgressParent != null) explorationProgressParent.SetActive(true);
            if (battleProgressParent != null) battleProgressParent.SetActive(true);

            // Keep exploration progress complete while battle progress is shown.
            if (explorationProgressIcons != null)
            {
                for (int i = 0; i < explorationProgressIcons.Length; i++)
                {
                    if (explorationProgressIcons[i] != null)
                        explorationProgressIcons[i].SetActive(true);
                }
            }

            // General battles light turn+1 icons; boss battles light all icons.
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
