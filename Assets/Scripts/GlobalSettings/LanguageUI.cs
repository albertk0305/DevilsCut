using UnityEngine;
using TMPro; 

[RequireComponent(typeof(TMP_Dropdown))]
public class LanguageUI : MonoBehaviour
{
    private TMP_Dropdown dropdown;

    void Start()
    {
        dropdown = GetComponent<TMP_Dropdown>();

        if (LocalizationManager.Instance != null)
        {
            dropdown.value = (int)LocalizationManager.Instance.currentLanguage;
        }

        dropdown.onValueChanged.AddListener(OnDropdownValueChanged);
    }

    private void OnDropdownValueChanged(int index)
    {
        if (LocalizationManager.Instance == null) return;

        // Dropdown order: 0 = Korean, 1 = English.
        if (index == 0)
        {
            LocalizationManager.Instance.SetKorean();
        }
        else if (index == 1)
        {
            LocalizationManager.Instance.SetEnglish();
        }

        DevLog.Log($"[Language] Changed to {(index == 0 ? "Korean" : "English")}.");
    }
}
