using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class AnalysisUI : MonoBehaviour
{
    [Header("UI 요소")]
    public TextMeshProUGUI nameText;

    [Header("스탯 텍스트 (순수 수치+공식만 표기)")]
    [Tooltip("0:BR, 1:AP, 2:Str, 3:Def, 4:Spd, 5:Luk")]
    public TextMeshProUGUI[] statTexts;

    [Header("미리 배치된 8개의 스킬 버튼 목록")]
    public Button[] staticSkillButtons;

    public void Open(EnemyData enemy)
    {
        if (enemy == null) return;

        gameObject.SetActive(true);

        // 1. 창이 열릴 때 단 한 번만 최신 스탯 및 스킬 정보를 세팅합니다. (최적화)
        UpdateStats(enemy);
        SetupSkills(enemy);

        // 2. 유니티 UI 레이아웃 압축 버그를 막기 위해 1프레임 뒤에 리빌드합니다.
        StartCoroutine(RefreshLayoutRoutine(enemy));
    }

    private IEnumerator RefreshLayoutRoutine(EnemyData enemy)
    {
        // 오브젝트가 켜지고 텍스트가 주입된 후, UI가 크기를 정상적으로 계산하도록 1프레임 대기
        yield return null;

        if (staticSkillButtons != null && staticSkillButtons.Length > 0 && staticSkillButtons[0] != null)
        {
            RectTransform parentRect = staticSkillButtons[0].transform.parent.GetComponent<RectTransform>();
            if (parentRect != null)
            {
                // 부모 레이아웃 그룹(Vertical/Grid 등) 강제 새로고침 -> 글자가 뿅 하고 나타남
                LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
            }
        }
        SetupSkills(enemy);
    }

    // =========================================================
    //  적군 전용 스탯 분해기 (창 열릴 때 1회 연산)
    // =========================================================
    private void UpdateStats(EnemyData enemy)
    {
        nameText.text = GetTranslatedString(enemy.enemyNameKey);

        statTexts[0].text = GetEnemyComprehensiveStatString(TargetStat.BreakResistance, enemy.breakResistance);
        statTexts[1].text = GetEnemyComprehensiveStatString(TargetStat.AP, enemy.ActionPoints);
        statTexts[2].text = GetEnemyComprehensiveStatString(TargetStat.Strength, enemy.strength);
        statTexts[3].text = GetEnemyComprehensiveStatString(TargetStat.Defense, enemy.defense);
        statTexts[4].text = GetEnemyComprehensiveStatString(TargetStat.Speed, enemy.speed);
        statTexts[5].text = GetEnemyComprehensiveStatString(TargetStat.Luck, enemy.luck);
    }

    private string GetEnemyComprehensiveStatString(TargetStat targetStat, int baseVal)
    {
        int flatBuff = 0;
        float pctBuff = 0f;

        if (BuffManager.Instance != null)
        {
            var effects = BuffManager.Instance.GetEffects(false);
            foreach (var eff in effects)
            {
                if (eff.effectData != null && eff.effectData.targetStat == targetStat)
                {
                    if (eff.effectData.modifierType == ModifierType.Percentage)
                        pctBuff += eff.value;
                    else
                        flatBuff += Mathf.RoundToInt(eff.value);
                }
            }
        }

        pctBuff = Mathf.Max(-0.9f, pctBuff);

        bool hasCombatMods = (flatBuff != 0 || Mathf.Abs(pctBuff) > 0.001f);

        int finalStat = Mathf.RoundToInt((baseVal + flatBuff) * (1f + pctBuff));
        finalStat = Mathf.Max(1, finalStat);

        // AP는 수치만, 다른 스탯은 버프가 없을 경우 수치만 반환
        if (targetStat == TargetStat.AP) return $"{finalStat}";
        if (!hasCombatMods) return $"{finalStat}";

        string buffFlatText = flatBuff > 0 ? $" <color=#FF4444>+ {flatBuff}</color>" : (flatBuff < 0 ? $" <color=#FF4444>- {Mathf.Abs(flatBuff)}</color>" : "");
        float displayBuffPct = (1f + pctBuff);

        return $"{finalStat} <size=75%><color=#AAAAAA>[({baseVal}{buffFlatText}) * <color=#FF4444>{displayBuffPct:F2}</color>]</color></size>";
    }

    // =========================================================
    //  정적 버튼 8개 매핑 및 번역 
    // =========================================================
    private void SetupSkills(EnemyData enemy)
    {
        if (staticSkillButtons == null || staticSkillButtons.Length == 0) return;

        // 전부 비활성화 초기화
        for (int i = 0; i < staticSkillButtons.Length; i++)
        {
            if (staticSkillButtons[i] != null) staticSkillButtons[i].gameObject.SetActive(false);
        }

        if (enemy.aiBrain == null) return;

        List<SkillData> enemySkills = enemy.aiBrain.GetEnemySkills();
        int activeButtonsCount = Mathf.Min(enemySkills.Count, staticSkillButtons.Length);

        for (int i = 0; i < activeButtonsCount; i++)
        {
            SkillData skill = enemySkills[i];
            Button btn = staticSkillButtons[i];

            if (skill == null || btn == null) continue;

            btn.gameObject.SetActive(true);

            TextMeshProUGUI btnText = btn.GetComponentInChildren<TextMeshProUGUI>(true);
            if (btnText != null)
            {
                btnText.gameObject.SetActive(true);
                btnText.text = GetTranslatedString(skill.skillNameKey);
            }

            btn.onClick.RemoveAllListeners();
            SkillData capturedSkill = skill;
            btn.onClick.AddListener(() => ShowSkillDesc(capturedSkill));
        }
    }

    private void ShowSkillDesc(SkillData skill)
    {
        string desc = GetTranslatedString(skill.skillDescKey);

        if (CombatUIManager.Instance != null)
        {
            CombatUIManager.Instance.InterruptAndTypeCommentary(desc);
        }
    }

    private string GetTranslatedString(string key)
    {
        if (string.IsNullOrEmpty(key)) return "Unknown";
        if (LocalizationManager.Instance != null)
        {
            string localizedText = LocalizationManager.Instance.GetText(key);
            if (!string.IsNullOrEmpty(localizedText)) return localizedText;
        }
        return key;
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }
}