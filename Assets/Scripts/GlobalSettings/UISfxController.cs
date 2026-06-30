using UnityEngine;

public class UISfxController : MonoBehaviour
{
    public static UISfxController Instance;

    [Header("UI SFX")]
    [SerializeField] private AudioClip buttonClickClip;
    [SerializeField, Range(0f, 1f)] private float buttonClickVolume = 1f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(this);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void PlayButtonClick()
    {
        if (buttonClickClip == null) return;
        if (SoundManager.Instance == null) return;

        SoundManager.Instance.PlaySFX(buttonClickClip, buttonClickVolume);
    }
}
