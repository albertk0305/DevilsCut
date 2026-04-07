using UnityEngine;
using UnityEngine.UI;
using TMPro; 

[RequireComponent(typeof(Slider))]
public class VolumeUI : MonoBehaviour
{
    private Slider volumeSlider;

    [Header("UI 연결")]
    public TextMeshProUGUI volumeText; // 인스펙터에서 볼륨 숫자를 띄울 텍스트를 연결할 칸

    void Start()
    {
        volumeSlider = GetComponent<Slider>();

        // 1. 설정창이 열릴 때, 슬라이더의 손잡이 위치를 현재 저장된 볼륨에 맞춤
        if (SoundManager.Instance != null)
        {
            volumeSlider.value = SoundManager.Instance.masterVolume;
        }

        // 2. 시작할 때 텍스트도 현재 볼륨에 맞게 한 번 업데이트 해줌
        UpdateVolumeText(volumeSlider.value);

        // 3. 슬라이더를 움직일 때마다 'OnSliderValueChanged' 함수가 자동으로 실행되도록 구독
        volumeSlider.onValueChanged.AddListener(OnSliderValueChanged);
    }

    // 슬라이더 값이 변할 때마다 실행될 함수
    private void OnSliderValueChanged(float value)
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.SetVolume(value); // 매니저에 볼륨 값 전달
        }

        // 텍스트도 실시간으로 변경해 줌
        UpdateVolumeText(value);
    }

    // 슬라이더의 0.0 ~ 1.0 값을 0 ~ 100 사이의 정수로 바꿔서 텍스트에 띄우는 함수
    private void UpdateVolumeText(float value)
    {
        if (volumeText != null)
        {
            // value(예: 0.553)에 100을 곱한 뒤(55.3), Mathf.RoundToInt로 반올림해서 정수(55)로 만듦!
            int volumePercent = Mathf.RoundToInt(value * 100f);

            // 정수를 문자로 바꿔서 텍스트에 적용
            volumeText.text = volumePercent.ToString();
        }
    }
}