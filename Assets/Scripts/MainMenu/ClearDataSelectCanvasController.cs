using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ClearDataSelectCanvasController : MonoBehaviour
{
    private const int PageSize = 10;

    [SerializeField] private GameObject rootCanvas;
    [SerializeField] private List<ClearDataSlotUI> slotUIs = new List<ClearDataSlotUI>();
    [SerializeField] private Button upButton;
    [SerializeField] private Button downButton;
    [SerializeField] private Button startButton;
    [SerializeField] private Button deleteButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private GameObject confirmationPanel;
    [SerializeField] private Button confirmYesButton;
    [SerializeField] private Button confirmNoButton;
    [SerializeField] private SkillClassTabManager skillPreviewCanvas;
    [SerializeField] private SkillDatabase skillDatabase;
    [SerializeField] private ItemDatabase itemDatabase;

    private readonly List<ClearRecordSummary> records = new List<ClearRecordSummary>();
    private int pageIndex;
    private string selectedClearId;
    private int selectedClearNumber;
    private bool isDeleting;

    private void Awake()
    {
        if (upButton != null)
        {
            upButton.onClick.RemoveListener(OnUpClicked);
            upButton.onClick.AddListener(OnUpClicked);
        }

        if (downButton != null)
        {
            downButton.onClick.RemoveListener(OnDownClicked);
            downButton.onClick.AddListener(OnDownClicked);
        }

        if (startButton != null)
        {
            startButton.onClick.RemoveListener(OnStartClicked);
            startButton.onClick.AddListener(OnStartClicked);
        }

        if (deleteButton != null)
        {
            deleteButton.onClick.RemoveListener(OnDeleteClicked);
            deleteButton.onClick.AddListener(OnDeleteClicked);
        }

        if (exitButton != null)
        {
            exitButton.onClick.RemoveListener(Hide);
            exitButton.onClick.AddListener(Hide);
        }

        if (confirmYesButton != null)
        {
            confirmYesButton.onClick.RemoveListener(OnConfirmDeleteYes);
            confirmYesButton.onClick.AddListener(OnConfirmDeleteYes);
        }

        if (confirmNoButton != null)
        {
            confirmNoButton.onClick.RemoveListener(OnConfirmDeleteNo);
            confirmNoButton.onClick.AddListener(OnConfirmDeleteNo);
        }
    }

    private void OnDestroy()
    {
        if (upButton != null)
            upButton.onClick.RemoveListener(OnUpClicked);

        if (downButton != null)
            downButton.onClick.RemoveListener(OnDownClicked);

        if (startButton != null)
            startButton.onClick.RemoveListener(OnStartClicked);

        if (deleteButton != null)
            deleteButton.onClick.RemoveListener(OnDeleteClicked);

        if (exitButton != null)
            exitButton.onClick.RemoveListener(Hide);

        if (confirmYesButton != null)
            confirmYesButton.onClick.RemoveListener(OnConfirmDeleteYes);

        if (confirmNoButton != null)
            confirmNoButton.onClick.RemoveListener(OnConfirmDeleteNo);
    }

    public void Show()
    {
        Root.SetActive(true);
        pageIndex = 0;
        HideConfirmation();
        ClearSelection();
        Refresh();
    }

    public void Hide()
    {
        HideConfirmation();
        ClearSelection();
        RefreshSlots();
        Root.SetActive(false);
    }

    public void Refresh()
    {
        LoadRecords();
        ClampPageIndex();
        RefreshSlots();
        RefreshButtons();
    }

    private GameObject Root => rootCanvas != null ? rootCanvas : gameObject;

    private void LoadRecords()
    {
        records.Clear();
        if (SaveManager.Instance == null)
            return;

        List<ClearRecordSummary> loadedRecords = SaveManager.Instance.LoadClearRecordSummaries();
        if (loadedRecords != null)
            records.AddRange(loadedRecords);

        records.Sort((a, b) =>
        {
            int aNumber = a != null ? a.clearNumber : int.MaxValue;
            int bNumber = b != null ? b.clearNumber : int.MaxValue;
            return aNumber.CompareTo(bNumber);
        });
    }

    private void RefreshSlots()
    {
        for (int i = 0; i < slotUIs.Count; i++)
        {
            ClearDataSlotUI slot = slotUIs[i];
            if (slot == null)
                continue;

            if (i >= PageSize)
            {
                slot.Clear();
                continue;
            }

            int recordIndex = pageIndex * PageSize + i;
            if (recordIndex < records.Count)
            {
                ClearRecordSummary summary = records[recordIndex];
                bool selected = summary != null && summary.clearId == selectedClearId;
                slot.Bind(summary, selected, OnSlotUseClicked, OnSlotSkillPreviewClicked);
            }
            else
            {
                slot.Clear();
            }
        }
    }

    private void RefreshButtons()
    {
        bool hasSelection = !string.IsNullOrEmpty(selectedClearId);
        bool hasPreviousPage = pageIndex > 0;
        bool hasNextPage = (pageIndex + 1) * PageSize < records.Count;

        if (upButton != null)
            upButton.interactable = hasPreviousPage && !isDeleting;

        if (downButton != null)
            downButton.interactable = hasNextPage && !isDeleting;

        if (startButton != null)
            startButton.interactable = hasSelection && !isDeleting;

        if (deleteButton != null)
            deleteButton.interactable = hasSelection && !isDeleting;
    }

    private void OnSlotUseClicked(ClearRecordSummary summary)
    {
        if (summary == null || string.IsNullOrEmpty(summary.clearId) || isDeleting)
            return;

        selectedClearId = summary.clearId;
        selectedClearNumber = summary.clearNumber;
        HideConfirmation();
        RefreshSlots();
        RefreshButtons();
    }

    private void OnSlotSkillPreviewClicked(ClearRecordSummary summary)
    {
        if (summary == null || string.IsNullOrEmpty(summary.clearId) || isDeleting)
            return;

        if (SaveManager.Instance == null)
        {
            DevLog.LogWarning("[MainMenu] Clear data skill preview failed: SaveManager missing.");
            return;
        }

        GameClearRecordData record = SaveManager.Instance.LoadGameClearRecord(summary.clearId);
        if (record == null)
        {
            DevLog.LogWarning($"[MainMenu] Clear data skill preview failed: record not found. clearId={summary.clearId}");
            return;
        }

        SkillDatabase resolvedSkillDatabase = ResolveSkillDatabase();
        if (resolvedSkillDatabase == null)
        {
            DevLog.LogWarning("[MainMenu] Clear data skill preview failed: SkillDatabase missing.");
            return;
        }

        ItemDatabase resolvedItemDatabase = ResolveItemDatabase();
        if (resolvedItemDatabase == null)
            DevLog.LogWarning("[MainMenu] Clear data skill preview opened without ItemDatabase; class synergy preview will be empty.");

        if (skillPreviewCanvas == null)
        {
            DevLog.LogWarning("[MainMenu] Clear data skill preview failed: SkillCanvas controller is not assigned.");
            return;
        }

        HideConfirmation();
        ClearRecordPlayerProfile profile = new ClearRecordPlayerProfile(record, resolvedSkillDatabase, resolvedItemDatabase);
        skillPreviewCanvas.OpenPreview(profile);
    }

    private void OnUpClicked()
    {
        if (pageIndex <= 0 || isDeleting)
            return;

        pageIndex--;
        ClearSelection();
        HideConfirmation();
        RefreshSlots();
        RefreshButtons();
    }

    private void OnDownClicked()
    {
        if ((pageIndex + 1) * PageSize >= records.Count || isDeleting)
            return;

        pageIndex++;
        ClearSelection();
        HideConfirmation();
        RefreshSlots();
        RefreshButtons();
    }

    private void OnStartClicked()
    {
        if (string.IsNullOrEmpty(selectedClearId) || isDeleting)
            return;

        DevLog.Log($"[MainMenu] Infinite Battle start is not implemented yet. clearId={selectedClearId}, clearNumber={selectedClearNumber}");
    }

    private void OnDeleteClicked()
    {
        if (string.IsNullOrEmpty(selectedClearId) || isDeleting)
            return;

        if (confirmationPanel != null)
            confirmationPanel.SetActive(true);
    }

    private void OnConfirmDeleteYes()
    {
        if (string.IsNullOrEmpty(selectedClearId) || isDeleting)
            return;

        isDeleting = true;
        RefreshButtons();

        bool deleted = SaveManager.Instance != null && SaveManager.Instance.DeleteGameClearRecord(selectedClearId);
        if (!deleted)
            DevLog.LogWarning($"[MainMenu] Clear data delete failed: clearId={selectedClearId}");

        isDeleting = false;
        HideConfirmation();
        ClearSelection();
        LoadRecords();
        ClampPageIndex();
        RefreshSlots();
        RefreshButtons();
    }

    private void OnConfirmDeleteNo()
    {
        HideConfirmation();
        RefreshButtons();
    }

    private void HideConfirmation()
    {
        if (confirmationPanel != null)
            confirmationPanel.SetActive(false);
    }

    private void ClearSelection()
    {
        selectedClearId = null;
        selectedClearNumber = 0;
    }

    private void ClampPageIndex()
    {
        int maxPageIndex = records.Count > 0 ? (records.Count - 1) / PageSize : 0;
        pageIndex = Mathf.Clamp(pageIndex, 0, maxPageIndex);
    }

    private SkillDatabase ResolveSkillDatabase()
    {
        if (skillDatabase != null)
            return skillDatabase;

        return SaveManager.Instance != null ? SaveManager.Instance.skillDatabase : null;
    }

    private ItemDatabase ResolveItemDatabase()
    {
        if (itemDatabase != null)
            return itemDatabase;

        return SaveManager.Instance != null ? SaveManager.Instance.itemDatabase : null;
    }
}
