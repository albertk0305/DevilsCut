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

        // 1. 기본 설명
        clickMessage = $"{data.effectName} : {data.baseDescription}";

        float displayTotal = totalValue;

        // 내부 스탯 연산과 동일하게 UI 표기에도 상한/하한선을 적용합니다.
        if (data.modifierType == ModifierType.Percentage && data.targetStat != TargetStat.None)
        {
            if (data.targetStat == TargetStat.Defense)
            {
                // 방어력 증감은 -80% ~ +200% 사이로 표기 제한
                displayTotal = Mathf.Clamp(displayTotal, -0.8f, 2.0f);
            }
            else
            {
                // 기타 스탯 감소는 최대 -80% 까지만 표기 제한
                displayTotal = Mathf.Max(-0.8f, displayTotal);
            }
        }

        StringBuilder sb = new StringBuilder();
        sb.Append($"{data.effectName} : {data.baseDescription}");

        // 기존의 totalValue > 0 조건을 displayTotal != 0f 로 변경하여 음수(디버프)도 출력되게 합니다!
        if (!string.IsNullOrEmpty(data.valueFormat) && displayTotal != 0f)
        {
            float finalPrintValue = data.modifierType == ModifierType.Percentage ? displayTotal * 100f : displayTotal;
            string sign = finalPrintValue > 0 ? "+" : "";
            sb.Append(" ").Append(string.Format(data.valueFormat, $"{sign}{finalPrintValue}"));
        }

        if (stackCount > 0)
        {
            sb.Append($"\n[적용 중인 스택: {stackCount}개]");
            for (int i = 0; i < myStacks.Count; i++)
            {
                sb.Append("\n - ");

                if (myStacks[i].value != 0f)
                {
                    float displayVal = data.modifierType == ModifierType.Percentage ? myStacks[i].value * 100f : myStacks[i].value;
                    string sign = displayVal > 0 ? "+" : "";
                    string unit = data.modifierType == ModifierType.Percentage ? "%" : "";

                    sb.Append($"수치: {sign}{displayVal}{unit} | ");
                }
                sb.Append($"남은 시간: {myStacks[i].turnsLeft}턴");
            }
        }
        clickMessage = sb.ToString();
    }

    // 버튼의 OnClick 이벤트
    public void OnSlotClicked()
    {
        // 버튼이 눌리는 순간에 현재 전투 상태 검사 (플레이어 턴 메뉴 선택 중일 때만)
        if (CombatManager.Instance.IsPlayerSelectingPhase)
        {
            // 버튼 잠금 없이, 즉시 Interrupt(강제 중단 및 덮어쓰기) 함수만 호출합니다!
            CombatUIManager.Instance.InterruptAndTypeCommentary(clickMessage);
        }
    }
}