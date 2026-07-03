using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class HelpTopic
{
    public string topicId;
    public string titleTextKey;
    public string bodyTextKey;
}

public class HelpCanvasController : MonoBehaviour
{
    [SerializeField] private GameObject rootCanvas;
    [SerializeField] private Button exitButton;
    [SerializeField] private Button leftButton;
    [SerializeField] private Button rightButton;
    [SerializeField] private Button[] topicButtons;
    [SerializeField] private TMP_Text[] topicButtonTexts;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private ScrollRect bodyScrollRect;
    [SerializeField] private Scrollbar bodyScrollbar;
    [SerializeField] private List<HelpTopic> topics = new List<HelpTopic>();
    [SerializeField] private int topicsPerPage = 4;

    private int pageIndex;
    private int selectedTopicIndex = -1;
    private Coroutine resetScrollRoutine;
    private bool isSubscribedToLanguageChanged;

    private GameObject Root => rootCanvas != null ? rootCanvas : gameObject;

    private void Awake()
    {
        ResolveReferences();
        RegisterListeners();
    }

    private void OnEnable()
    {
        ResolveReferences();
        RegisterListeners();
        SubscribeLanguageChanged();
    }

    private void OnDisable()
    {
        UnsubscribeLanguageChanged();
    }

    private void OnDestroy()
    {
        UnregisterListeners();
        UnsubscribeLanguageChanged();
    }

    public void Show()
    {
        ResolveReferences();
        Root.SetActive(true);
        pageIndex = 0;
        selectedTopicIndex = GetFirstTopicIndexOnCurrentPage();
        RefreshAll();
        SelectTopic(selectedTopicIndex);
    }

    public void Hide()
    {
        Root.SetActive(false);
    }

    public void NextPage()
    {
        if (!CanMoveNextPage())
            return;

        pageIndex++;
        selectedTopicIndex = GetFirstTopicIndexOnCurrentPage();
        RefreshAll();
        SelectTopic(selectedTopicIndex);
    }

    public void PrevPage()
    {
        if (pageIndex <= 0)
            return;

        pageIndex--;
        selectedTopicIndex = GetFirstTopicIndexOnCurrentPage();
        RefreshAll();
        SelectTopic(selectedTopicIndex);
    }

    public void SelectTopic(int topicIndex)
    {
        if (topicIndex < 0 || topicIndex >= topics.Count)
        {
            selectedTopicIndex = -1;
            if (bodyText != null)
                bodyText.text = "";

            ResetBodyScrollToTop();
            RefreshTopicButtonVisuals();
            return;
        }

        selectedTopicIndex = topicIndex;

        HelpTopic topic = topics[topicIndex];
        if (bodyText != null)
            bodyText.text = GetLocalizedText(topic != null ? topic.bodyTextKey : "");

        RefreshTopicButtonVisuals();
        ResetBodyScrollToTop();
    }

    private void RefreshAll()
    {
        RefreshMenuPage();

        if (selectedTopicIndex >= 0 && selectedTopicIndex < topics.Count)
        {
            HelpTopic topic = topics[selectedTopicIndex];
            if (bodyText != null)
                bodyText.text = GetLocalizedText(topic != null ? topic.bodyTextKey : "");
        }
    }

    private void RefreshMenuPage()
    {
        int pageSize = Mathf.Max(1, topicsPerPage);
        int pageStart = pageIndex * pageSize;

        if (topicButtons != null)
        {
            for (int i = 0; i < topicButtons.Length; i++)
            {
                Button button = topicButtons[i];
                TMP_Text label = GetTopicButtonText(i, button);
                int topicIndex = pageStart + i;
                bool hasTopic = topicIndex >= 0 && topicIndex < topics.Count;

                if (button != null)
                {
                    button.gameObject.SetActive(hasTopic);
                    button.interactable = hasTopic;
                    button.onClick.RemoveAllListeners();

                    if (hasTopic)
                    {
                        int capturedIndex = topicIndex;
                        button.onClick.AddListener(() => SelectTopic(capturedIndex));
                    }
                }

                if (label != null)
                    label.text = hasTopic ? GetLocalizedText(topics[topicIndex].titleTextKey) : "";
            }
        }

        if (leftButton != null)
            leftButton.interactable = pageIndex > 0;

        if (rightButton != null)
            rightButton.interactable = CanMoveNextPage();

        RefreshTopicButtonVisuals();
    }

    private void RefreshTopicButtonVisuals()
    {
        if (topicButtons == null)
            return;

        int pageStart = pageIndex * Mathf.Max(1, topicsPerPage);
        for (int i = 0; i < topicButtons.Length; i++)
        {
            Button button = topicButtons[i];
            if (button == null || !button.gameObject.activeSelf)
                continue;

            int topicIndex = pageStart + i;
            button.interactable = topicIndex >= 0 && topicIndex < topics.Count;
        }
    }

    private TMP_Text GetTopicButtonText(int index, Button button)
    {
        if (topicButtonTexts != null && index >= 0 && index < topicButtonTexts.Length && topicButtonTexts[index] != null)
            return topicButtonTexts[index];

        if (button != null)
            return button.GetComponentInChildren<TMP_Text>(true);

        return null;
    }

