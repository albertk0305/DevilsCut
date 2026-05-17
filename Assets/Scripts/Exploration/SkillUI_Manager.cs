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
    }

    public void RefreshSkillCanvas()
    {
        if (PlayerManager.Instance == null || allSkillSlots.Count != 20) return;

        int slotIndex = 0;

        // 카테고리 순서대로 스킬을 4개씩 가져와서 슬롯에 끼워 넣습니다.
        foreach (SkillCategory cat in categoryOrder)
        {
            List<SkillData> catSkills = PlayerManager.Instance.GetSkillsByCategory(cat);

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
        descriptionText.text = "확인할 스킬을 선택해 주세요.";
    }

    // 슬롯에서 클릭 이벤트가 들어왔을 때 호출됨
    public void ShowSkillDescription(SkillData skill)
    {
        if (LocalizationManager.Instance == null) return;

        // 1. [스킬명] [Lv.X]
        string skillName = LocalizationManager.Instance.GetText(skill.skillNameKey);
        string levelStr = $"[Lv.{skill.skillLevel}]";

        // 2. 진화명 및 사용할 설명 Key 스위칭
        string evoStr = "";
        string descKeyToUse = skill.skillDescKey; // 기본 상태일 땐 기본 설명 Key 사용

        if (skill.currentEvolution != SkillEvolution.None)
        {
            string evoNameKey = "";

            // 진화 상태에 따라 사용할 이름 Key와 설명 Key를 덮어씌웁니다.
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

            // [수정] 컬러 태그(<color>)를 제거하여 스킬 이름/레벨과 동일한 기본 색상으로 통일합니다.
            if (!string.IsNullOrEmpty(evoNameKey))
            {
                evoStr = $" [{LocalizationManager.Instance.GetText(evoNameKey)}]";
            }
        }

        // 3. 결정된 Key(기본 or 진화)를 바탕으로 스킬 설명 본문 번역 가져오기
        string desc = LocalizationManager.Instance.GetText(descKeyToUse);

        // 4. 타이핑 효과 없이 즉시 텍스트 출력!
        // 형식: [스킬명] [Lv.X] [진화명] \n\n 스킬 설명
        descriptionText.text = $"<b>[{skillName}] {levelStr}{evoStr}</b>\n\n{desc}";
    }
}