using UnityEngine;
using UnityEngine.UI;
using System.Text;

public class BuffSlotUI : MonoBehaviour
{
    public Image iconImage;
    public Button slotButton;
    public Sprite overLimitSprite;

    private string clickMessage;

    public void Setup(StatusEffectData data, float totalValue, bool isPlayer, bool isOverLimit = false)
    {
        if (!gameObject.activeSelf) gameObject.SetActive(true);

        if (isOverLimit)
        {
            iconImage.sprite = overLimitSprite;
            slotButton.interactable = false;
            return;
        }

        iconImage.sprite = data.icon;
        slotButton.interactable = true;

        var allEffects = BuffManager.Instance.GetEffects(isPlayer);
        var myStacks = allEffects.FindAll(e => e.effectData == data);
        int stackCount = myStacks.Count;

        StringBuilder sb = new StringBuilder();

        float displayTotal = totalValue;

        if (data.modifierType == ModifierType.Percentage && data.targetStat != TargetStat.None)
        {
            if (data.targetStat == TargetStat.Defense)
            {
                displayTotal = Mathf.Clamp(displayTotal, -0.8f, 2.0f);
            }
            else
            {
                displayTotal = Mathf.Max(-0.9f, displayTotal);
            }
        }

        bool hasDisplayValue = Mathf.Abs(displayTotal) > 0.0001f;
        string formattedDisplayValue = hasDisplayValue ? FormatEffectValue(data, displayTotal) : "";

        if (stackCount > 0 && data.showStackDetails)
        {
            sb.Append($"{data.effectName} [Active Stacks: {stackCount}]");
            sb.Append("\n");

            for (int i = 0; i < myStacks.Count; i++)
            {
                if (i > 0) sb.Append(" ");

                bool hasStackValue = Mathf.Abs(myStacks[i].value) > 0.0001f;
                string durationText = GetDurationText(data, myStacks[i].turnsLeft);

                if (hasStackValue)
                {
                    sb.Append($"({FormatEffectValue(data, myStacks[i].value)} / {durationText})");
                }
                else
                {
                    sb.Append($"({durationText})");
                }
            }
        }
        else if (data.isPermanentPassive)
        {
            sb.Append($"{data.effectName} [Permanent Effect]");

            if (hasDisplayValue)
            {
                sb.Append($" (Current Value: {formattedDisplayValue})");
            }
        }
        else
        {
            int minTurn = int.MaxValue;
            foreach (var stack in myStacks) if (stack.turnsLeft < minTurn) minTurn = stack.turnsLeft;
            string durationText = minTurn != int.MaxValue ? GetDurationText(data, minTurn) : "0 turns";

            sb.Append(data.effectName);

            if (hasDisplayValue)
            {
                sb.Append($" (Current Value: {formattedDisplayValue} / {durationText})");
            }
            else
            {
                sb.Append($" ({durationText})");
            }
        }

        clickMessage = sb.ToString();
    }

    private string FormatEffectValue(StatusEffectData data, float value)
    {
        bool isPercentage = ShouldDisplayAsPercentage(data);
        float displayValue = ShouldScalePercentageValue(data) ? value * 100f : value;
        string sign = displayValue > 0f ? "+" : "";
        string unit = isPercentage ? "%" : "";
        string numberFormat = Mathf.Abs(displayValue - Mathf.Round(displayValue)) < 0.001f ? "F0" : "F1";

        return $"{sign}{displayValue.ToString(numberFormat)}{unit}";
    }

    private string GetDurationText(StatusEffectData data, int turnsLeft)
    {
        if (data != null && data.isPermanentPassive) return "Permanent";
        if (turnsLeft >= 999) return "Permanent";
        return turnsLeft == 1 ? "1 turn" : $"{turnsLeft} turns";
    }

    private bool ShouldDisplayAsPercentage(StatusEffectData data)
    {
        if (data == null) return false;
        if (data.modifierType == ModifierType.Percentage) return true;

        switch (data.specialType)
        {
            case SpecialEffectType.DamageAmp:
            case SpecialEffectType.DamageReduction:
            case SpecialEffectType.DamageGivenAmp:
            case SpecialEffectType.CritRateUp:
            case SpecialEffectType.CritDamageUp:
            case SpecialEffectType.AccuracyUp:
            case SpecialEffectType.EvasionUp:
                return true;
            default:
                return false;
        }
    }

    private bool ShouldScalePercentageValue(StatusEffectData data)
    {
        if (data == null) return false;
        if (data.specialType == SpecialEffectType.AccuracyUp ||
        data.specialType == SpecialEffectType.EvasionUp ||
        data.specialType == SpecialEffectType.CritRateUp)
        {
            return false;
        }

        if (data.modifierType == ModifierType.Percentage) return true;

        switch (data.specialType)
        {
            case SpecialEffectType.DamageAmp:
            case SpecialEffectType.DamageReduction:
            case SpecialEffectType.DamageGivenAmp:
            case SpecialEffectType.CritDamageUp:
                return true;
            default:
                return false;
        }
    }

    public void OnSlotClicked()
    {
        // 1. 현재 턴 주인이 플레이어(true)가 아니면 클릭을 무시합니다.
        if (CombatManager.Instance == null || !CombatManager.Instance.CanInteractWithCombatUI)
            return;

        // 2. 전투 코멘터리 텍스트(카린 대사 역할)로 상세 정보를 띄워줍니다.
        if (CombatUIManager.Instance != null)
        {
            CombatUIManager.Instance.InterruptAndTypeCommentary(clickMessage);
        }
    }
}
