using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SkillUI_Manager : MonoBehaviour
{
    [Header("스킬 슬롯 리스트 (순서대로 20개 끌어다 넣기)")]
    // [0~3]: 검술, [4~7]: 총, [8~11]: 격투, [12~15]: 마법, [16~19]: 오니
    public List<SkillUI_Slot> allSkillSlots;

    [Header("하단 텍스트 UI")]
    public TextMeshProUGUI descriptionText;

    private const string SelectSkillPromptKey = "ui_select_skill_prompt";
    private ClearRecordPlayerProfile previewProfile;

    // 카테고리 순서 고정 (검 -> 총 -> 격투 -> 마법 -> 오니)
    private readonly SkillCategory[] categoryOrder = new SkillCategory[]
    {
        SkillCategory.Sword,
        SkillCategory.Gun,
        SkillCategory.Martial,
        SkillCategory.Magic,
        SkillCategory.Oni
    };

    // 프리팹이 켜질 때마다 스킬 정보를 최신화합니다.
    private void OnEnable()
    {
        RefreshSkillCanvas();

        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged += RefreshCurrentText;
    }

    private void OnDisable()
    {
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged -= RefreshCurrentText;
    }

    public void RefreshSkillCanvas()
    {
        if (allSkillSlots == null || allSkillSlots.Count != 20) return;
        if (previewProfile == null && PlayerManager.Instance == null) return;

        int slotIndex = 0;

        // 카테고리 순서대로 스킬을 4개씩 가져와서 슬롯에 끼워 넣습니다.
        foreach (SkillCategory cat in categoryOrder)
        {
            List<SkillData> catSkills = previewProfile != null
                ? previewProfile.GetSkillsByCategory(cat)
                : PlayerManager.Instance.GetSkillsByCategory(cat);

            for (int i = 0; i < 4; i++)
            {
                if (i < catSkills.Count)
                    allSkillSlots[slotIndex].InitSlot(catSkills[i], this);
                else
                    allSkillSlots[slotIndex].InitSlot(null, this); // 데이터가 부족하면 빈 슬롯 처리

                slotIndex++;
            }
        }

        // 창을 처음 열었을 때는 안내 문구 출력
        SetDefaultPrompt();
    }

    public void SetPreviewProfile(ClearRecordPlayerProfile profile)
    {
        if (previewProfile != null && previewProfile != profile)
            previewProfile.Dispose();

        previewProfile = profile;

        if (isActiveAndEnabled)
            RefreshSkillCanvas();
    }

    public void ClearPreviewProfile()
    {
        if (previewProfile != null)
        {
            previewProfile.Dispose();
            previewProfile = null;
        }
    }

    private void SetDefaultPrompt()
    {
        if (descriptionText == null)
            return;

        descriptionText.text = GetLocalizedText(SelectSkillPromptKey);
    }

    private void RefreshCurrentText()
    {
        SetDefaultPrompt();
    }

    // 슬롯에서 클릭 이벤트가 들어왔을 때 호출됨
    public void ShowSkillDescription(SkillData skill)
    {
        if (skill == null)
            return;

        string skillName = GetLocalizedText(skill.skillNameKey);
        string levelStr = $"[Lv.{skill.skillLevel}]";

        string evoStr = "";
        string descKeyToUse = skill.skillDescKey;

        if (skill.currentEvolution != SkillEvolution.None)
        {
            string evoNameKey = "";

            switch (skill.currentEvolution)
            {
                case SkillEvolution.PathA:
                    evoNameKey = skill.evolutionANameKey;
                    descKeyToUse = skill.evolutionADescKey;
                    break;
                case SkillEvolution.PathB:
                    evoNameKey = skill.evolutionBNameKey;
                    descKeyToUse = skill.evolutionBDescKey;
                    break;
                case SkillEvolution.PathC:
                    evoNameKey = skill.evolutionCNameKey;
                    descKeyToUse = skill.evolutionCDescKey;
                    break;
            }

            if (!string.IsNullOrEmpty(evoNameKey))
                evoStr = $" [{GetLocalizedText(evoNameKey)}]";
        }

        string desc = GetLocalizedText(descKeyToUse);
        descriptionText.text = $"<b>[{skillName}] {levelStr}{evoStr}</b>\n\n{desc}";
    }

    private string GetLocalizedText(string key)
    {
        return LocalizationManager.Instance != null
            ? LocalizationManager.Instance.GetText(key)
            : key;
    }
}
