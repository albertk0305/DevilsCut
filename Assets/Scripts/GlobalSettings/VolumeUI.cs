using UnityEngine;
using UnityEngine.UI;
using TMPro; 

[RequireComponent(typeof(Slider))]
public class VolumeUI : MonoBehaviour
{
    private Slider volumeSlider;

    [Header("UI 연결")]
    public TextMeshProUGUI volumeText;

    void Start()
    {
        volumeSlider = GetComponent<Slider>();

        if (SoundManager.Instance != null)
        {
            volumeSlider.value = SoundManager.Instance.masterVolume;
        }

        UpdateVolumeText(volumeSlider.value);

        volumeSlider.onValueChanged.AddListener(OnSliderValueChanged);
    }

    private void OnSliderValueChanged(float value)
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.SetVolume(value);
        }

        UpdateVolumeText(value);
    }

    private void UpdateVolumeText(float value)
    {
        if (volumeText != null)
        {
            int volumePercent = Mathf.RoundToInt(value * 100f);

            volumeText.text = volumePercent.ToString();
        }
    }
}
