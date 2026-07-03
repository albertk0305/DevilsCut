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
    private ClearRecordPlayerProfile previewProfile;

    private void OnEnable()
    {
        if (previewProfile != null)
        {
            RefreshPreview();
            SubscribeLanguageChanged();
            return;
        }

        ShowPreview(PlayerManager.Instance.activeSupporter, isJoinedState: true);
        RefreshRosterList();

        SubscribeLanguageChanged();
    }

    private void OnDisable()
    {
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged -= RefreshLanguage;
    }

    private void RefreshLanguage()
    {
        if (previewProfile != null)
        {
            bool isPreviewJoined = currentPreview != null && previewProfile.IsActiveSupporter(currentPreview.supporterID);
            ShowPreview(currentPreview, isPreviewJoined);
            return;
        }

        bool isJoined = (currentPreview != null && PlayerManager.Instance != null && currentPreview == PlayerManager.Instance.activeSupporter);
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

            bool canChangeParty = previewProfile != null || isExploration;
            joinButton.interactable = !isJoinedState && canChangeParty;
            leaveButton.interactable = isJoinedState && canChangeParty;
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
        if (previewProfile != null)
        {
            RefreshPreviewRosterList();
            return;
        }

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

        if (previewProfile != null)
        {
            JoinPreviewSupporter();
            return;
        }

        PlayerManager.Instance.activeSupporter = currentPreview;

        ShowPreview(currentPreview, isJoinedState: true);

        RefreshRosterList();
    }

    public void OnClickLeave()
    {
        if (previewProfile != null)
        {
            LeavePreviewSupporter();
            return;
        }

        PlayerManager.Instance.activeSupporter = null;
        ShowPreview(null, isJoinedState: false);
        RefreshRosterList();
    }

    public void OnClickCancel()
    {
        if (previewProfile != null)
        {
            ShowPreview(previewProfile.GetActiveSupporter(), isJoinedState: true);
            return;
        }

        ShowPreview(PlayerManager.Instance.activeSupporter, isJoinedState: true);
    }

    private int GetTotalPages()
    {
        return Mathf.Max(1, Mathf.CeilToInt((float)displayList.Count / rosterButtons.Length));
    }

    public void SetPreviewProfile(ClearRecordPlayerProfile profile)
    {
        previewProfile = profile;
        currentPage = 0;

        if (isActiveAndEnabled)
            RefreshPreview();
    }

    public void ClearPreviewProfile()
    {
        previewProfile = null;
    }

    private void RefreshPreview()
    {
        if (previewProfile == null)
            return;

        ShowPreview(previewProfile.GetActiveSupporter(), isJoinedState: true);
        RefreshPreviewRosterList();
    }

    private void RefreshPreviewRosterList()
    {
        SupporterData activeSupporter = previewProfile.GetActiveSupporter();
        displayList = previewProfile.UnlockedSupporters
            .Where(s => s != null && (activeSupporter == null || s.supporterID != activeSupporter.supporterID))
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
                rosterButtons[i].image.sprite = displayList[dataIndex].iconImage;

            if (rosterBackgrounds.Length > i && rosterBackgrounds[i] != null)
                rosterBackgrounds[i].SetActive(hasData);
        }

        bool hasMultiplePages = totalPages > 1;
        leftArrow.SetActive(hasMultiplePages);
        rightArrow.SetActive(hasMultiplePages);
    }

    private void JoinPreviewSupporter()
    {
        string previousActiveId = previewProfile.GetActiveSupporter() != null
            ? previewProfile.GetActiveSupporter().supporterID
            : null;

        if (!previewProfile.SetActiveSupporter(currentPreview.supporterID))
        {
            DevLog.LogWarning($"[MainMenu] ClearRecord supporter join failed: supporterID={currentPreview.supporterID}");
            RefreshPreview();
            return;
        }

        if (!SavePreviewRecord())
        {
            RestorePreviewActiveSupporter(previousActiveId);
            RefreshPreview();
            return;
        }

        ShowPreview(currentPreview, isJoinedState: true);
        RefreshPreviewRosterList();
    }

    private void LeavePreviewSupporter()
    {
        string previousActiveId = previewProfile.GetActiveSupporter() != null
            ? previewProfile.GetActiveSupporter().supporterID
            : null;

        if (!previewProfile.ClearActiveSupporter())
        {
            RefreshPreview();
            return;
        }

        if (!SavePreviewRecord())
        {
            RestorePreviewActiveSupporter(previousActiveId);
            RefreshPreview();
            return;
        }

        ShowPreview(null, isJoinedState: false);
        RefreshPreviewRosterList();
    }

    private bool SavePreviewRecord()
    {
        if (SaveManager.Instance == null || previewProfile == null || previewProfile.Record == null)
        {
            DevLog.LogWarning("[MainMenu] ClearRecord supporter preview save failed: SaveManager or record missing.");
            return false;
        }

        bool saved = SaveManager.Instance.UpdateGameClearRecord(previewProfile.Record);
        if (!saved)
            DevLog.LogWarning($"[MainMenu] ClearRecord supporter preview save failed: clearId={previewProfile.ClearId}");

        return saved;
    }

    private void RestorePreviewActiveSupporter(string supporterId)
    {
        if (string.IsNullOrEmpty(supporterId))
            previewProfile.ClearActiveSupporter();
        else
            previewProfile.SetActiveSupporter(supporterId);
    }

    private void SubscribeLanguageChanged()
    {
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged += RefreshLanguage;
    }
}
