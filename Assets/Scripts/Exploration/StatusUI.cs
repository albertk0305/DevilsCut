using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class StatusUI : MonoBehaviour
{
    [Header("텍스트 연결")]
    public TextMeshProUGUI lvText;
    public TextMeshProUGUI hpText;
    public TextMeshProUGUI apText;
    public TextMeshProUGUI breakResText;
    public TextMeshProUGUI strText;
    public TextMeshProUGUI defText;
    public TextMeshProUGUI spdText;
    public TextMeshProUGUI lukText;

    public TextMeshProUGUI maxBreakGaugeText;

    private ClearRecordPlayerProfile previewProfile;

    private void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        UpdateStatsUI();
    }

    private void UpdateStatsUI()
    {
        if (previewProfile != null)
        {
            UpdatePreviewStatsUI();
            return;
        }

        if (PlayerManager.Instance == null) return;

        bool isVictoryResult = CombatVictoryUIController.IsVictoryUIActive;
        bool useCombatStats = CombatManager.Instance != null && !isVictoryResult;

        PlayerStats baseStats = PlayerManager.Instance.stats;
        PlayerStats itemStats = PlayerManager.Instance.GetItemModifiedStats();

        // Combat uses the battle snapshot; exploration uses current item-modified stats.
        PlayerStats displayStats = useCombatStats ? (CombatManager.Instance.GetCurrentPlayerStats() ?? itemStats) : itemStats;

        strText.text = GetComprehensiveStatString("Str", TargetStat.Strength, baseStats.strength, displayStats.strength, useCombatStats);
        defText.text = GetComprehensiveStatString("Def", TargetStat.Defense, baseStats.defense, displayStats.defense, useCombatStats);
        spdText.text = GetComprehensiveStatString("Spd", TargetStat.Speed, baseStats.speed, displayStats.speed, useCombatStats);
        lukText.text = GetComprehensiveStatString("Luk", TargetStat.Luck, baseStats.luck, displayStats.luck, useCombatStats);
        breakResText.text = GetComprehensiveStatString("BR", TargetStat.BreakResistance, baseStats.breakResistance, displayStats.breakResistance, useCombatStats);
        if (maxBreakGaugeText != null) maxBreakGaugeText.text = $"{displayStats.maxBreakGauge}";

        if (useCombatStats)
        {
            // Combat AP is volatile, so show the raw snapshot value.
            apText.text = $"{displayStats.ActionPoints}";
            int currentHp = Mathf.Clamp(displayStats.currentHp, 0, displayStats.maxHp);
            string hpDisplay = $"{currentHp} / {displayStats.maxHp}";
            hpText.text = hpDisplay;
            lvText.text = $"{displayStats.level} ({displayStats.currentExp} / {displayStats.maxExp})";
        }
        else
        {
            // Exploration AP can show the item formula.
            apText.text = GetComprehensiveStatString("AP", TargetStat.Strength, baseStats.ActionPoints, itemStats.ActionPoints, false);

            int currentHp = Mathf.Clamp(baseStats.currentHp, 0, itemStats.maxHp);
            int bonusHp = itemStats.maxHp - baseStats.maxHp;
            string hpCalc = bonusHp > 0 ? $" <size=70%><color=#AAAAAA>[{baseStats.maxHp} <color=#00FF00>+ {bonusHp}</color>]</color></size>" : "";
            string hpDisplay = $"{currentHp} / {itemStats.maxHp}{hpCalc}";
            hpText.text = hpDisplay;

            lvText.text = $"{baseStats.level} ({baseStats.currentExp} / {baseStats.maxExp})";
        }
    }

    public void SetPreviewProfile(ClearRecordPlayerProfile profile)
    {
        previewProfile = profile;

        if (isActiveAndEnabled)
            Refresh();
    }

    public void ClearPreviewProfile()
    {
        previewProfile = null;
    }

    private void UpdatePreviewStatsUI()
    {
        PlayerStats baseStats = previewProfile.GetBaseStats();
        PlayerStats itemStats = previewProfile.GetItemModifiedStats();

        strText.text = GetComprehensiveStatString("Str", TargetStat.Strength, baseStats.strength, itemStats.strength, false, previewProfile.Inventory, previewProfile.GetPreviewSynergies(), previewProfile.GetRejectedSupporterCount());
        defText.text = GetComprehensiveStatString("Def", TargetStat.Defense, baseStats.defense, itemStats.defense, false, previewProfile.Inventory, previewProfile.GetPreviewSynergies(), previewProfile.GetRejectedSupporterCount());
        spdText.text = GetComprehensiveStatString("Spd", TargetStat.Speed, baseStats.speed, itemStats.speed, false, previewProfile.Inventory, previewProfile.GetPreviewSynergies(), previewProfile.GetRejectedSupporterCount());
        lukText.text = GetComprehensiveStatString("Luk", TargetStat.Luck, baseStats.luck, itemStats.luck, false, previewProfile.Inventory, previewProfile.GetPreviewSynergies(), previewProfile.GetRejectedSupporterCount());
        breakResText.text = GetComprehensiveStatString("BR", TargetStat.BreakResistance, baseStats.breakResistance, itemStats.breakResistance, false, previewProfile.Inventory, previewProfile.GetPreviewSynergies(), previewProfile.GetRejectedSupporterCount());
        if (maxBreakGaugeText != null) maxBreakGaugeText.text = $"{itemStats.maxBreakGauge}";

        apText.text = GetComprehensiveStatString("AP", TargetStat.Strength, baseStats.ActionPoints, itemStats.ActionPoints, false, previewProfile.Inventory, previewProfile.GetPreviewSynergies(), previewProfile.GetRejectedSupporterCount());

        int currentHp = Mathf.Clamp(baseStats.currentHp, 0, itemStats.maxHp);
        int bonusHp = itemStats.maxHp - baseStats.maxHp;
        string hpCalc = bonusHp > 0 ? $" <size=70%><color=#AAAAAA>[{baseStats.maxHp} <color=#00FF00>+ {bonusHp}</color>]</color></size>" : "";
        string hpDisplay = $"{currentHp} / {itemStats.maxHp}{hpCalc}";
        hpText.text = hpDisplay;

        lvText.text = $"{baseStats.level} ({baseStats.currentExp} / {baseStats.maxExp})";
    }

    // Builds the stat formula from item, synergy, and combat buff modifiers.
    private string GetComprehensiveStatString(string statType, TargetStat targetStat, int baseVal, int itemModifiedVal, bool isInCombat)
    {
        List<OwnedItem> inventory = PlayerManager.Instance != null ? PlayerManager.Instance.inventory : null;
        Dictionary<ItemClass, int> synergies = PlayerManager.Instance != null ? PlayerManager.Instance.GetCurrentSynergies() : null;
        int rejectedSupporterCount = PlayerManager.Instance != null ? PlayerManager.Instance.stats.rejectedSupporterCount : 0;
        return GetComprehensiveStatString(statType, targetStat, baseVal, itemModifiedVal, isInCombat, inventory, synergies, rejectedSupporterCount);
    }

    private string GetComprehensiveStatString(
        string statType,
        TargetStat targetStat,
        int baseVal,
        int itemModifiedVal,
        bool isInCombat,
        IReadOnlyList<OwnedItem> inventory,
        Dictionary<ItemClass, int> synergies,
        int rejectedSupporterCount)
    {
        int flat = 0;
        float pct = 0f;

        foreach (var item in inventory ?? new List<OwnedItem>())
        {
            if (item == null || item.data == null)
                continue;

            int sl = item.starLevel;
            if (statType == "Str") { flat += item.data.GetFlatStr(sl); pct += item.data.GetPctStr(sl); }
            else if (statType == "Def") { flat += item.data.GetFlatDef(sl); pct += item.data.GetPctDef(sl); }
            else if (statType == "Spd") { flat += item.data.GetFlatSpd(sl); pct += item.data.GetPctSpd(sl); }
            else if (statType == "Luk") { flat += item.data.GetFlatLuck(sl); pct += item.data.GetPctLuck(sl); }
            else if (statType == "AP") { flat += item.data.GetFlatAP(sl); pct += item.data.GetPctAP(sl); }
            else if (statType == "BR") { flat += item.data.GetFlatBR(sl); }
        }

        Dictionary<ItemClass, int> syn = synergies ?? new Dictionary<ItemClass, int>();

        float[] loneWolfAmps = { 0f, 0.05f, 0.10f, 0.20f, 0.40f, 0.75f, 1.30f, 2.00f };
        int rejectCount = Mathf.Clamp(rejectedSupporterCount, 0, 7);
        pct += loneWolfAmps[rejectCount];

        if (statType == "Str" && syn.GetValueOrDefault(ItemClass.Saber) >= 2) pct += 0.15f;
        if (statType == "Def" && syn.GetValueOrDefault(ItemClass.Shielder) >= 2) pct += 0.20f;
        if (statType == "Spd" && syn.GetValueOrDefault(ItemClass.Boxer) >= 2) pct += 0.20f;
        if (statType == "Luk" && syn.GetValueOrDefault(ItemClass.Gunner) >= 2) pct += 0.15f;
        if (statType == "AP" && syn.GetValueOrDefault(ItemClass.Assassin) >= 2) pct += 0.15f;
        if (statType == "BR" && syn.GetValueOrDefault(ItemClass.Beast) >= 4) pct += 0.20f;

        int calcVal = Mathf.Max(1, Mathf.RoundToInt((baseVal + flat) * (1f + pct)));
        int conversion = itemModifiedVal - calcVal;

        string flatText = flat > 0 ? $" <color=#00FF00>+ {flat}</color>" : (flat < 0 ? $" <color=#00FF00>- {Mathf.Abs(flat)}</color>" : "");
        string convText = conversion > 0 ? $" <color=#FFA500>+ {conversion}</color>" : "";
        float displayItemPct = (1f + pct);

        bool hasItemMods = (flat != 0 || pct != 0f || conversion != 0);

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

        if (!isInCombat)
        {
            // Exploration: raw value unless item/synergy modifiers exist.
            if (!hasItemMods) return $"{itemModifiedVal}";
            return $"{itemModifiedVal} <size=70%><color=#AAAAAA>[({baseVal}{flatText}) * <color=#00FF00>{displayItemPct:F2}</color>{convText}]</color></size>";
        }
        else
        {
            // Combat: item-modified stats plus temporary battle buffs.
            int finalCombatRaw = Mathf.Max(1, Mathf.RoundToInt((itemModifiedVal + flatBuff) * (1f + pctBuff)));

            if (!hasItemMods && !hasCombatMods) return $"{finalCombatRaw}";

            string buffFlatText = flatBuff > 0 ? $" <color=#FF4444>+ {flatBuff}</color>" : (flatBuff < 0 ? $" <color=#FF4444>- {Mathf.Abs(flatBuff)}</color>" : "");
            float displayBuffPct = (1f + pctBuff);

            if (!hasItemMods)
            {
                return $"{finalCombatRaw} <size=70%><color=#AAAAAA>[({baseVal}{buffFlatText}) * <color=#FF4444>{displayBuffPct:F2}</color>]</color></size>";
            }

            if (!hasCombatMods)
            {
                return $"{itemModifiedVal} <size=70%><color=#AAAAAA>[({baseVal}{flatText}) * <color=#00FF00>{displayItemPct:F2}</color>{convText}]</color></size>";
            }

            // Combined formula: item modifiers first, then combat buffs.
            return $"{finalCombatRaw} <size=70%><color=#AAAAAA>[ {{({baseVal}{flatText}) * <color=#00FF00>{displayItemPct:F2}</color>{convText}}} {buffFlatText} ] * <color=#FF4444>{displayBuffPct:F2}</color></color></size>";
        }
    }
}
