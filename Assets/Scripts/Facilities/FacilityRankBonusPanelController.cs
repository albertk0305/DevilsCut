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
    public Image iconImage;
}

public class FacilityRankBonusPanelController : MonoBehaviour
{
    [SerializeField] private Image currentRankImage;
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
        Sprite currentSprite = GetRankSprite(info, rankIndex);

        if (currentRankImage != null)
        {
            currentRankImage.sprite = currentSprite;
            currentRankImage.gameObject.SetActive(currentSprite != null);
        }

        if (rankBonusViews == null)
            return;

        for (int i = 0; i < rankBonusViews.Length; i++)
        {
            RankBonusView view = rankBonusViews[i];
            if (view == null)
                continue;

            if (view.root != null)
                view.root.SetActive(true);

            if (view.achievedBorder != null)
                view.achievedBorder.SetActive(i <= rankIndex);

            if (view.descriptionText != null)
                view.descriptionText.text = GetRankDescription(info, i);

            if (view.iconImage != null)
            {
                Sprite rankSprite = GetRankSprite(info, i);
                view.iconImage.sprite = rankSprite;
                view.iconImage.gameObject.SetActive(rankSprite != null);
            }
        }
    }

    private Sprite GetRankSprite(FacilityRankBonusInfo info, int rank)
    {
        if (info == null || info.rankSprites == null || rank < 0 || rank >= info.rankSprites.Length)
            return null;

        return info.rankSprites[rank];
    }

    private string GetRankDescription(FacilityRankBonusInfo info, int rank)
    {
        if (info == null || info.rankDescriptions == null || rank < 0 || rank >= info.rankDescriptions.Length)
            return "";

        return info.rankDescriptions[rank] ?? "";
    }

    private void RestoreTimeScale()
    {
        if (!isOpen)
            return;

        Time.timeScale = previousTimeScale;
        isOpen = false;
    }
}
