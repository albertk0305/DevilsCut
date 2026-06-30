using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ButtonClickSfxAutoBinder : MonoBehaviour
{
    public static ButtonClickSfxAutoBinder Instance;

    private Coroutine scanRoutine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureInstance()
    {
        if (Instance != null)
            return;

        GameObject binderObject = new GameObject("ButtonClickSfxAutoBinder");
        binderObject.AddComponent<ButtonClickSfxAutoBinder>();
    }

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

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        QueueScanLoadedScenes();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void RescanLoadedScenes()
    {
        RegisterButtonsInLoadedScenes();
    }

    public void RegisterButtonsIn(GameObject root)
    {
        if (root == null)
            return;

        Button[] buttons = root.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
            RegisterButton(buttons[i]);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        QueueScanLoadedScenes();
    }

    private void QueueScanLoadedScenes()
    {
        if (!isActiveAndEnabled)
            return;

        if (scanRoutine != null)
            StopCoroutine(scanRoutine);

        scanRoutine = StartCoroutine(ScanLoadedScenesNextFrame());
    }

    private IEnumerator ScanLoadedScenesNextFrame()
    {
        yield return null;

        RegisterButtonsInLoadedScenes();
        scanRoutine = null;
    }

    private void RegisterButtonsInLoadedScenes()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded)
                continue;

            GameObject[] roots = scene.GetRootGameObjects();
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
                RegisterButtonsIn(roots[rootIndex]);
        }
    }

    private void RegisterButton(Button button)
    {
        if (button == null)
            return;

        if (button.GetComponent<ButtonClickSfxIgnore>() != null)
            return;

        ButtonClickSfxBinding binding = button.GetComponent<ButtonClickSfxBinding>();
        if (binding == null)
            binding = button.gameObject.AddComponent<ButtonClickSfxBinding>();

        binding.Bind(button);
    }
}
