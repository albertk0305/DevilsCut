using UnityEngine;
using UnityEngine.UI;

public class MenuTabManager : MonoBehaviour
{
    [Header("Tab Panels")]
    // 0: Status, 1: Supporter, 2: Karin, 3: Equipment
    public GameObject[] tabPanels;
    [SerializeField] private StatusUI statusUI;

    [Header("Tab Button Images")]
    public Image[] tabButtonImages;

    [Header("Button Colors")]
    public Color normalColor = Color.white;
    public Color activeColor = new Color(0.6f, 0.6f, 0.6f);
    
    private void OnEnable()
    {
        SwitchTab(0);
    }

    private void OnDisable()
    {
        TimeScalePauseManager.ReleasePause(this);
    }

    public void OpenMenu()
    {
        TimeScalePauseManager.RequestPause(this);
        DevLog.Log("[Menu] Opened: time paused");

        gameObject.SetActive(true);
    }

    public void CloseMenu()
    {
        TimeScalePauseManager.ReleasePause(this);
        DevLog.Log("[Menu] Closed: time scale refreshed");

        gameObject.SetActive(false);
    }

    public void SwitchTab(int tabIndex)
    {
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
        if (statusUI == null && tabPanels != null && tabPanels.Length > 0 && tabPanels[0] != null)
            statusUI = tabPanels[0].GetComponentInChildren<StatusUI>(true);

        if (statusUI != null)
            statusUI.Refresh();
    }
}
