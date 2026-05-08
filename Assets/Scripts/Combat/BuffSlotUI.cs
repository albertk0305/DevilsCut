using UnityEngine;
using UnityEngine.UI;

public class BuffSlotUI : MonoBehaviour
{
    public Image iconImage;
    public Button slotButton;
    public Sprite overLimitSprite;

    private string clickMessage;

    public void Setup(StatusEffectData data, float totalValue, bool isPlayer, bool isOverLimit = false)
    {
        gameObject.SetActive(true);

        if (isOverLimit)
        {
            iconImage.sprite = overLimitSprite;
            slotButton.interactable = false;
            return;
        }

        iconImage.sprite = data.icon;
        slotButton.interactable = true;

        var allEffects = isPlayer ? CombatManager.Instance.GetPlayerEffects() : CombatManager.Instance.GetEnemyEffects();

        var myStacks = allEffects.FindAll(e => e.effectData == data);
        int stackCount = myStacks.Count;

        // 1. 기본 설명
        clickMessage = $"{data.effectName} : {data.baseDescription}";

        // 2. 총합 수치 (기존 유지: "총 +50%" 등을 보여주기 위함)
        if (!string.IsNullOrEmpty(data.valueFormat) && totalValue > 0)
        {
            clickMessage += " " + string.Format(data.valueFormat, totalValue);
        }

        if (stackCount > 0)
        {
            clickMessage += $"\n[적용 중인 스택: {stackCount}개]";
            for (int i = 0; i < myStacks.Count; i++)
            {
                string detail = "\n - ";

                // 가드 같은 특수 효과(TargetStat.None)가 아닐 때만 '수치'를 보여줍니다.
                if (data.targetStat != TargetStat.None && myStacks[i].value > 0)
                {
                    // Percentage면 0.2를 20%로 변환, Flat이면 그대로 출력
                    float displayVal = data.modifierType == ModifierType.Percentage ? myStacks[i].value * 100f : myStacks[i].value;
                    string sign = displayVal > 0 ? "+" : ""; // 양수면 + 붙이기
                    string unit = data.modifierType == ModifierType.Percentage ? "%" : "";

                    detail += $"수치: {sign}{displayVal}{unit} | ";
                }

                // 모든 버프 공통으로 남은 턴수 출력
                detail += $"남은 시간: {myStacks[i].turnsLeft}턴";

                clickMessage += detail;
            }
        }
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