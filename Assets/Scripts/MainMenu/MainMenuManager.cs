using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenuManager : MonoBehaviour
{
    public GameObject settingsPanel;
    public Button continueButton;
    public GameObject confirmNewGamePanel;
    public TextMeshProUGUI confirmNewGameText;
    public string explorationSceneName = "Exploration";
    [SerializeField] private TextMeshProUGUI continueButtonText;
    [SerializeField] private Button infiniteBattleButton;
    [SerializeField] private TextMeshProUGUI infiniteBattleButtonText;
    [SerializeField] private ClearDataSelectCanvasController clearDataSelectCanvasController;
    [SerializeField] private DialogueDataDatabase dialogueDataDatabase;
    [SerializeField] private DialogueData newGameDialogueData;
    [SerializeField] private float enabledTextAlpha = 1f;
    [SerializeField] private float disabledTextAlpha = 0.4f;
    [SerializeField] private bool disableTextButtonTransition = true;
    [SerializeField] private Button helpButton;
    [SerializeField] private HelpCanvasController helpCanvasController;
    [SerializeField] private Button creditsButton;
    [SerializeField] private GameObject creditsCanvas;
    [SerializeField] private Button creditsExitButton;
    [SerializeField] private TextMeshProUGUI creditText;
    [SerializeField] private string creditsTextKey = "ui_credits_text";

    private const string NewGameOverwriteMessage = "저장된 진행 상황이 있습니다.\n새 게임을 시작하면 기존 이어하기 데이터가 삭제됩니다.\n정말 새로 시작하시겠습니까?";

    void Start()
    {
        ResolveHelpReferences();
        RegisterHelpListener();
        ResolveCreditsReferences();
        RegisterCreditsListeners();

        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (confirmNewGamePanel != null) confirmNewGamePanel.SetActive(false);
        if (clearDataSelectCanvasController != null) clearDataSelectCanvasController.Hide();
        if (helpCanvasController != null) helpCanvasController.Hide();
        if (creditsCanvas != null) creditsCanvas.SetActive(false);
        UpdateContinueButtonState();
        UpdateInfiniteBattleButtonState();
    }

    private void OnEnable()
    {
        ResolveHelpReferences();
        RegisterHelpListener();
        ResolveCreditsReferences();
        RegisterCreditsListeners();
        SubscribeLanguageChanged();
        UpdateContinueButtonState();
        UpdateInfiniteBattleButtonState();
    }

    private void OnDisable()
    {
        UnsubscribeLanguageChanged();
    }

    private void OnDestroy()
    {
        UnregisterHelpListener();
        UnregisterCreditsListeners();
        UnsubscribeLanguageChanged();
    }

    private void UpdateContinueButtonState()
    {
        if (continueButton == null)
            return;

        bool hasSave = SaveManager.Instance != null && SaveManager.Instance.HasContinueSave();
        ApplyTextButtonState(continueButton, ref continueButtonText, hasSave, true);
    }

    private void UpdateInfiniteBattleButtonState()
    {
        if (infiniteBattleButton == null)
            return;

        bool hasClearRecords = SaveManager.Instance != null && SaveManager.Instance.HasAnyClearRecords();
        ApplyTextButtonState(infiniteBattleButton, ref infiniteBattleButtonText, hasClearRecords, false);
    }

    private void ApplyTextButtonState(Button button, ref TextMeshProUGUI label, bool interactable, bool keepActive)
    {
        if (button == null)
            return;

        if (keepActive)
            button.gameObject.SetActive(true);

        if (disableTextButtonTransition)
            button.transition = Selectable.Transition.None;

        button.interactable = interactable;

        if (label == null)
            label = button.GetComponentInChildren<TextMeshProUGUI>(true);

        if (label == null)
            return;

        Color color = label.color;
        color.a = interactable ? enabledTextAlpha : disabledTextAlpha;
        label.color = color;
    }

    public void OnClickStart()
    {
        if (SaveManager.Instance != null && SaveManager.Instance.HasContinueSave())
        {
            ShowNewGameConfirmPanel();
            return;
        }

        StartNewGameInternal();
    }

    public void OnConfirmNewGameYes()
    {
        if (confirmNewGamePanel != null)
            confirmNewGamePanel.SetActive(false);

        StartNewGameInternal();
    }

    public void OnConfirmNewGameNo()
    {
        if (confirmNewGamePanel != null)
            confirmNewGamePanel.SetActive(false);
    }

    private void ShowNewGameConfirmPanel()
    {
        if (confirmNewGameText != null)
            confirmNewGameText.text = NewGameOverwriteMessage;

        if (confirmNewGamePanel != null)
        {
            confirmNewGamePanel.SetActive(true);
            return;
        }

        DevLog.LogWarning("[Save] New-game confirmation panel is not assigned; starting immediately.");
        StartNewGameInternal();
    }

    private void StartNewGameInternal()
    {
        GameStartManager.GetOrCreateInstance().StartNewGame(dialogueDataDatabase, newGameDialogueData);
    }
    public void OnClickContinue()
    {
        if (SaveManager.Instance == null)
        {
            DevLog.LogWarning("[Save] Continue failed: SaveManager missing.");
            UpdateContinueButtonState();
            return;
        }

        if (!SaveManager.Instance.TryPrepareContinueLoad(out string sceneName))
        {
            UpdateContinueButtonState();
            return;
        }

        DevLog.Log("[MainMenu] Loading continue data.");
        SceneLoader.LoadScene(string.IsNullOrEmpty(sceneName) ? explorationSceneName : sceneName);
    }

    public void OnClickInfiniteBattle()
    {
        UpdateInfiniteBattleButtonState();

        if (infiniteBattleButton != null && !infiniteBattleButton.interactable)
            return;

        if (clearDataSelectCanvasController == null)
        {
            DevLog.LogWarning("[MainMenu] Clear data select canvas is not assigned.");
            return;
        }

        clearDataSelectCanvasController.Show();
    }

    public void OnClickHelp()
    {
        ResolveHelpReferences();

        if (helpCanvasController != null)
        {
            helpCanvasController.Show();
            DevLog.Log("[MainMenu] Help opened.");
            return;
        }

        DevLog.LogWarning("[MainMenu] HelpCanvasController is not assigned and could not be found.");
    }

    private void ResolveHelpReferences()
    {
        if (helpButton == null)
            helpButton = FindSceneComponentByName<Button>("HelpButton");

        if (helpCanvasController == null)
            helpCanvasController = FindSceneComponent<HelpCanvasController>();

        if (helpCanvasController == null)
        {
            GameObject helpCanvas = FindSceneGameObjectByName("HelpCanvas");
            if (helpCanvas != null)
                helpCanvasController = helpCanvas.GetComponent<HelpCanvasController>() ?? helpCanvas.AddComponent<HelpCanvasController>();
        }
    }

    private void RegisterHelpListener()
    {
        if (helpButton == null || HasPersistentListener(helpButton, nameof(OnClickHelp)))
            return;

        helpButton.onClick.RemoveListener(OnClickHelp);
        helpButton.onClick.AddListener(OnClickHelp);
    }

    private void UnregisterHelpListener()
    {
        if (helpButton != null && !HasPersistentListener(helpButton, nameof(OnClickHelp)))
            helpButton.onClick.RemoveListener(OnClickHelp);
    }

    private static bool HasPersistentListener(Button button, string methodName)
    {
        if (button == null || string.IsNullOrEmpty(methodName))
            return false;

        int count = button.onClick.GetPersistentEventCount();
        for (int i = 0; i < count; i++)
        {
            if (button.onClick.GetPersistentMethodName(i) == methodName)
                return true;
        }

        return false;
    }

    public void OnClickCredits()
    {
        ResolveCreditsReferences();
        RefreshCreditText();

        if (creditsCanvas != null)
        {
            creditsCanvas.SetActive(true);
            DevLog.Log("[MainMenu] Credits opened.");
            return;
        }

        DevLog.LogWarning("[MainMenu] CreditsCanvas is not assigned and could not be found.");
    }

    public void OnClickCreditsExit()
    {
        ResolveCreditsReferences();

        if (creditsCanvas != null)
            creditsCanvas.SetActive(false);
    }

    private void ResolveCreditsReferences()
    {
        if (creditsButton == null)
            creditsButton = FindSceneComponentByName<Button>("CreditsButton");

        if (creditsCanvas == null)
            creditsCanvas = FindSceneGameObjectByName("CreditsCanvas");

        GameObject searchRoot = creditsCanvas != null ? creditsCanvas : gameObject;

        if (creditsExitButton == null)
            creditsExitButton = FindChildComponentByName<Button>(searchRoot, "ExitButton");

        if (creditText == null)
            creditText = FindChildComponentByName<TextMeshProUGUI>(searchRoot, "CreditText");
    }

    private void RegisterCreditsListeners()
    {
        if (creditsButton != null)
        {
            creditsButton.onClick.RemoveListener(OnClickCredits);
            creditsButton.onClick.AddListener(OnClickCredits);
        }

        if (creditsExitButton != null)
        {
            creditsExitButton.onClick.RemoveListener(OnClickCreditsExit);
            creditsExitButton.onClick.AddListener(OnClickCreditsExit);
        }
    }

    private void UnregisterCreditsListeners()
    {
        if (creditsButton != null)
            creditsButton.onClick.RemoveListener(OnClickCredits);

        if (creditsExitButton != null)
            creditsExitButton.onClick.RemoveListener(OnClickCreditsExit);
    }

    private void RefreshCreditText()
    {
        if (creditText == null)
            return;

        LocalizedText localizedText = creditText.GetComponent<LocalizedText>();
        if (localizedText != null)
            localizedText.SetKey(creditsTextKey);
        else if (LocalizationManager.Instance != null)
            creditText.text = LocalizationManager.Instance.GetText(creditsTextKey);
        else
            creditText.text = creditsTextKey;

        if (!string.IsNullOrEmpty(creditText.text))
            creditText.text = creditText.text.Replace("\\n", "\n");
    }

    private void SubscribeLanguageChanged()
    {
        if (LocalizationManager.Instance == null)
            return;

        LocalizationManager.Instance.OnLanguageChanged -= OnLanguageChanged;
        LocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;
    }

    private void UnsubscribeLanguageChanged()
    {
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged -= OnLanguageChanged;
    }

    private void OnLanguageChanged()
    {
        if (creditsCanvas != null && creditsCanvas.activeInHierarchy)
            RefreshCreditText();
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

    private static T FindSceneComponentByName<T>(string objectName) where T : Component
    {
        GameObject obj = FindSceneGameObjectByName(objectName);
        return obj != null ? obj.GetComponent<T>() : null;
    }

    private static T FindSceneComponent<T>() where T : Component
    {
        T[] components = FindObjectsByType<T>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (T component in components)
        {
            if (component != null
                && component.gameObject.scene == SceneManager.GetActiveScene())
                return component;
        }

        return null;
    }

    private static GameObject FindSceneGameObjectByName(string objectName)
    {
        if (string.IsNullOrEmpty(objectName))
            return null;

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
            return null;

        GameObject[] roots = scene.GetRootGameObjects();
        foreach (GameObject root in roots)
        {
            GameObject found = FindChildGameObjectByName(root, objectName);
            if (found != null)
                return found;
        }

        return null;
    }

    private static GameObject FindChildGameObjectByName(GameObject root, string objectName)
    {
        if (root == null || string.IsNullOrEmpty(objectName))
            return null;

        if (root.name == objectName)
            return root;

        Transform rootTransform = root.transform;
        for (int i = 0; i < rootTransform.childCount; i++)
        {
            GameObject found = FindChildGameObjectByName(rootTransform.GetChild(i).gameObject, objectName);
            if (found != null)
                return found;
        }

        return null;
    }

    public void OnClickQuit()
    {
        DevLog.Log("[MainMenu] Quit requested.");
        // Application.Quit only exits in player builds.
        Application.Quit();
    }
}
