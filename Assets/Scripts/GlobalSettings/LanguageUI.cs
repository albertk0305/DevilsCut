using UnityEngine;
using TMPro; 

[RequireComponent(typeof(TMP_Dropdown))]
//설정 UI에서 드롭다운으로 언어 변경해주는 코드
public class LanguageUI : MonoBehaviour
{
    private TMP_Dropdown dropdown;

    void Start()
    {
        dropdown = GetComponent<TMP_Dropdown>();

        // 1. 현재 저장된 언어 설정에 맞게 드롭다운의 초기 값을 세팅해
        if (LocalizationManager.Instance != null)
        {
            // Enum 값을 int로 변환해서 드롭다운 인덱스(0, 1)와 맞춰줘
            dropdown.value = (int)LocalizationManager.Instance.currentLanguage;
        }

        // 2. 드롭다운 값이 바뀔 때 실행될 함수 연결
        dropdown.onValueChanged.AddListener(OnDropdownValueChanged);
    }

    private void OnDropdownValueChanged(int index)
    {
        if (LocalizationManager.Instance == null) return;

        // 드롭다운 인덱스에 따라 매니저의 함수를 호출해
        // 0: Korean, 1: English (우리가 설정한 순서 기준이야)
        if (index == 0)
        {
            LocalizationManager.Instance.SetKorean();
        }
        else if (index == 1)
        {
            LocalizationManager.Instance.SetEnglish();
        }

        DevLog.Log($"언어 변경됨: {(index == 0 ? "한국어" : "영어")}");
    }
}