using System.Collections.Generic;
using UnityEngine;

public sealed class CombatPresentationDirector
{
    private readonly CombatUIManager uiManager;
    private readonly BattleVisualizer visualizer;

    public CombatPresentationDirector(CombatUIManager uiManager, BattleVisualizer visualizer)
    {
        this.uiManager = uiManager;
        this.visualizer = visualizer;
    }

    public void UpdateTurnOrder(List<Sprite> icons)
    {
        uiManager?.UpdateTurnOrderUI(icons);
    }

    public string BuildSkillCommentary(
        string attackerName,
        string skillName,
        SkillResult result,
        bool isPureUtility)
    {
        if (isPureUtility)
            return FormatLocalizedText("combat_comment_skill_utility_format", "{0:이가} {1:을를} 시전합니다.", attackerName, skillName);

        if (!result.anyHit)
            return FormatLocalizedText("combat_comment_skill_miss_format", "{0}의 {1:이가} 빗나갔습니다!", attackerName, skillName);

        if (result.anyCrit)
            return FormatLocalizedText("combat_comment_skill_crit_format", "{0}의 {1} 치명적으로 적중!", attackerName, skillName);

        return FormatLocalizedText("combat_comment_skill_hit_format", "{0}의 {1} 적중!", attackerName, skillName);
    }

    public void EnqueueUltimateCutIn(Sprite cutInSprite, string attackerName)
    {
        if (cutInSprite == null) return;
        if (uiManager == null || visualizer == null) return;

        visualizer.EnqueueAction(() =>
            uiManager.InterruptAndTypeLocalizedCommentary("combat_comment_ultimate_format", "{0}의 필살기!", attackerName));

        visualizer.EnqueueCutIn(cutInSprite);
    }

    public void ClearCombatEffects()
    {
        uiManager?.ClearCombatEffects();
    }

    public void ShowSpecialCastPresentationIfNeeded(
        SkillData skill,
        bool isPlayerAttacking)
    {
        if (uiManager == null || skill == null) return;

        if (skill.skillLogic == null) return;

        if (skill.skillLogic.TryGetSpecialCastPresentation(skill, out int count))
        {
            uiManager.ShowFantasticDreamerDice(count, isPlayerAttacking);
        }
    }

    public void SetCasterImage(bool isPlayerAttacking, Sprite actionImage)
    {
        uiManager?.SetCasterImage(isPlayerAttacking, actionImage);
    }

    public void ShowCastResultPresentation(
        bool defenderIsPlayer,
        Sprite defenderReactionSprite,
        string commentary,
        bool showCritAlert,
        string commentaryKey = null,
        string commentaryFallback = null,
        object[] commentaryArgs = null)
    {
        if (uiManager == null) return;

        uiManager.SetDefenderImage(defenderIsPlayer, defenderReactionSprite);
        if (!string.IsNullOrEmpty(commentaryKey))
            uiManager.InterruptAndTypeLocalizedCommentary(commentaryKey, commentaryFallback, commentaryArgs);
        else
            uiManager.InterruptAndTypeCommentary(commentary);

        if (showCritAlert)
        {
            uiManager.StartCoroutine(uiManager.ShowCritAlert());
        }
    }

    private string GetLocalizedText(string key, string fallback)
    {
        if (!string.IsNullOrEmpty(key) && LocalizationManager.Instance != null)
        {
            string localized = LocalizationManager.Instance.GetText(key);
            if (!string.IsNullOrEmpty(localized) && localized != key)
                return localized;
        }

        if (!string.IsNullOrEmpty(fallback))
            return fallback;

        return key ?? "";
    }

    private string FormatLocalizedText(string key, string fallback, params object[] args)
    {
        string format = GetLocalizedText(key, fallback);
        try
        {
            return KoreanParticleFormatter.Format(format, args);
        }
        catch (System.FormatException)
        {
            try
            {
                return KoreanParticleFormatter.Format(fallback, args);
            }
            catch (System.FormatException)
            {
                return fallback ?? "";
            }
        }
    }
}
