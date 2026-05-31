using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingSceneController : MonoBehaviour
{
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TextMeshProUGUI loadingText;
    [SerializeField] private Sprite[] loadingBackgrounds;
    [SerializeField] private string loadingBaseText = "Loading";
    [SerializeField] private float dotInterval = 0.35f;
    [SerializeField] private float minimumLoadingTime = 0.8f;

    private const string FallbackSceneName = "MainMenu";

    private void Start()
    {
        SelectRandomBackground();
        StartCoroutine(AnimateLoadingText());
        StartCoroutine(LoadTargetSceneRoutine());
    }

    private void SelectRandomBackground()
    {
        if (backgroundImage == null || loadingBackgrounds == null || loadingBackgrounds.Length == 0)
            return;

        backgroundImage.sprite = loadingBackgrounds[Random.Range(0, loadingBackgrounds.Length)];
        backgroundImage.enabled = backgroundImage.sprite != null;
    }

    private IEnumerator AnimateLoadingText()
    {
        int dotCount = 0;

        while (true)
        {
            if (loadingText != null)
                loadingText.text = loadingBaseText + new string('.', dotCount);

            dotCount = (dotCount + 1) % 4;

            float interval = Mathf.Max(0.01f, dotInterval);
            yield return new WaitForSecondsRealtime(interval);
        }
    }

    private IEnumerator LoadTargetSceneRoutine()
    {
        string targetSceneName = SceneLoader.HasTargetScene()
            ? SceneLoader.TargetSceneName
            : FallbackSceneName;

        if (!SceneLoader.HasTargetScene())
            DevLog.LogWarning($"[SceneLoader] Target scene is empty. Falling back to {FallbackSceneName}.");

        AsyncOperation operation = SceneManager.LoadSceneAsync(targetSceneName);
        if (operation == null)
        {
            DevLog.LogWarning($"[SceneLoader] Failed to start async load. targetScene={targetSceneName}");
            SceneLoader.ClearTargetScene();
            yield break;
        }

        operation.allowSceneActivation = false;

        float elapsed = 0f;

        while (operation.progress < 0.9f || elapsed < minimumLoadingTime)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        SceneLoader.ClearTargetScene();
        operation.allowSceneActivation = true;
    }
}
