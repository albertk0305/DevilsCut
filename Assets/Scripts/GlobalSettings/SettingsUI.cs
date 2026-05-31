using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    [Header("UI 연결")]
    public GameObject goToMainButton;
    public GameObject confirmationPopup;
    public Toggle fastCombatToggle;
    public GameObject restartBattleButton;

    [Header("씬 이름 설정")]
    public string mainMenuSceneName = "MainMenu";
    public string battleSceneName = "Battle";

    private float timeScaleBeforePause = 1f;

    private void OnEnable()
    {
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
    }

    public void OpenSettings()
    {
        timeScaleBeforePause = Time.timeScale;
        if (timeScaleBeforePause <= 0) timeScaleBeforePause = 1f;

        Time.timeScale = 0f;
        DevLog.Log($"[Settings] Opened: time paused (restore scale: {timeScaleBeforePause})");
        gameObject.SetActive(true);
    }

    public void CloseSettings()
    {
        Time.timeScale = timeScaleBeforePause;
        DevLog.Log("[Settings] Closed.");
        gameObject.SetActive(false);
    }

    public void ShowConfirmation() { if (confirmationPopup != null) confirmationPopup.SetActive(true); }
    public void HideConfirmation() { if (confirmationPopup != null) confirmationPopup.SetActive(false); }

    public void GoToMainMenu()
    {
        DevLog.Log("[Settings] Returning to main menu.");
        confirmationPopup.SetActive(false);
        gameObject.SetActive(false);
        Time.timeScale = 1f;
        SceneLoader.LoadScene(mainMenuSceneName);
    }

    public void OnFastCombatToggleChanged(bool isOn)
    {
        float targetSpeed = isOn ? 2.0f : 1.0f;

        PlayerPrefs.SetInt("FastCombat", isOn ? 1 : 0);
        PlayerPrefs.Save();

        if (CombatManager.Instance != null)
        {
            if (Time.timeScale == 0f)
            {
                timeScaleBeforePause = targetSpeed;
            }
            else
            {
                Time.timeScale = targetSpeed;
            }

            if (CombatUIManager.Instance != null)
                CombatUIManager.Instance.UpdateFastCombatIcon(isOn);
        }
        else
        {
            // Keep non-combat scenes at normal speed when closing settings.
            timeScaleBeforePause = 1.0f;
        }
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

        gameObject.SetActive(false);

        CombatManager.Instance.RestorePlayerHpToBattleStart();

        Time.timeScale = 1f;
        SceneLoader.LoadScene(battleSceneName);
    }
}
