using UnityEngine;
using UnityEngine.UI;
using TMPro; 

[RequireComponent(typeof(Slider))]
public class VolumeUI : MonoBehaviour
{
    private enum VolumeType
    {
        Music,
        Sfx
    }

    private Slider volumeSlider;

    [Header("UI 연결")]
    [SerializeField] private VolumeType volumeType = VolumeType.Music;
    public TextMeshProUGUI volumeText;

    void Start()
    {
        volumeSlider = GetComponent<Slider>();

        if (SoundManager.Instance != null)
        {
            volumeSlider.SetValueWithoutNotify(GetCurrentVolume());
        }

        UpdateVolumeText(volumeSlider.value);

        volumeSlider.onValueChanged.AddListener(OnSliderValueChanged);
    }

    private void OnSliderValueChanged(float value)
    {
        if (SoundManager.Instance != null)
        {
            if (volumeType == VolumeType.Sfx)
                SoundManager.Instance.SetSfxVolume(value);
            else
                SoundManager.Instance.SetMusicVolume(value);
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

    private float GetCurrentVolume()
    {
        if (SoundManager.Instance == null)
            return volumeSlider != null ? volumeSlider.value : 1f;

        return volumeType == VolumeType.Sfx
            ? SoundManager.Instance.SfxVolume
            : SoundManager.Instance.MusicVolume;
    }
}