    private int GetFirstTopicIndexOnCurrentPage()
    {
        int topicIndex = pageIndex * Mathf.Max(1, topicsPerPage);
        return topicIndex < topics.Count ? topicIndex : -1;
    }

    private bool CanMoveNextPage()
    {
        int pageSize = Mathf.Max(1, topicsPerPage);
        return (pageIndex + 1) * pageSize < topics.Count;
    }

    private void ResetBodyScrollToTop()
    {
        Canvas.ForceUpdateCanvases();

        if (bodyScrollRect != null)
            bodyScrollRect.verticalNormalizedPosition = 1f;

        if (bodyScrollbar != null)
            bodyScrollbar.value = 1f;

        if (resetScrollRoutine != null)
            StopCoroutine(resetScrollRoutine);

        resetScrollRoutine = StartCoroutine(ResetBodyScrollToTopNextFrame());
    }

    private IEnumerator ResetBodyScrollToTopNextFrame()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();

        if (bodyScrollRect != null)
            bodyScrollRect.verticalNormalizedPosition = 1f;

        if (bodyScrollbar != null)
            bodyScrollbar.value = 1f;

        resetScrollRoutine = null;
    }

    private string GetLocalizedText(string key)
    {
        if (string.IsNullOrEmpty(key))
            return "";

        string text = LocalizationManager.Instance != null
            ? LocalizationManager.Instance.GetText(key)
            : key;

        return string.IsNullOrEmpty(text) ? "" : text.Replace("\\n", "\n");
    }

    private void OnLanguageChanged()
    {
        if (Root.activeInHierarchy)
            RefreshAll();
    }

    private void ResolveReferences()
    {
        if (rootCanvas == null)
        {
            if (gameObject.name == "HelpCanvas")
                rootCanvas = gameObject;
            else
                rootCanvas = FindSceneGameObjectByName("HelpCanvas");
        }

        GameObject searchRoot = rootCanvas != null ? rootCanvas : gameObject;

        if (exitButton == null)
            exitButton = FindChildComponentByName<Button>(searchRoot, "ExitButton");

        if (leftButton == null)
            leftButton = FindChildComponentByName<Button>(searchRoot, "LeftButton");

        if (rightButton == null)
            rightButton = FindChildComponentByName<Button>(searchRoot, "RightButton");

        if (bodyText == null)
            bodyText = FindChildComponentByName<TMP_Text>(searchRoot, "TutorialText");

        if (bodyScrollRect == null)
            bodyScrollRect = searchRoot.GetComponentInChildren<ScrollRect>(true);

        if (bodyScrollbar == null)
            bodyScrollbar = FindChildComponentByName<Scrollbar>(searchRoot, "ScrollBar");

        if (topicButtons == null || topicButtons.Length == 0)
            topicButtons = FindTopicButtons(searchRoot);

        ResolveTopicButtonTexts();
    }

    private void ResolveTopicButtonTexts()
    {
        if (topicButtons == null)
            return;

        if (topicButtonTexts == null || topicButtonTexts.Length != topicButtons.Length)
            topicButtonTexts = new TMP_Text[topicButtons.Length];

        for (int i = 0; i < topicButtons.Length; i++)
        {
            if (topicButtonTexts[i] == null && topicButtons[i] != null)
                topicButtonTexts[i] = topicButtons[i].GetComponentInChildren<TMP_Text>(true);
        }
    }

    private Button[] FindTopicButtons(GameObject searchRoot)
    {
        List<Button> foundButtons = new List<Button>();
        string[] names =
        {
            "TutorialButton",
            "TutorialButton (1)",
            "TutorialButton (2)",
            "TutorialButton (3)"
        };

        for (int i = 0; i < names.Length; i++)
        {
            Button button = FindChildComponentByName<Button>(searchRoot, names[i]);
            if (button != null)
                foundButtons.Add(button);
        }

        return foundButtons.ToArray();
    }

    private void RegisterListeners()
    {
        if (exitButton != null)
        {
            exitButton.onClick.RemoveListener(Hide);
            exitButton.onClick.AddListener(Hide);
        }

        if (leftButton != null)
        {
            leftButton.onClick.RemoveListener(PrevPage);
            leftButton.onClick.AddListener(PrevPage);
        }

        if (rightButton != null)
        {
            rightButton.onClick.RemoveListener(NextPage);
            rightButton.onClick.AddListener(NextPage);
        }
    }

    private void UnregisterListeners()
    {
        if (exitButton != null)
            exitButton.onClick.RemoveListener(Hide);

        if (leftButton != null)
            leftButton.onClick.RemoveListener(PrevPage);

        if (rightButton != null)
            rightButton.onClick.RemoveListener(NextPage);
    }

    private void SubscribeLanguageChanged()
    {
        if (isSubscribedToLanguageChanged || LocalizationManager.Instance == null)
            return;

        LocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;
        isSubscribedToLanguageChanged = true;
    }

    private void UnsubscribeLanguageChanged()
    {
        if (!isSubscribedToLanguageChanged || LocalizationManager.Instance == null)
            return;

        LocalizationManager.Instance.OnLanguageChanged -= OnLanguageChanged;
        isSubscribedToLanguageChanged = false;
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

    private static GameObject FindSceneGameObjectByName(string objectName)
    {
        if (string.IsNullOrEmpty(objectName))
            return null;

        UnityEngine.SceneManagement.Scene scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
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
}
