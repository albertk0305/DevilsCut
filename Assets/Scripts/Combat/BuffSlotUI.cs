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

        // 1. 기본 이름 및 설명 출력
        StringBuilder sb = new StringBuilder();
        sb.Append($"<b>{data.effectName}</b> : {data.baseDescription}");

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

        // 수치 포맷팅 정보가 있다면 총합 수치 노출
        if (!string.IsNullOrEmpty(data.valueFormat) && displayTotal != 0f)
        {
            float finalPrintValue = data.modifierType == ModifierType.Percentage ? displayTotal * 100f : displayTotal;
            string sign = finalPrintValue > 0 ? "+" : "";
            sb.Append(" ").Append(string.Format(data.valueFormat, $"{sign}{finalPrintValue:F0}"));
        }

        // [수정] 1. 영구 패시브일 경우 깔끔하게 고유 문구 출력 후 종료
        if (data.isPermanentPassive)
        {
            sb.Append("\n\n<color=#FFD700>[ 영구 귀속 스탯 ]</color>");
            //sb.Append("\n<color=#DDDDDD><size=80%>* 장비, 시너지, 고유 특성이 반영된 캐릭터의 기본 스탯입니다.</size></color>");
            //sb.Append("\n<color=#DDDDDD><size=80%>* 전투 중 스킬로 발생한 일시적 버프는 별도로 합산됩니다.</size></color>");
        }
        // [수정] 2. 스택 추적이 켜진 버프/디버프(예: 혈액 저주)만 낱개 리스트를 출력하도록 제한!
        else if (stackCount > 0 && data.showStackDetails)
        {
            sb.Append($"\n[적용 중인 중첩: {stackCount}개]");
            for (int i = 0; i < myStacks.Count; i++)
            {
                sb.Append("\n - ");

                if (myStacks[i].value != 0f)
                {
                    float displayVal = data.modifierType == ModifierType.Percentage ? myStacks[i].value * 100f : myStacks[i].value;
                    string sign = displayVal > 0 ? "+" : "";
                    string unit = data.modifierType == ModifierType.Percentage ? "%" : "";

                    sb.Append($"수치: {sign}{displayVal:F0}{unit} | ");
                }
                sb.Append($"남은 시간: {myStacks[i].turnsLeft}턴");
            }
        }
        else
        {
            // 스택 추적이 필요 없는 일반 지속시간제 버프는 가장 짧은 남은 턴수 하나만 심플하게 보여줍니다.
            int minTurn = int.MaxValue;
            foreach (var stack in myStacks) if (stack.turnsLeft < minTurn) minTurn = stack.turnsLeft;
            if (minTurn != int.MaxValue) sb.Append($"\n(지속 시간: {minTurn}턴 남음)");
        }

        clickMessage = sb.ToString();
    }

    public void OnSlotClicked()
    {
        // 1. 현재 턴 주인이 플레이어(true)가 아니면 클릭을 무시합니다.
        if (CombatManager.Instance != null && !CombatManager.Instance.IsCurrentTurnOwner(true)) return;

        // 2. 전투 코멘터리 텍스트(카린 대사 역할)로 상세 정보를 띄워줍니다.
        if (CombatUIManager.Instance != null)
        {
            CombatUIManager.Instance.InterruptAndTypeCommentary(clickMessage);
        }
    }
}