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
        // 1. 방어 코드: 텍스트 컴포넌트를 아직 못 찾았다면, 지금 당장 찾아서 연결합니다!
        if (textComponent == null)
        {
            textComponent = GetComponent<TextMeshProUGUI>();
        }

        // 그래도 컴포넌트가 없다면 에러를 막기 위해 함수를 종료합니다.
        if (textComponent == null) return;

        // 2. 방어 코드: 다국어 매니저가 아직 안 만들어졌다면 에러를 내지 않고 종료합니다.
        // (조금 뒤에 매니저가 켜지면서 알아서 다시 글자를 바꿔줄 것입니다.)
        if (LocalizationManager.Instance == null) return;

        // 3. 모든 준비가 끝났을 때만 안전하게 글자를 바꿉니다.
        // (기존에 작성하셨던 다국어 매니저 호출 함수 이름을 사용하시면 됩니다. GetText 또는 GetValue 등)
        textComponent.text = LocalizationManager.Instance.GetText(textKey);
    }

    public void SetKey(string newKey)
    {
        textKey = newKey;
        UpdateText(); // 키를 바꾸자마자 즉시 텍스트 갱신!
    }
}