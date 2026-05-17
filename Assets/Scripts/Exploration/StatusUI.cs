using UnityEngine;
using TMPro;
using System.Collections.Generic;

// Status UI 제어 코드
public class StatusUI : MonoBehaviour
{
    [Header("텍스트 연결")]
    public TextMeshProUGUI lvText;
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI apText; // 행동력
    public TextMeshProUGUI breakResText;
    public TextMeshProUGUI strText;
    public TextMeshProUGUI defText;
    public TextMeshProUGUI spdText;
    public TextMeshProUGUI lukText;

    private void OnEnable()
    {
        UpdateStatsUI();
    }

    private void UpdateStatsUI()
    {
        if (PlayerManager.Instance == null) return;

        bool isInCombat = (CombatManager.Instance != null);

        PlayerStats baseStats = PlayerManager.Instance.stats;
        PlayerStats itemStats = PlayerManager.Instance.GetItemModifiedStats();

        // 전투 중이라면 스냅샷 스탯을 가져오고, 아니라면 현재 아이템 스탯을 사용
        PlayerStats combatBase = isInCombat ? (CombatManager.Instance.GetCurrentPlayerStats() ?? itemStats) : itemStats;

        // 통합 문자열 생성기로 모든 스탯을 한 번에 처리합니다!
        strText.text = GetComprehensiveStatString("Str", TargetStat.Strength, baseStats.strength, combatBase.strength, isInCombat);
        defText.text = GetComprehensiveStatString("Def", TargetStat.Defense, baseStats.defense, combatBase.defense, isInCombat);
        spdText.text = GetComprehensiveStatString("Spd", TargetStat.Speed, baseStats.speed, combatBase.speed, isInCombat);
        lukText.text = GetComprehensiveStatString("Luk", TargetStat.Luck, baseStats.luck, combatBase.luck, isInCombat);
        breakResText.text = GetComprehensiveStatString("BR", TargetStat.BreakResistance, baseStats.breakResistance, combatBase.breakResistance, isInCombat);

        if (isInCombat)
        {
            // AP는 전투 중 시스템 소모가 크므로 괄호 없이 원본만 표기
            apText.text = $"{combatBase.ActionPoints}";
            hpText.text = $"{combatBase.currentHp} / {combatBase.maxHp}";
            lvText.text = $"{combatBase.level} ({combatBase.currentExp} / {combatBase.maxExp})";
        }
        else
        {
            // 탐색 씬에서는 AP도 아이템 보정 공식 출력
            apText.text = GetComprehensiveStatString("AP", TargetStat.Strength, baseStats.ActionPoints, itemStats.ActionPoints, false);

            int bonusHp = itemStats.maxHp - baseStats.maxHp;
            string hpCalc = bonusHp > 0 ? $" <size=70%><color=#AAAAAA>[{baseStats.maxHp} <color=#00FF00>+ {bonusHp}</color>]</color></size>" : "";
            hpText.text = $"{itemStats.currentHp} / {itemStats.maxHp}{hpCalc}";

            lvText.text = $"{baseStats.level} ({baseStats.currentExp} / {baseStats.maxExp})";
        }
    }

