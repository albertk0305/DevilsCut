using UnityEngine;
using UnityEngine.SceneManagement;

public class FacilitySceneControllerBase : MonoBehaviour
{
    [SerializeField] protected string fallbackFacilityID;
    [SerializeField] protected string returnSceneName = "Exploration";

    protected string FacilityID { get; private set; }
    protected int CurrentRank { get; private set; }

    protected virtual void Start()
    {
        ResolveFacilityContext();
    }

    protected virtual void ResolveFacilityContext()
    {
        PlayerManager playerManager = PlayerManager.Instance;

        if (playerManager != null && playerManager.HasCurrentFacilityVisit())
            FacilityID = playerManager.currentFacilityID;
        else
            FacilityID = fallbackFacilityID;

        if (string.IsNullOrEmpty(FacilityID))
        {
            CurrentRank = 0;
            DevLog.LogWarning("[FacilityScene] FacilityID is missing. Assign fallbackFacilityID or enter from Exploration.");
            return;
        }

        CurrentRank = playerManager != null ? playerManager.GetSavedFacilityRank(FacilityID) : 0;
        DevLog.Log($"[FacilityScene] Facility context resolved. facilityID={FacilityID}, rank={CurrentRank}");
    }

    public virtual void ReturnToExploration()
    {
        PlayerManager.Instance?.ClearCurrentFacilityVisit();
        SceneManager.LoadScene(returnSceneName);
    }
}
