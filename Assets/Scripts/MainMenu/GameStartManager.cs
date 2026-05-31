using UnityEngine;
using UnityEngine.SceneManagement;

public class GameStartManager : MonoBehaviour
{
    public static GameStartManager Instance;

    private const string StorySceneName = "Story";
    private const string NewGameDialogueID = "Prologue_A";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    public static GameStartManager GetOrCreateInstance()
    {
        if (Instance != null)
            return Instance;

        GameObject managerObject = new GameObject(nameof(GameStartManager));
        return managerObject.AddComponent<GameStartManager>();
    }

    public void StartNewGame()
    {
        DevLog.Log("[NewGame] Starting new game.");

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.CancelPendingContinueLoadRequest();
            SaveManager.Instance.DeleteCurrentSave();
        }
        else
        {
            DevLog.LogWarning("[NewGame] SaveManager missing; continuing without deleting existing save.");
        }

        if (PlayerManager.Instance != null)
            PlayerManager.Instance.ResetForNewGame();
        else
            DevLog.LogWarning("[NewGame] PlayerManager missing; skipping player reset.");

        if (ExplorationManager.Instance != null)
            ExplorationManager.Instance.ResetForNewGame();

        if (SaveManager.Instance != null)
            SaveManager.Instance.SaveContinueDataForDialogue(StorySceneName, NewGameDialogueID);

        DialogueRuntimeContext.SetPendingDialogueID(NewGameDialogueID);
        SceneLoader.LoadScene(StorySceneName);
    }
}
