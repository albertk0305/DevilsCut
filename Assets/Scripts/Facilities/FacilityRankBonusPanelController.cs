using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class RankBonusView
{
    public GameObject root;
    public GameObject achievedBorder;
    public TMP_Text descriptionText;
}

public class FacilityRankBonusPanelController : MonoBehaviour
{
    [SerializeField] private RankBonusView[] rankBonusViews;
    [SerializeField] private Button returnButton;

    private float previousTimeScale = 1f;
    private bool isOpen;

    private void Awake()
    {
        if (returnButton != null)
        {
            returnButton.onClick.RemoveListener(Close);
            returnButton.onClick.AddListener(Close);
        }
    }

    private void OnDisable()
    {
        if (isOpen)
            RestoreTimeScale();
    }

    public void Open(int currentRank, FacilityRankBonusInfo info)
    {
        if (!isOpen)
        {
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            isOpen = true;
        }

        gameObject.SetActive(true);
        Refresh(currentRank, info);
    }

    public void Close()
    {
        RestoreTimeScale();
        gameObject.SetActive(false);
    }

    private void Refresh(int currentRank, FacilityRankBonusInfo info)
    {
        int rankIndex = Mathf.Clamp(currentRank, 0, 3);

        if (rankBonusViews == null)
        {
            DevLog.LogWarning("[FacilityRankBonusPanel] rankBonusViews is not assigned.");
            return;
        }

        for (int i = 0; i < rankBonusViews.Length; i++)
        {
            RankBonusView view = rankBonusViews[i];
            if (view == null)
                continue;

            int requiredRank = i + 1;

            if (view.root != null)
                view.root.SetActive(true);

            if (view.achievedBorder != null)
                view.achievedBorder.SetActive(rankIndex >= requiredRank);

            if (view.descriptionText != null)
                view.descriptionText.text = GetRankDescription(info, requiredRank);
        }
    }

    private string GetRankDescription(FacilityRankBonusInfo info, int rank)
    {
        if (info == null)
        {
            DevLog.LogWarning("[FacilityRankBonusPanel] FacilityRankBonusInfo is not assigned.");
            return "";
        }

        if (info.rankDescriptions == null || rank < 0 || rank >= info.rankDescriptions.Length)
        {
            DevLog.LogWarning($"[FacilityRankBonusPanel] rankDescriptions is missing rank {rank}. facilityID={info.facilityID}");
            return "";
        }

        string descriptionKey = info.rankDescriptions[rank];
        if (string.IsNullOrEmpty(descriptionKey))
            return "";

        if (LocalizationManager.Instance == null)
            return descriptionKey;

        string localized = LocalizationManager.Instance.GetText(descriptionKey);
        if (string.IsNullOrEmpty(localized))
            return descriptionKey;

        return localized;
    }

    private void RestoreTimeScale()
    {
        if (!isOpen)
            return;

        Time.timeScale = previousTimeScale;
        isOpen = false;
    }
}
