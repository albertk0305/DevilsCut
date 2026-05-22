using System;
using System.Collections.Generic;

public class CombatActionMenuController
{
    private enum MenuState { Hidden, CategorySelect, SkillSelect }

    private readonly CombatUIManager uiManager;
    private readonly AnalysisUI analysisUI;
    private readonly Func<EnemyData> getCurrentEnemyData;
    private readonly Action<SkillData, bool, bool> executeSkillFromActionMenu;

    private MenuState currentMenuState = MenuState.Hidden;
    private SkillCategory selectedCategory;
    private List<SkillData> currentDisplaySkills = new List<SkillData>();

    private readonly List<SkillCategory> categoryMenuOrder = new List<SkillCategory>
    {
        SkillCategory.Sword,
        SkillCategory.Gun,
        SkillCategory.Martial,
        SkillCategory.Magic,
        SkillCategory.Oni
    };

    public CombatActionMenuController(
        CombatUIManager uiManager,
        AnalysisUI analysisUI,
        Func<EnemyData> getCurrentEnemyData,
        Action<SkillData, bool, bool> executeSkillFromActionMenu)
    {
        this.uiManager = uiManager;
        this.analysisUI = analysisUI;
        this.getCurrentEnemyData = getCurrentEnemyData;
        this.executeSkillFromActionMenu = executeSkillFromActionMenu;
    }

    public bool IsPlayerSelectingPhase =>
        currentMenuState == MenuState.CategorySelect || currentMenuState == MenuState.SkillSelect;

    public SkillCategory SelectedCategory => selectedCategory;

    public void HideActionMenuAndShowWaiting()
    {
        if (uiManager != null)
        {
            uiManager.SetActionPanelActive(false);
            uiManager.SetWaitingPanelActive(true);
        }

        currentMenuState = MenuState.Hidden;
    }

    public void ShowCategoryMenu()
    {
        if (uiManager == null) return;

        uiManager.SetActionPanelActive(true);
        currentMenuState = MenuState.CategorySelect;

        string[] keys = new string[categoryMenuOrder.Count];
        for (int i = 0; i < categoryMenuOrder.Count; i++)
        {
            keys[i] = GetCategoryLocalizationKey(categoryMenuOrder[i]);
        }

        uiManager.UpdateActionButtonsForCategory(keys);
    }

    public void ShowSkillMenu(int categoryIndex)
    {
        if (uiManager == null) return;
        if (categoryIndex < 0 || categoryIndex >= categoryMenuOrder.Count) return;

        currentMenuState = MenuState.SkillSelect;
        selectedCategory = categoryMenuOrder[categoryIndex];

        currentDisplaySkills = PlayerManager.Instance != null
            ? PlayerManager.Instance.GetSkillsByCategory(selectedCategory)
            : new List<SkillData>();

        StyleRank currentRank = StyleRankManager.Instance != null
            ? StyleRankManager.Instance.currentRank
            : StyleRank.None;

        uiManager.UpdateActionButtonsForSkills(currentDisplaySkills, currentRank);
    }

    public void OnActionSlotClicked(int slotIndex)
    {
        if (currentMenuState == MenuState.CategorySelect)
        {
            ShowSkillMenu(slotIndex);
            return;
        }

        if (currentMenuState == MenuState.SkillSelect)
        {
            if (slotIndex == 4)
            {
                ShowCategoryMenu();
                return;
            }

            if (slotIndex >= 0 && slotIndex < currentDisplaySkills.Count)
            {
                bool isUltimate = slotIndex == 3;
                ExecuteSkill(currentDisplaySkills[slotIndex], true, isUltimate);
            }
        }
    }

    public void ToggleAnalysis()
    {
        if (!IsPlayerSelectingPhase) return;
        if (analysisUI == null) return;

        if (analysisUI.gameObject.activeSelf)
            analysisUI.Close();
        else
            analysisUI.Open(getCurrentEnemyData());
    }

    private void ExecuteSkill(SkillData skill, bool isPlayerAttacking, bool isUltimate = false)
    {
        HideActionMenuAndShowWaiting();
        executeSkillFromActionMenu?.Invoke(skill, isPlayerAttacking, isUltimate);
    }

    private string GetCategoryLocalizationKey(SkillCategory category)
    {
        return category switch
        {
            SkillCategory.Sword => "cat_sword",
            SkillCategory.Gun => "cat_gun",
            SkillCategory.Martial => "cat_martial",
            SkillCategory.Magic => "cat_magic",
            SkillCategory.Oni => "cat_oni",
            _ => "cat_unknown"
        };
    }
}