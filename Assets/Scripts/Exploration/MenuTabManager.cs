using UnityEngine;
using UnityEngine.UI;

public class MenuTabManager : MonoBehaviour
{
    [Header("Tab Panels")]
    // 0: Status, 1: Supporter, 2: Karin, 3: Equipment
    public GameObject[] tabPanels;
    [SerializeField] private StatusUI statusUI;
    [SerializeField] private SupporterUI supporterUI;
    [SerializeField] private KarinEquipmentUI karinEquipmentUI;
    [SerializeField] private EquipmentUI equipmentUI;

    [Header("Tab Button Images")]
    public Image[] tabButtonImages;

    [Header("Button Colors")]
    public Color normalColor = Color.white;
    public Color activeColor = new Color(0.6f, 0.6f, 0.6f);

    private ClearRecordPlayerProfile previewProfile;
    private bool isPreviewMode;
    private bool[] tabButtonOriginalInteractable;
    private bool hasStoredTabButtonState;
    
    private void OnEnable()
    {
        SwitchTab(0);
    }

    private void OnDisable()
    {
        TimeScalePauseManager.ReleasePause(this);
        ClearPreviewMode();
    }

    public void OpenMenu()
    {
        ClearPreviewMode();
        TimeScalePauseManager.RequestPause(this);
        DevLog.Log("[Menu] Opened: time paused");

        gameObject.SetActive(true);
    }

    public void OpenPreview(ClearRecordPlayerProfile profile)
    {
        previewProfile = profile;
        isPreviewMode = true;
        ResolveStatusUI();
        if (statusUI != null)
            statusUI.SetPreviewProfile(profile);

        ResolveSupporterUI();
        if (supporterUI != null)
            supporterUI.SetPreviewProfile(profile);

        ResolveKarinEquipmentUI();
        if (karinEquipmentUI != null)
            karinEquipmentUI.SetPreviewProfile(profile);

        ResolveEquipmentUI();
        if (equipmentUI != null)
            equipmentUI.SetPreviewProfile(profile);

        StorePreviewTabButtonState();
        TimeScalePauseManager.RequestPause(this);
        DevLog.Log($"[MainMenu] ClearRecord status preview opened: clearId={profile?.ClearId}");

        gameObject.SetActive(true);
        SwitchTab(0);
    }

    public void CloseMenu()
    {
        TimeScalePauseManager.ReleasePause(this);
        DevLog.Log("[Menu] Closed: time scale refreshed");

        ClearPreviewMode();
        gameObject.SetActive(false);
    }

    public void SwitchTab(int tabIndex)
    {
        if (isPreviewMode && tabIndex != 0 && tabIndex != 1 && tabIndex != 2 && tabIndex != 3)
        {
            DevLog.Log("[MainMenu] ClearRecord status preview supports Status, Supporter, Karin, and Equipment tabs in this step.");
            tabIndex = 0;
        }

        for (int i = 0; i < tabPanels.Length; i++)
        {
            bool isActive = (i == tabIndex);
            tabPanels[i].SetActive(isActive);

            if (i < tabButtonImages.Length && tabButtonImages[i] != null)
            {
                tabButtonImages[i].color = isActive ? activeColor : normalColor;
            }
        }

        if (tabIndex == 0)
            RefreshStatusTab();
    }

    private void RefreshStatusTab()
    {
        ResolveStatusUI();

        if (statusUI != null)
            statusUI.Refresh();
    }

    private void ClearPreviewMode()
    {
        previewProfile = null;
        isPreviewMode = false;
        ResolveStatusUI();
        if (statusUI != null)
            statusUI.ClearPreviewProfile();

        ResolveSupporterUI();
        if (supporterUI != null)
            supporterUI.ClearPreviewProfile();

        ResolveKarinEquipmentUI();
        if (karinEquipmentUI != null)
            karinEquipmentUI.ClearPreviewProfile();

        ResolveEquipmentUI();
        if (equipmentUI != null)
            equipmentUI.ClearPreviewProfile();

        RestorePreviewTabButtonsInteractable();
    }

    private void ResolveStatusUI()
    {
        if (statusUI == null && tabPanels != null && tabPanels.Length > 0 && tabPanels[0] != null)
            statusUI = tabPanels[0].GetComponentInChildren<StatusUI>(true);
    }

    private void ResolveSupporterUI()
    {
        if (supporterUI == null && tabPanels != null && tabPanels.Length > 1 && tabPanels[1] != null)
            supporterUI = tabPanels[1].GetComponentInChildren<SupporterUI>(true);
    }

    private void ResolveKarinEquipmentUI()
    {
        if (karinEquipmentUI == null && tabPanels != null && tabPanels.Length > 2 && tabPanels[2] != null)
            karinEquipmentUI = tabPanels[2].GetComponentInChildren<KarinEquipmentUI>(true);
    }

    private void ResolveEquipmentUI()
    {
        if (equipmentUI == null && tabPanels != null && tabPanels.Length > 3 && tabPanels[3] != null)
            equipmentUI = tabPanels[3].GetComponentInChildren<EquipmentUI>(true);
    }

    private void StorePreviewTabButtonState()
    {
        if (tabButtonImages == null)
            return;

        tabButtonOriginalInteractable = new bool[tabButtonImages.Length];
        hasStoredTabButtonState = true;

        for (int i = 0; i < tabButtonImages.Length; i++)
        {
            if (tabButtonImages[i] == null)
                continue;

            Button button = tabButtonImages[i].GetComponent<Button>();
            if (button != null)
                tabButtonOriginalInteractable[i] = button.interactable;
        }
    }

    private void RestorePreviewTabButtonsInteractable()
    {
        if (!hasStoredTabButtonState || tabButtonImages == null || tabButtonOriginalInteractable == null)
            return;

        for (int i = 1; i < tabButtonImages.Length; i++)
        {
            if (tabButtonImages[i] == null)
                continue;

            Button button = tabButtonImages[i].GetComponent<Button>();
            if (button == null)
                continue;

            bool originalInteractable = i < tabButtonOriginalInteractable.Length && tabButtonOriginalInteractable[i];
            button.interactable = originalInteractable;
        }

        hasStoredTabButtonState = false;
        tabButtonOriginalInteractable = null;
    }
}
