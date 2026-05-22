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
            return $"{attackerName}이(가) {skillName}을(를) 시전합니다!";

        if (!result.anyHit)
            return $"{attackerName}의 {skillName}이(가) 빗나갔습니다!";

        if (result.anyCrit)
            return $"{attackerName}의 {skillName} 치명적으로 적중!";

        return $"{attackerName}의 {skillName} 적중!";
    }

    public void EnqueueUltimateCutIn(Sprite cutInSprite, string attackerName)
    {
        if (cutInSprite == null) return;
        if (uiManager == null || visualizer == null) return;

        visualizer.EnqueueAction(() =>
            uiManager.InterruptAndTypeCommentary($"{attackerName}의 필살기!"));

        visualizer.EnqueueCutIn(cutInSprite);
    }

    public void ClearCombatEffects()
    {
        uiManager?.ClearCombatEffects();
    }

    public void ShowFantasticDreamerDiceIfNeeded(
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
    bool showCritAlert)
{
    if (uiManager == null) return;

    uiManager.SetDefenderImage(defenderIsPlayer, defenderReactionSprite);
    uiManager.InterruptAndTypeCommentary(commentary);

    if (showCritAlert)
    {
        uiManager.StartCoroutine(uiManager.ShowCritAlert());
    }
}
}