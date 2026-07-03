using UnityEngine;

[RequireComponent(typeof(Camera))]

//카메라 비율 고정해주는 함수. 메인 카메라에 넣으면 됨.
public class CameraResolution : MonoBehaviour
{
    [SerializeField] private float targetWidth = 16f;
    [SerializeField] private float targetHeight = 9f;

    private Camera targetCamera;
    private int lastScreenWidth = -1;
    private int lastScreenHeight = -1;

    private void Awake()
    {
        targetCamera = GetComponent<Camera>();
        ApplyResolution();
    }

    private void OnEnable()
    {
        if (targetCamera == null)
        {
            targetCamera = GetComponent<Camera>();
        }

        ApplyResolution();
    }

    private void Update()
    {
        if (Screen.width == lastScreenWidth && Screen.height == lastScreenHeight)
        {
            return;
        }

        ApplyResolution();
    }

    private void ApplyResolution()
    {
        if (targetCamera == null)
        {
            targetCamera = GetComponent<Camera>();
        }

        int screenWidth = Screen.width;
        int screenHeight = Screen.height;

        if (screenWidth <= 0 || screenHeight <= 0)
        {
            return;
        }

        lastScreenWidth = screenWidth;
        lastScreenHeight = screenHeight;

        if (targetWidth <= 0f || targetHeight <= 0f)
        {
            Debug.LogWarning($"[CameraResolution] Invalid target aspect size. targetWidth={targetWidth}, targetHeight={targetHeight}", this);
            return;
        }

        Rect rect = new Rect(0f, 0f, 1f, 1f);
        float targetRatio = targetWidth / targetHeight;
        float currentRatio = (float)screenWidth / screenHeight;

        if (Mathf.Approximately(currentRatio, targetRatio))
        {
            targetCamera.rect = rect;
            return;
        }

        if (currentRatio < targetRatio)
        {
            float scaleHeight = currentRatio / targetRatio;
            rect.height = scaleHeight;
            rect.y = (1f - scaleHeight) * 0.5f;
        }
        else
        {
            float scaleWidth = targetRatio / currentRatio;
            rect.width = scaleWidth;
            rect.x = (1f - scaleWidth) * 0.5f;
        }

        targetCamera.rect = rect;
    }
}
