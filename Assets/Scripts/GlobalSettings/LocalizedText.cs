using UnityEngine;
using TMPro; // TextMeshPro를 사용하기 위해 필요해요

// 이 스크립트를 넣으면 TextMeshPro 컴포넌트가 자동으로 필수로 붙어요
[RequireComponent(typeof(TextMeshProUGUI))]
//언어 패치 적용할 텍스트에 붙이는 코드
public class LocalizedText : MonoBehaviour
{
    public string textKey; // Inspector에서 "btn_start" 등을 적어줄 곳
    private TextMeshProUGUI textComponent;

    void Start()
    {
        textComponent = GetComponent<TextMeshProUGUI>();
        UpdateText(); // 처음 시작할 때 한 번 글자를 맞춰줌

        // 매니저의 방송 마이크에 이 'UpdateText' 함수를 귀기울이게 연결(구독)함
        LocalizationManager.Instance.OnLanguageChanged += UpdateText;
    }

    void OnDestroy()
    {
        // 씬이 바뀌거나 버튼이 파괴될 때 방송 듣기를 취소함 (에러 방지)
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged -= UpdateText;
    }

    void UpdateText()
    {
        if (LocalizationManager.Instance != null && !string.IsNullOrEmpty(textKey))
        {
            textComponent.text = LocalizationManager.Instance.GetText(textKey);
        }
    }

    public void SetKey(string newKey)
    {
        textKey = newKey;
        UpdateText(); // 키를 바꾸자마자 즉시 텍스트 갱신!
    }
}