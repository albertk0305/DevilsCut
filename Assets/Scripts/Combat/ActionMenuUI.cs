using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ActionMenuUI : MonoBehaviour
{
    public GameObject actionUIPanel;
    public GameObject waitingPanel;
    public Button[] actionButtons;
    public LocalizedText[] actionButtonTexts;

    public Image styleRankImage;
    public Sprite[] styleRankSprites;

    public void SetActionPanelActive(bool isActive) => actionUIPanel.SetActive(isActive);
    public void SetWaitingPanelActive(bool isActive) => waitingPanel.SetActive(isActive);

    public void UpdateCategoryButtons(string[] categoryKeys)
    {
        for (int i = 0; i < 5; i++)
        {
            actionButtons[i].gameObject.SetActive(true);
            actionButtons[i].interactable = true;
            actionButtonTexts[i].SetKey(categoryKeys[i]);
        }
    }

    public void UpdateSkillButtons(List<SkillData> skills, StyleRank currentRank)
    {
        for (int i = 0; i < 4; i++)
        {
            if (i < skills.Count)
            {
                actionButtons[i].gameObject.SetActive(true);
                actionButtonTexts[i].SetKey(skills[i].skillNameKey);
                actionButtons[i].interactable = (i == 3) ? (currentRank == StyleRank.SSS) : true;
            }
            else
            {
                actionButtons[i].gameObject.SetActive(false);
            }
        }
        actionButtons[4].gameObject.SetActive(true);
        actionButtons[4].interactable = true;
        actionButtonTexts[4].SetKey("btn_cancel");
    }

    public void UpdateStyleRank(StyleRank rank)
    {
        int rankIndex = (int)rank;
        if (rank == StyleRank.None || styleRankSprites.Length <= rankIndex || styleRankSprites[rankIndex] == null)
            styleRankImage.gameObject.SetActive(false);
        else
        {
            styleRankImage.gameObject.SetActive(true);
            styleRankImage.sprite = styleRankSprites[rankIndex];
        }
    }
}