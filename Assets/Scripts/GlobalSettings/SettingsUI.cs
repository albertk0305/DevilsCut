using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    [Header("UI 연결")]
    public GameObject goToMainButton;
    public GameObject confirmationPopup;
    public Toggle fastCombatToggle;
    public Toggle storySkipToggle;
    public GameObject restartBattleButton;

    [Header("씬 이름 설정")]
    public string mainMenuSceneName = "MainMenu";
    public string battleSceneName = "Battle";

    private bool isReturningToMainMenu;

    private void OnEnable()
    {
        isReturningToMainMenu = false;

        string currentSceneName = SceneManager.GetActiveScene().name;
        if (currentSceneName == mainMenuSceneName)
        {
            if (goToMainButton != null) goToMainButton.SetActive(false);
        }
        else
        {
            if (goToMainButton != null) goToMainButton.SetActive(true);
        }

        bool isBattleScene = currentSceneName == battleSceneName && CombatManager.Instance != null;

        if (restartBattleButton != null)
            restartBattleButton.SetActive(isBattleScene);

        if (confirmationPopup != null) confirmationPopup.SetActive(false);

        if (fastCombatToggle != null)
        {
            bool isFast = PlayerPrefs.GetInt("FastCombat", 0) == 1;

            fastCombatToggle.onValueChanged.RemoveAllListeners();
            fastCombatToggle.isOn = isFast;
            fastCombatToggle.onValueChanged.AddListener(OnFastCombatToggleChanged);
        }

        if (storySkipToggle != null)
        {
            storySkipToggle.onValueChanged.RemoveListener(OnStorySkipToggleChanged);
            storySkipToggle.SetIsOnWithoutNotify(StorySkipSettings.Load());
            storySkipToggle.onValueChanged.AddListener(OnStorySkipToggleChanged);
        }
    }

    private void OnDisable()
    {
        TimeScalePauseManager.ReleasePause(this);
    }

    public void OpenSettings()
    {
        TimeScalePauseManager.RequestPause(this);
        DevLog.Log("[Settings] Opened: time paused");
        gameObject.SetActive(true);
    }

    public void CloseSettings()
    {
        TimeScalePauseManager.ReleasePause(this);
        DevLog.Log("[Settings] Closed.");
        gameObject.SetActive(false);
    }

    public void ShowConfirmation() { if (confirmationPopup != null) confirmationPopup.SetActive(true); }
    public void HideConfirmation() { if (confirmationPopup != null) confirmationPopup.SetActive(false); }

    public void GoToMainMenu()
    {
        if (isReturningToMainMenu)
            return;

        if (string.IsNullOrEmpty(mainMenuSceneName))
        {
            DevLog.LogWarning("[Settings] Return to main menu failed: main menu scene name is empty.");
            return;
        }

        isReturningToMainMenu = true;
        DevLog.Log("[Settings] Returning to main menu.");
        TimeScalePauseManager.ClearAllPauses();
        Time.timeScale = 1f;
        SceneLoader.LoadScene(mainMenuSceneName);
    }

    public void OnFastCombatToggleChanged(bool isOn)
    {
        PlayerPrefs.SetInt("FastCombat", isOn ? 1 : 0);
        PlayerPrefs.Save();

        if (CombatManager.Instance != null)
        {
            TimeScalePauseManager.ApplyGameplayTimeScale();

            if (CombatUIManager.Instance != null)
                CombatUIManager.Instance.UpdateFastCombatIcon(isOn);
        }
    }

    public void OnStorySkipToggleChanged(bool isOn)
    {
        StorySkipSettings.SetEnabled(isOn);
    }

    public void RestartBattle()
    {
        if (CombatManager.Instance == null)
        {
            DevLog.LogWarning("[Settings] Restart battle failed: CombatManager missing.");
            return;
        }

        DevLog.Log("[Settings] Restarting battle.");

        if (confirmationPopup != null)
            confirmationPopup.SetActive(false);

        TimeScalePauseManager.ReleasePause(this);
        gameObject.SetActive(false);

        CombatManager.Instance.RestorePlayerHpToBattleStart();

        Time.timeScale = 1f;
        SceneLoader.LoadScene(battleSceneName);
    }
}
