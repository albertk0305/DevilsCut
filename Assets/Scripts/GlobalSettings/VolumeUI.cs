using UnityEngine;
using UnityEngine.UI;
using TMPro; 

[RequireComponent(typeof(Slider))]
public class VolumeUI : MonoBehaviour
{
    private enum VolumeType
    {
        Master,
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
            switch (volumeType)
            {
                case VolumeType.Master:
                    SoundManager.Instance.SetMasterVolume(value);
                    break;
                case VolumeType.Sfx:
                    SoundManager.Instance.SetSfxVolume(value);
                    break;
                default:
                    SoundManager.Instance.SetMusicVolume(value);
                    break;
            }
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

        switch (volumeType)
        {
            case VolumeType.Master:
                return SoundManager.Instance.MasterVolume;
            case VolumeType.Sfx:
                return SoundManager.Instance.SfxVolume;
            default:
                return SoundManager.Instance.MusicVolume;
        }
    }
}
