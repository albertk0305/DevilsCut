using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class SupporterUI : MonoBehaviour
{
    [Header("메인 디스플레이")]
    public Image mainImage;
    public TextMeshProUGUI passiveText;
    public TextMeshProUGUI passiveLevelText;
    public TextMeshProUGUI startText;
    public TextMeshProUGUI startSkillLevelText;
    public TextMeshProUGUI battleText;
    public TextMeshProUGUI battleSkillLevelText;
    public TextMeshProUGUI dialogueText;
    public TextMeshProUGUI supporterNameText;

    [Header("하단 대기열 목록")]
    public Button[] rosterButtons;
    public GameObject[] rosterBackgrounds;
    public GameObject leftArrow;
    public GameObject rightArrow;

    [Header("버튼")]
    public Button joinButton;
    public Button leaveButton;
    public Button cancelButton;

    private SupporterData currentPreview;
    private List<SupporterData> displayList = new List<SupporterData>();
    private int currentPage = 0;

    private void OnEnable()
    {
        ShowPreview(PlayerManager.Instance.activeSupporter, isJoinedState: true);
        RefreshRosterList();

        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged += RefreshLanguage;
    }

    private void OnDisable()
    {
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged -= RefreshLanguage;
    }

    private void RefreshLanguage()
    {
        bool isJoined = (currentPreview != null && currentPreview == PlayerManager.Instance.activeSupporter);
        ShowPreview(currentPreview, isJoined);
    }


    private void ShowPreview(SupporterData data, bool isJoinedState)
    {
        currentPreview = data;

        bool isExploration = ExplorationManager.Instance != null;

        if (data == null)
        {
            mainImage.gameObject.SetActive(false);

            supporterNameText.text = "";
            passiveText.text = "";
            startText.text = "";
            battleText.text = "";
            SetSkillLevelTexts(null);
            dialogueText.text = LocalizationManager.Instance.GetText("msg_no_active_supporter");

            joinButton.interactable = false;
            leaveButton.interactable = false;
            if (cancelButton != null) cancelButton.gameObject.SetActive(false);
        }
        else
        {
            mainImage.gameObject.SetActive(true);

            supporterNameText.text = LocalizationManager.Instance.GetText(data.supporterName);
            mainImage.sprite = data.mainImage;
            passiveText.text = LocalizationManager.Instance.GetText(data.passiveSkillDesc);
            startText.text = LocalizationManager.Instance.GetText(data.startSkillDesc);
            battleText.text = LocalizationManager.Instance.GetText(data.battleSkillDesc);
            SetSkillLevelTexts(data);

            string dialogueKey = isJoinedState ? data.joinMessage : data.selectMessage;
            dialogueText.text = LocalizationManager.Instance.GetText(dialogueKey);

            joinButton.interactable = !isJoinedState && isExploration;
            leaveButton.interactable = isJoinedState && isExploration;
            if (cancelButton != null) cancelButton.gameObject.SetActive(!isJoinedState);
        }
    }

    private void SetSkillLevelTexts(SupporterData data)
    {
        if (data == null)
        {
            if (passiveLevelText != null) passiveLevelText.text = "";
            if (startSkillLevelText != null) startSkillLevelText.text = "";
            if (battleSkillLevelText != null) battleSkillLevelText.text = "";
            return;
        }

        if (passiveLevelText != null) passiveLevelText.text = FormatSkillLevel(data.passiveLevel);
        if (startSkillLevelText != null) startSkillLevelText.text = FormatSkillLevel(data.startSkillLevel);
        if (battleSkillLevelText != null) battleSkillLevelText.text = FormatSkillLevel(data.battleSkillLevel);
    }

    private string FormatSkillLevel(int level)
    {
        return $"Lv.{Mathf.Max(1, level)}";
    }

    private void RefreshRosterList()
    {
        displayList = PlayerManager.Instance.unlockedSupporters
            .Where(s => s != PlayerManager.Instance.activeSupporter)
            .ToList();

        int totalPages = GetTotalPages();
        if (currentPage >= totalPages && currentPage > 0) currentPage = totalPages - 1;

        int startIndex = currentPage * rosterButtons.Length;

        for (int i = 0; i < rosterButtons.Length; i++)
        {
            int dataIndex = startIndex + i;

            bool hasData = dataIndex < displayList.Count;

            rosterButtons[i].gameObject.SetActive(hasData);
            rosterButtons[i].interactable = hasData;

            if (hasData)
            {
                rosterButtons[i].image.sprite = displayList[dataIndex].iconImage;
            }

            if (rosterBackgrounds.Length > i && rosterBackgrounds[i] != null)
            {
                rosterBackgrounds[i].SetActive(hasData);
            }
        }

        bool hasMultiplePages = totalPages > 1;
        leftArrow.SetActive(hasMultiplePages);
        rightArrow.SetActive(hasMultiplePages);
    }

    public void OnClickRosterIcon(int slotIndex)
    {
        int dataIndex = (currentPage * rosterButtons.Length) + slotIndex;
        if (dataIndex < displayList.Count)
        {
            ShowPreview(displayList[dataIndex], isJoinedState: false);
        }
    }

    public void OnClickLeftArrow()
    {
        currentPage--;
        int totalPages = GetTotalPages();
        if (currentPage < 0) currentPage = totalPages - 1;
        RefreshRosterList();
    }

    public void OnClickRightArrow()
    {
        currentPage++;
        int totalPages = GetTotalPages();
        if (currentPage >= totalPages) currentPage = 0;
        RefreshRosterList();
    }

    public void OnClickJoin()
    {
        if (currentPreview == null) return;

        PlayerManager.Instance.activeSupporter = currentPreview;

        ShowPreview(currentPreview, isJoinedState: true);

        RefreshRosterList();
    }

    public void OnClickLeave()
    {
        PlayerManager.Instance.activeSupporter = null;
        ShowPreview(null, isJoinedState: false);
        RefreshRosterList();
    }

    public void OnClickCancel()
    {
        ShowPreview(PlayerManager.Instance.activeSupporter, isJoinedState: true);
    }

    private int GetTotalPages()
    {
        return Mathf.Max(1, Mathf.CeilToInt((float)displayList.Count / rosterButtons.Length));
    }
}
