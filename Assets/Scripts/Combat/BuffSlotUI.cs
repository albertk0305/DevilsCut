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
        string effectName = GetLocalizedText(data != null ? data.effectName : null, data != null ? data.effectName : "");

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
            sb.Append(FormatLocalizedText("combat_buff_active_stacks_format", "{0} [Active Stacks: {1}]", effectName, stackCount));
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
            sb.Append(FormatLocalizedText("combat_buff_permanent_effect_format", "{0} [Permanent Effect]", effectName));

            if (hasDisplayValue)
            {
                sb.Append(FormatLocalizedText("combat_buff_current_value_suffix_format", " (Current Value: {0})", formattedDisplayValue));
            }
        }
        else
        {
            int minTurn = int.MaxValue;
            foreach (var stack in myStacks) if (stack.turnsLeft < minTurn) minTurn = stack.turnsLeft;
            string durationText = minTurn != int.MaxValue ? GetDurationText(data, minTurn) : FormatLocalizedText("combat_buff_turns_format", "{0} turns", 0);

            sb.Append(effectName);

            if (hasDisplayValue)
            {
                sb.Append(FormatLocalizedText("combat_buff_current_value_duration_suffix_format", " (Current Value: {0} / {1})", formattedDisplayValue, durationText));
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
        if (data != null && data.isPermanentPassive) return GetLocalizedText("combat_buff_permanent", "Permanent");
        if (turnsLeft >= 999) return GetLocalizedText("combat_buff_permanent", "Permanent");
        return turnsLeft == 1
            ? GetLocalizedText("combat_buff_one_turn", "1 turn")
            : FormatLocalizedText("combat_buff_turns_format", "{0} turns", turnsLeft);
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
        if (ShouldDisplayRawPercentPointValue(data))
            return false;

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

    private bool ShouldDisplayRawPercentPointValue(StatusEffectData data)
    {
        if (data == null) return false;

        if (data.specialType == SpecialEffectType.AccuracyUp ||
            data.specialType == SpecialEffectType.EvasionUp ||
            data.specialType == SpecialEffectType.CritRateUp)
            return true;

        return data.effectID == "PermAccuracyBuff" ||
               data.effectID == "PermEvasionBuff";
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
