using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InfiniteBattleResultUIController : MonoBehaviour
{
    [SerializeField] private GameObject rootCanvas;
    [SerializeField] private TMP_Text recordText;
    [SerializeField] private Button exitButton;
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private bool isShowingResult;
    private bool isExiting;

    private void Awake()
    {
        ResolveReferences();
        if (isShowingResult)
            DevLog.Log("[InfiniteBattle] ResultUI Awake executed while ShowResult is active. Keeping root canvas visible.");

        if (exitButton != null)
        {
            exitButton.onClick.RemoveListener(OnClickExit);
            exitButton.onClick.AddListener(OnClickExit);
        }
    }

    private void OnDestroy()
    {
        if (exitButton != null)
            exitButton.onClick.RemoveListener(OnClickExit);
    }

    public bool ShowResult(int currentRecord, int previousBest)
    {
        isShowingResult = true;
        isExiting = false;
        ResolveReferences();

        string rootNameBefore = rootCanvas != null ? rootCanvas.name : "null";
        bool rootActiveSelfBefore = rootCanvas != null && rootCanvas.activeSelf;
        bool rootActiveInHierarchyBefore = rootCanvas != null && rootCanvas.activeInHierarchy;
        DevLog.Log($"[InfiniteBattle] ResultUI ShowResult entered. rootCanvasNull={rootCanvas == null}, rootCanvasName={rootNameBefore}, activeSelfBefore={rootActiveSelfBefore}, activeInHierarchyBefore={rootActiveInHierarchyBefore}");

        GameObject root = rootCanvas != null ? rootCanvas : gameObject;
        if (root == null)
        {
            DevLog.LogWarning("[InfiniteBattle] Result UI cannot show: rootCanvas and controller GameObject are both null.");
            return false;
        }

        if (rootCanvas == null)
        {
            rootCanvas = root;
            DevLog.LogWarning("[InfiniteBattle] Result UI rootCanvas was not assigned. Using controller GameObject as root.");
        }

        root.SetActive(true);
        CanvasGroup canvasGroup = root.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        DevLog.Log($"[InfiniteBattle] ResultUI root activated. rootCanvasName={root.name}, activeSelfAfter={root.activeSelf}, activeInHierarchyAfter={root.activeInHierarchy}");
        if (!root.activeInHierarchy)
            LogInactiveParentChain(root);

        ResolveReferences();
        DevLog.Log($"[InfiniteBattle] ResultUI references. recordTextFound={recordText != null}, recordTextName={(recordText != null ? recordText.gameObject.name : "null")}, exitButtonFound={exitButton != null}, exitButtonName={(exitButton != null ? exitButton.gameObject.name : "null")}");

        if (recordText != null)
            recordText.text = $"Your Record is {Mathf.Max(0, currentRecord)}\nBest score was {Mathf.Max(0, previousBest)}";
        else
            DevLog.LogWarning("[InfiniteBattle] Result UI RecordText was not found.");

        if (exitButton != null)
        {
            exitButton.onClick.RemoveListener(OnClickExit);
            exitButton.onClick.AddListener(OnClickExit);
        }
        else
        {
            DevLog.LogWarning("[InfiniteBattle] Result UI ExitButton was not found.");
        }

        return root.activeInHierarchy && recordText != null;
    }

    public static InfiniteBattleResultUIController GetOrCreate()
    {
        InfiniteBattleResultUIController controller = FindExistingController();
        if (controller != null)
            return controller;

        GameObject canvas = FindInactiveGameObjectByName("InfiniteBattleCanvas");
        if (canvas == null)
        {
            DevLog.LogWarning("[InfiniteBattle] InfiniteBattleCanvas was not found in the loaded scene.");
            return null;
        }

        return canvas.AddComponent<InfiniteBattleResultUIController>();
    }

    private void HideResult()
    {
        isShowingResult = false;

        if (rootCanvas != null)
            rootCanvas.SetActive(false);
        else
            gameObject.SetActive(false);
    }

    private void OnClickExit()
    {
        if (isExiting)
            return;

        isExiting = true;
        isShowingResult = false;
        InfiniteBattleRunContext.Clear();
        TimeScalePauseManager.ClearAllPauses();
        Time.timeScale = 1f;
        SceneLoader.LoadScene(string.IsNullOrEmpty(mainMenuSceneName) ? "MainMenu" : mainMenuSceneName);
    }

    private void ResolveReferences()
    {
        if (rootCanvas == null)
        {
            if (gameObject.name == "InfiniteBattleCanvas")
                rootCanvas = gameObject;
            else
                rootCanvas = FindInactiveGameObjectByName("InfiniteBattleCanvas");
        }

        GameObject searchRoot = rootCanvas != null ? rootCanvas : gameObject;

        if (recordText == null)
            recordText = FindChildComponentByName<TMP_Text>(searchRoot, "RecordText");

        if (recordText == null)
            recordText = searchRoot.GetComponentInChildren<TMP_Text>(true);

        if (exitButton == null)
            exitButton = FindChildComponentByName<Button>(searchRoot, "ExitButton");

        if (exitButton == null)
            exitButton = searchRoot.GetComponentInChildren<Button>(true);
    }

    private static InfiniteBattleResultUIController FindExistingController()
    {
        InfiniteBattleResultUIController[] controllers = FindObjectsByType<InfiniteBattleResultUIController>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (InfiniteBattleResultUIController controller in controllers)
        {
            if (controller != null && IsLoadedSceneObject(controller.gameObject))
                return controller;
        }

        InfiniteBattleResultUIController[] resourceControllers = Resources.FindObjectsOfTypeAll<InfiniteBattleResultUIController>();
        foreach (InfiniteBattleResultUIController controller in resourceControllers)
        {
            if (controller != null && IsLoadedSceneObject(controller.gameObject))
                return controller;
        }

        return null;
    }

    private static T FindChildComponentByName<T>(GameObject root, string objectName) where T : Component
    {
        if (root == null || string.IsNullOrEmpty(objectName))
            return null;

        T[] components = root.GetComponentsInChildren<T>(true);
        foreach (T component in components)
        {
            if (component != null && component.gameObject.name == objectName)
                return component;
        }

        return null;
    }

    private static GameObject FindInactiveGameObjectByName(string objectName)
    {
        GameObject[] sceneObjects = FindObjectsByType<GameObject>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (GameObject obj in sceneObjects)
        {
            if (obj != null && obj.name == objectName && IsLoadedSceneObject(obj))
                return obj;
        }

        GameObject[] objects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject obj in objects)
        {
            if (obj != null && obj.name == objectName && IsLoadedSceneObject(obj))
                return obj;
        }

        return null;
    }

    private static bool IsLoadedSceneObject(GameObject obj)
    {
        return obj != null
            && obj.scene.IsValid()
            && obj.scene.isLoaded
            && !string.IsNullOrEmpty(obj.scene.name);
    }

    private static void LogInactiveParentChain(GameObject root)
    {
        if (root == null)
            return;

        Transform current = root.transform;
        while (current != null)
        {
            GameObject currentObject = current.gameObject;
            DevLog.LogWarning($"[InfiniteBattle] Result UI hierarchy state: name={currentObject.name}, activeSelf={currentObject.activeSelf}, activeInHierarchy={currentObject.activeInHierarchy}");
            current = current.parent;
        }
    }
}
