using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SynergyUI_Button : MonoBehaviour
{
    [Header("버튼 데이터")]
    public int requiredPoints;
    public string synergyNameKey;
    [TextArea] public string synergyDescKey;

    [Header("UI 컴포넌트")]
    public TextMeshProUGUI nameText;
    public GameObject activeBorder;
    public Button myButton;

    private SynergyUI_Manager manager;

    private int currentPoints;

    public void InitButton(int currentPoints, SynergyUI_Manager mgr)
    {
        manager = mgr;

        this.currentPoints = currentPoints;

        nameText.text = LocalizationManager.Instance != null ? LocalizationManager.Instance.GetText(synergyNameKey) : synergyNameKey;

        activeBorder.SetActive(currentPoints >= requiredPoints);

        myButton.onClick.RemoveAllListeners();
        myButton.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        if (manager != null)
        {
            manager.ShowDescription(synergyNameKey, synergyDescKey, currentPoints >= requiredPoints);
        }
    }
}
