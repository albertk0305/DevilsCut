using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class EntityStatusUI : MonoBehaviour
{
    public Image profileImage;
    public Slider hpSlider;
    public Slider breakSlider;
    public TextMeshProUGUI hpText;
    public BuffSlotUI[] buffSlots;

    private Sprite defaultSprite;

    [Header("그로기 게이지 연출")]
    public Image breakGaugeFill; // 인스펙터에서 브레이크 슬라이더의 Fill 이미지 할당
    public Sprite normalBreakSprite; // 평소 게이지 이미지 (ex: 노란색/하얀색 바)
    public Sprite brokenGroggySprite; // 그로기 터졌을 때 전용 이미지 (공용)

    public void InitUI(int maxHp, int currentHp, Sprite sprite)
    {
        defaultSprite = sprite;
        if (profileImage != null) profileImage.sprite = sprite;
        if (hpSlider != null) { hpSlider.maxValue = maxHp; hpSlider.value = currentHp; }
        if (hpText != null) hpText.text = $"{currentHp}/{maxHp}";
        if (breakSlider != null) { breakSlider.maxValue = 100; breakSlider.value = 0; }
    }

    public void UpdateHP(int currentHp, int maxHp)
    {
        if (hpSlider != null) hpSlider.value = currentHp;
        if (hpText != null) hpText.text = $"{currentHp}/{maxHp}";
    }

    public void UpdateBreak(float breakValue)
    {
        if (breakSlider != null) breakSlider.value = breakValue;
    }

    public void SetProfileImage(Sprite sprite)
    {
        if (profileImage != null && sprite != null) profileImage.sprite = sprite;
    }

    public void ResetProfileImage()
    {
        if (profileImage != null) profileImage.sprite = defaultSprite;
    }

    public void UpdateBuffs(Dictionary<StatusEffectData, float> groupedEffects, bool isPlayer)
    {
        int maxSlots = buffSlots.Length;
        int activeCount = groupedEffects.Count;
        int index = 0;

        foreach (var kvp in groupedEffects)
        {
            if (index < maxSlots - 1) buffSlots[index].Setup(kvp.Key, kvp.Value, isPlayer);
            else if (index == maxSlots - 1)
            {
                if (activeCount > maxSlots) buffSlots[index].Setup(null, 0, isPlayer, true);
                else buffSlots[index].Setup(kvp.Key, kvp.Value, isPlayer);
                break;
            }
            index++;
        }

        for (int i = index; i < maxSlots; i++)
        {
            if (buffSlots[i].gameObject.activeSelf) buffSlots[i].gameObject.SetActive(false);
        }
    }

    public void SetBreakGaugeState(bool isBroken)
    {
        if (breakGaugeFill != null && brokenGroggySprite != null && normalBreakSprite != null)
        {
            breakGaugeFill.sprite = isBroken ? brokenGroggySprite : normalBreakSprite;
        }
    }
}