    // =========================================================
    //  궁극의 스탯 분해기 (아이템 + 시너지 + 전투 버프 통합 연산)
    // =========================================================
    private string GetComprehensiveStatString(string statType, TargetStat targetStat, int baseVal, int itemModifiedVal, bool isInCombat)
    {
        // -------------------------------------
        // 1. 아이템 & 시너지 연산 (탐색 씬 기준)
        // -------------------------------------
        int flat = 0;
        float pct = 0f;

        foreach (var item in PlayerManager.Instance.inventory)
        {
            int sl = item.starLevel;
            if (statType == "Str") { flat += item.data.GetFlatStr(sl); pct += item.data.GetPctStr(sl); }
            else if (statType == "Def") { flat += item.data.GetFlatDef(sl); pct += item.data.GetPctDef(sl); }
            else if (statType == "Spd") { flat += item.data.GetFlatSpd(sl); pct += item.data.GetPctSpd(sl); }
            else if (statType == "Luk") { flat += item.data.GetFlatLuck(sl); pct += item.data.GetPctLuck(sl); }
            else if (statType == "AP") { flat += item.data.GetFlatAP(sl); pct += item.data.GetPctAP(sl); }
            else if (statType == "BR") { flat += item.data.GetFlatBR(sl); }
        }

        var syn = PlayerManager.Instance.GetCurrentSynergies();

        float[] loneWolfAmps = { 0f, 0.05f, 0.10f, 0.20f, 0.40f, 0.75f, 1.30f, 2.00f };
        int rejectCount = Mathf.Clamp(PlayerManager.Instance.stats.rejectedSupporterCount, 0, 7);
        pct += loneWolfAmps[rejectCount];

        if (statType == "Str" && syn.GetValueOrDefault(ItemClass.Saber) >= 2) pct += 0.15f;
        if (statType == "Def" && syn.GetValueOrDefault(ItemClass.Shielder) >= 2) pct += 0.20f;
        if (statType == "Spd" && syn.GetValueOrDefault(ItemClass.Boxer) >= 2) pct += 0.20f;
        if (statType == "Luk" && syn.GetValueOrDefault(ItemClass.Gunner) >= 2) pct += 0.15f;
        if (statType == "AP" && syn.GetValueOrDefault(ItemClass.Assassin) >= 2) pct += 0.15f;
        if (statType == "BR" && syn.GetValueOrDefault(ItemClass.Beast) >= 4) pct += 0.20f;

        int calcVal = Mathf.Max(1, Mathf.RoundToInt((baseVal + flat) * (1f + pct)));
        int conversion = itemModifiedVal - calcVal;

        // 아이템용 포맷팅 (초록/주황)
        string flatText = flat > 0 ? $" <color=#00FF00>+ {flat}</color>" : (flat < 0 ? $" <color=#00FF00>- {Mathf.Abs(flat)}</color>" : "");
        string convText = conversion > 0 ? $" <color=#FFA500>+ {conversion}</color>" : "";
        float displayItemPct = (1f + pct);

        bool hasItemMods = (flat != 0 || pct != 0f || conversion != 0);

        // -------------------------------------
        // 2. 전투 중 버프 연산 (전투 씬 기준)
        // -------------------------------------
        int flatBuff = 0;
        float pctBuff = 0f;

        if (isInCombat && BuffManager.Instance != null)
        {
            var effects = BuffManager.Instance.GetEffects(true);
            foreach (var eff in effects)
            {
                if (eff.effectData != null && eff.effectData.targetStat == targetStat)
                {
                    if (eff.effectData.modifierType == ModifierType.Percentage) pctBuff += eff.value;
                    else flatBuff += Mathf.RoundToInt(eff.value);
                }
            }
        }

        bool hasCombatMods = (flatBuff != 0 || pctBuff != 0f);

        // -------------------------------------
        // 3. 최종 출력 조립 
        // -------------------------------------
        if (!isInCombat)
        {
            // 탐색 씬: 아무 보정이 없으면 원본만, 있으면 [ (기본 + 초록) * 초록 + 주황 ]
            if (!hasItemMods) return $"{itemModifiedVal}";
            return $"{itemModifiedVal} <size=70%><color=#AAAAAA>[({baseVal}{flatText}) * <color=#00FF00>{displayItemPct:F2}</color>{convText}]</color></size>";
        }
        else
        {
            // 전투 씬: 아이템 스탯 기반에 전투 버프 덧씌우기
            int finalCombatRaw = Mathf.Max(1, Mathf.RoundToInt((itemModifiedVal + flatBuff) * (1f + pctBuff)));

            // 보정이 아예 없으면 원본만!
            if (!hasItemMods && !hasCombatMods) return $"{finalCombatRaw}";

            string buffFlatText = flatBuff > 0 ? $" <color=#FF4444>+ {flatBuff}</color>" : (flatBuff < 0 ? $" <color=#FF4444>- {Mathf.Abs(flatBuff)}</color>" : "");
            float displayBuffPct = (1f + pctBuff);

            // Case A: 아이템 보정은 없고 전투 버프만 걸렸을 때
            if (!hasItemMods)
            {
                return $"{finalCombatRaw} <size=70%><color=#AAAAAA>[({baseVal}{buffFlatText}) * <color=#FF4444>{displayBuffPct:F2}</color>]</color></size>";
            }

            // Case B: 전투 버프는 없고 아이템 보정만 있을 때
            if (!hasCombatMods)
            {
                return $"{itemModifiedVal} <size=70%><color=#AAAAAA>[({baseVal}{flatText}) * <color=#00FF00>{displayItemPct:F2}</color>{convText}]</color></size>";
            }

            // Case C: 아이템 보정도 있고 전투 버프도 걸려있을 때 (궁극의 2중 괄호 포맷)
            // 포맷: 최종값 [ { (기본 + 아이템합) * 아이템곱 + 전환 } + 버프합 ] * 버프곱
            return $"{finalCombatRaw} <size=70%><color=#AAAAAA>[ {{({baseVal}{flatText}) * <color=#00FF00>{displayItemPct:F2}</color>{convText}}} {buffFlatText} ] * <color=#FF4444>{displayBuffPct:F2}</color></color></size>";
        }
    }
}