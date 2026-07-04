using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ClearRecordPlayerProfile : IDisposable
{
    private readonly List<SkillData> previewSkills = new List<SkillData>();
    private readonly List<OwnedItem> previewInventory = new List<OwnedItem>();
    private readonly List<SupporterData> previewUnlockedSupporters = new List<SupporterData>();
    private readonly List<KarinItemData> previewOwnedKarinItems = new List<KarinItemData>();
    private readonly Dictionary<string, SavedSupporterState> supporterStatesById = new Dictionary<string, SavedSupporterState>();
    private PlayerStats baseStats;
    private PlayerStats itemModifiedStats;

    public GameClearRecordData Record { get; }
    public PlayerGrowthSaveData PlayerGrowth => Record != null ? Record.playerGrowth : null;
    public string ClearId => Record != null ? Record.clearId : "";
    public int ClearNumber => Record != null ? Record.clearNumber : 0;
    public int Level => baseStats != null ? baseStats.level : 1;
    public int Exp => baseStats != null ? baseStats.currentExp : 0;
    public int MaxExp => baseStats != null ? baseStats.maxExp : 0;
    public int HP => itemModifiedStats != null ? itemModifiedStats.maxHp : (baseStats != null ? baseStats.maxHp : 0);
    public int AP => itemModifiedStats != null ? itemModifiedStats.ActionPoints : (baseStats != null ? baseStats.ActionPoints : 0);
    public int BreakResistance => itemModifiedStats != null ? itemModifiedStats.breakResistance : (baseStats != null ? baseStats.breakResistance : 0);
    public float MaxBreakGauge => itemModifiedStats != null ? itemModifiedStats.maxBreakGauge : (baseStats != null ? baseStats.maxBreakGauge : 0f);
    public int STR => itemModifiedStats != null ? itemModifiedStats.strength : (baseStats != null ? baseStats.strength : 0);
    public int DEF => itemModifiedStats != null ? itemModifiedStats.defense : (baseStats != null ? baseStats.defense : 0);
    public int SPD => itemModifiedStats != null ? itemModifiedStats.speed : (baseStats != null ? baseStats.speed : 0);
    public int LUK => itemModifiedStats != null ? itemModifiedStats.luck : (baseStats != null ? baseStats.luck : 0);
    public int Gold => baseStats != null ? baseStats.currentGold : 0;
    public IReadOnlyList<OwnedItem> Inventory => previewInventory;
    public IReadOnlyList<SupporterData> UnlockedSupporters => previewUnlockedSupporters;
    public IReadOnlyList<KarinItemData> OwnedKarinItems => previewOwnedKarinItems;
    public int RejectedSupporterCount => GetRejectedSupporterCount();

    public ClearRecordPlayerProfile(GameClearRecordData record, SkillDatabase skillDatabase, ItemDatabase itemDatabase)
        : this(record, skillDatabase, itemDatabase, null)
    {
    }

    public ClearRecordPlayerProfile(GameClearRecordData record, SkillDatabase skillDatabase, ItemDatabase itemDatabase, SupporterDatabase supporterDatabase)
        : this(record, skillDatabase, itemDatabase, supporterDatabase, null)
    {
    }

    public ClearRecordPlayerProfile(GameClearRecordData record, SkillDatabase skillDatabase, ItemDatabase itemDatabase, SupporterDatabase supporterDatabase, KarinItemDatabase karinItemDatabase)
    {
        Record = record;
        BuildPreviewStats();
        BuildPreviewSkills(skillDatabase);
        BuildPreviewInventory(itemDatabase);
        BuildPreviewSupporters(supporterDatabase);
        BuildPreviewKarinItems(karinItemDatabase);
        itemModifiedStats = BuildItemModifiedStats();
    }

    public PlayerStats GetBaseStats()
    {
        return baseStats != null ? baseStats.Clone() : new PlayerStats();
    }

    public PlayerStats GetItemModifiedStats()
    {
        return itemModifiedStats != null ? itemModifiedStats.Clone() : GetBaseStats();
    }

    public SupporterData GetActiveSupporter()
    {
        foreach (SupporterData supporter in previewUnlockedSupporters)
        {
            if (supporter != null && IsActiveSupporter(supporter.supporterID))
                return supporter;
        }

        return null;
    }

    public bool IsActiveSupporter(string supporterId)
    {
        if (string.IsNullOrEmpty(supporterId))
            return false;

        return supporterStatesById.TryGetValue(supporterId, out SavedSupporterState state) && state != null && state.active;
    }

    public SupporterChoiceState GetSupporterChoiceState(string supporterId)
    {
        if (string.IsNullOrEmpty(supporterId))
            return SupporterChoiceState.Undecided;

        return supporterStatesById.TryGetValue(supporterId, out SavedSupporterState state) && state != null
            ? NormalizeSupporterChoiceState(state)
            : SupporterChoiceState.Undecided;
    }

    public bool SetActiveSupporter(string supporterId)
    {
        if (string.IsNullOrEmpty(supporterId))
            return false;

        if (!supporterStatesById.TryGetValue(supporterId, out SavedSupporterState targetState) || targetState == null)
            return false;

        if (!targetState.unlocked || NormalizeSupporterChoiceState(targetState) != SupporterChoiceState.Recruited)
            return false;

        foreach (SavedSupporterState state in supporterStatesById.Values)
        {
            if (state != null)
                state.active = state == targetState;
        }

        return true;
    }

    public bool ClearActiveSupporter()
    {
        bool changed = false;
        foreach (SavedSupporterState state in supporterStatesById.Values)
        {
            if (state != null && state.active)
            {
                state.active = false;
                changed = true;
            }
        }

        return changed;
    }

    public KarinItemData GetEquippedKarinItem()
    {
        PlayerGrowthSaveData playerGrowth = PlayerGrowth;
        if (playerGrowth == null || string.IsNullOrEmpty(playerGrowth.equippedKarinItemID))
            return null;

        foreach (KarinItemData item in previewOwnedKarinItems)
        {
            if (item != null && item.itemID == playerGrowth.equippedKarinItemID)
                return item;
        }

        return null;
    }

    public bool IsEquippedKarinItem(string itemId)
    {
        PlayerGrowthSaveData playerGrowth = PlayerGrowth;
        return playerGrowth != null
            && !string.IsNullOrEmpty(itemId)
            && playerGrowth.equippedKarinItemID == itemId;
    }

    public bool SetEquippedKarinItem(string itemId)
    {
        PlayerGrowthSaveData playerGrowth = PlayerGrowth;
        if (playerGrowth == null || string.IsNullOrEmpty(itemId))
            return false;

        bool isOwned = false;
        foreach (string ownedItemId in playerGrowth.ownedKarinItemIDs ?? new List<string>())
        {
            if (ownedItemId == itemId)
            {
                isOwned = true;
                break;
            }
        }

        if (!isOwned)
            return false;

        playerGrowth.equippedKarinItemID = itemId;
        return true;
    }

    public bool ClearEquippedKarinItem()
    {
        PlayerGrowthSaveData playerGrowth = PlayerGrowth;
        if (playerGrowth == null)
            return false;

        bool changed = !string.IsNullOrEmpty(playerGrowth.equippedKarinItemID);
        playerGrowth.equippedKarinItemID = null;
        return changed;
    }

    public List<SkillData> GetSkillsByCategory(SkillCategory category)
    {
        List<SkillData> result = new List<SkillData>();
        foreach (SkillData skill in previewSkills)
        {
            if (skill != null && skill.category == category)
                result.Add(skill);
        }

        return result;
    }

    public Dictionary<ItemClass, int> GetPreviewSynergies()
    {
        Dictionary<string, OwnedItem> bestItemById = new Dictionary<string, OwnedItem>();

        foreach (OwnedItem item in previewInventory)
        {
            if (item == null || item.data == null)
                continue;

            string itemId = item.data.itemID;
            if (string.IsNullOrEmpty(itemId))
                continue;

            if (!bestItemById.TryGetValue(itemId, out OwnedItem currentBest))
            {
                bestItemById[itemId] = item;
                continue;
            }

            int currentBestPoints = currentBest.data.GetSynergyPoints(currentBest.starLevel);
            int newItemPoints = item.data.GetSynergyPoints(item.starLevel);
            if (newItemPoints > currentBestPoints)
                bestItemById[itemId] = item;
        }

        Dictionary<ItemClass, int> synergies = new Dictionary<ItemClass, int>();
        foreach (OwnedItem item in bestItemById.Values)
        {
            ItemClass itemClass = item.data.itemClass;
            int points = item.data.GetSynergyPoints(item.starLevel);

            if (!synergies.ContainsKey(itemClass))
                synergies[itemClass] = 0;

            synergies[itemClass] += points;
        }

        return synergies;
    }

    public int GetRejectedSupporterCount()
    {
        return PlayerGrowth != null ? Mathf.Max(0, PlayerGrowth.rejectedSupporterCount) : 0;
    }

    public void Dispose()
    {
        foreach (SkillData skill in previewSkills)
        {
            if (skill == null)
                continue;

            if (Application.isPlaying)
                UnityEngine.Object.Destroy(skill);
            else
                UnityEngine.Object.DestroyImmediate(skill);
        }

        previewSkills.Clear();

        foreach (SupporterData supporter in previewUnlockedSupporters)
        {
            if (supporter == null)
                continue;

            if (Application.isPlaying)
                UnityEngine.Object.Destroy(supporter);
            else
                UnityEngine.Object.DestroyImmediate(supporter);
        }

        previewUnlockedSupporters.Clear();
    }

    private void BuildPreviewStats()
    {
        PlayerGrowthSaveData playerGrowth = PlayerGrowth;
        if (playerGrowth == null)
        {
            baseStats = new PlayerStats();
            return;
        }

        baseStats = new PlayerStats
        {
            level = Mathf.Max(1, playerGrowth.level),
            maxExp = Mathf.Max(0, playerGrowth.maxExp),
            currentExp = Mathf.Max(0, playerGrowth.currentExp),
            maxHp = Mathf.Max(1, playerGrowth.maxHp),
            currentHp = Mathf.Max(0, playerGrowth.currentHp),
            ActionPoints = Mathf.Max(0, playerGrowth.actionPoints),
            breakResistance = Mathf.Max(0, playerGrowth.breakResistance),
            maxBreakGauge = Mathf.Max(0f, playerGrowth.maxBreakGauge),
            strength = Mathf.Max(1, playerGrowth.strength),
            defense = Mathf.Max(1, playerGrowth.defense),
            speed = Mathf.Max(1, playerGrowth.speed),
            luck = Mathf.Max(1, playerGrowth.luck),
            currentGold = Mathf.Max(0, playerGrowth.currentGold),
            rejectedSupporterCount = Mathf.Max(0, playerGrowth.rejectedSupporterCount)
        };
    }

    private void BuildPreviewSkills(SkillDatabase skillDatabase)
    {
        PlayerGrowthSaveData playerGrowth = PlayerGrowth;
        if (playerGrowth == null || playerGrowth.skills == null || skillDatabase == null)
            return;

        foreach (SavedSkillState savedSkill in playerGrowth.skills)
        {
            if (savedSkill == null)
                continue;

            SkillData sourceSkill = skillDatabase.GetByID(savedSkill.skillID);
            if (sourceSkill == null)
                sourceSkill = skillDatabase.GetByNameKeyFallback(savedSkill.skillID);

            if (sourceSkill == null)
            {
                DevLog.LogWarning($"[MainMenu] ClearRecord preview skill not found: {savedSkill.skillID}");
                continue;
            }

            SkillData runtimeSkill = UnityEngine.Object.Instantiate(sourceSkill);
            runtimeSkill.skillLevel = Mathf.Max(1, savedSkill.skillLevel);
            runtimeSkill.currentEvolution = savedSkill.currentEvolution;
            previewSkills.Add(runtimeSkill);
        }
    }

    private void BuildPreviewInventory(ItemDatabase itemDatabase)
    {
        PlayerGrowthSaveData playerGrowth = PlayerGrowth;
        if (playerGrowth == null || playerGrowth.inventory == null)
            return;

        if (itemDatabase == null || itemDatabase.allItems == null)
        {
            DevLog.LogWarning("[MainMenu] ClearRecord preview inventory cannot resolve items: ItemDatabase missing.");
            return;
        }

        foreach (SavedOwnedItem savedItem in playerGrowth.inventory)
        {
            if (savedItem == null || string.IsNullOrEmpty(savedItem.itemID))
                continue;

            EquipmentItemData itemData = FindItem(itemDatabase, savedItem.itemID);
            if (itemData == null)
            {
                DevLog.LogWarning($"[MainMenu] ClearRecord preview item not found: {savedItem.itemID}");
                continue;
            }

            previewInventory.Add(new OwnedItem(itemData, Mathf.Clamp(savedItem.starLevel, 1, 3)));
        }
    }

    private void BuildPreviewSupporters(SupporterDatabase supporterDatabase)
    {
        PlayerGrowthSaveData playerGrowth = PlayerGrowth;
        if (playerGrowth == null || playerGrowth.supporters == null)
            return;

        if (supporterDatabase == null || supporterDatabase.allSupporters == null)
            return;

        foreach (SavedSupporterState savedSupporter in playerGrowth.supporters)
        {
            if (savedSupporter == null || string.IsNullOrEmpty(savedSupporter.supporterID))
                continue;

            supporterStatesById[savedSupporter.supporterID] = savedSupporter;

            SupporterChoiceState choiceState = NormalizeSupporterChoiceState(savedSupporter);
            if (!savedSupporter.unlocked || choiceState != SupporterChoiceState.Recruited)
                continue;

            SupporterData sourceSupporter = supporterDatabase.GetByID(savedSupporter.supporterID);
            if (sourceSupporter == null)
            {
                DevLog.LogWarning($"[MainMenu] ClearRecord preview supporter not found: {savedSupporter.supporterID}");
                continue;
            }

            SupporterData runtimeSupporter = UnityEngine.Object.Instantiate(sourceSupporter);
            runtimeSupporter.passiveLevel = Mathf.Clamp(savedSupporter.passiveLevel, 1, 3);
            runtimeSupporter.startSkillLevel = Mathf.Clamp(savedSupporter.startSkillLevel, 1, 3);
            runtimeSupporter.battleSkillLevel = Mathf.Clamp(savedSupporter.battleSkillLevel, 1, 3);
            previewUnlockedSupporters.Add(runtimeSupporter);
        }
    }

    private void BuildPreviewKarinItems(KarinItemDatabase karinItemDatabase)
    {
        PlayerGrowthSaveData playerGrowth = PlayerGrowth;
        if (playerGrowth == null || playerGrowth.ownedKarinItemIDs == null)
            return;

        if (karinItemDatabase == null || karinItemDatabase.allItems == null)
            return;

        foreach (string itemID in playerGrowth.ownedKarinItemIDs)
        {
            if (string.IsNullOrEmpty(itemID))
                continue;

            KarinItemData item = karinItemDatabase.GetByID(itemID);
            if (item == null)
            {
                DevLog.LogWarning($"[MainMenu] ClearRecord preview Karin item not found: {itemID}");
                continue;
            }

            if (!previewOwnedKarinItems.Contains(item))
                previewOwnedKarinItems.Add(item);
        }
    }

    private EquipmentItemData FindItem(ItemDatabase itemDatabase, string itemID)
    {
        foreach (EquipmentItemData item in itemDatabase.allItems)
        {
            if (item != null && item.itemID == itemID)
                return item;
        }

        return null;
    }

    private SupporterChoiceState NormalizeSupporterChoiceState(SavedSupporterState savedSupporter)
    {
        if (savedSupporter == null)
            return SupporterChoiceState.Undecided;

        if (savedSupporter.choiceState != SupporterChoiceState.Undecided)
            return savedSupporter.choiceState;

        return savedSupporter.unlocked ? SupporterChoiceState.Recruited : SupporterChoiceState.Undecided;
    }

    private PlayerStats BuildItemModifiedStats()
    {
        PlayerStats modified = baseStats != null ? baseStats.Clone() : new PlayerStats();

        int flatStr = 0, flatDef = 0, flatSpd = 0, flatLuck = 0, flatMaxHp = 0, flatAP = 0, flatBR = 0;
        float pctStr = 0f, pctDef = 0f, pctSpd = 0f, pctLuck = 0f, pctMaxHp = 0f, pctAP = 0f, pctBR = 0f;

        foreach (OwnedItem item in previewInventory)
        {
            if (item == null || item.data == null)
                continue;

            int sl = item.starLevel;
            flatStr += item.data.GetFlatStr(sl);
            flatDef += item.data.GetFlatDef(sl);
            flatSpd += item.data.GetFlatSpd(sl);
            flatLuck += item.data.GetFlatLuck(sl);
            flatMaxHp += item.data.GetFlatMaxHp(sl);
            flatAP += item.data.GetFlatAP(sl);
            flatBR += item.data.GetFlatBR(sl);

            pctStr += item.data.GetPctStr(sl);
            pctDef += item.data.GetPctDef(sl);
            pctSpd += item.data.GetPctSpd(sl);
            pctLuck += item.data.GetPctLuck(sl);
            pctMaxHp += item.data.GetPctMaxHp(sl);
            pctAP += item.data.GetPctAP(sl);

            modified.finalDamageAmp += item.data.GetFinalDamageAmp(sl);
            modified.finalDamageReduction += item.data.GetFinalDamageReduction(sl);
            modified.critRate += item.data.GetCritRateBonus(sl);
            modified.critDamage += item.data.GetCritDamageBonus(sl);
            modified.lifeSteal += item.data.GetLifeStealRate(sl);
        }

        Dictionary<ItemClass, int> syn = GetPreviewSynergies();
        ItemSynergyBalanceData synergyBalance = ItemSynergyBalanceData.Resolve();

        if (syn.GetValueOrDefault(ItemClass.Saber) >= 2) pctStr += 0.15f;
        if (syn.GetValueOrDefault(ItemClass.Saber) >= 4) modified.finalDamageAmp += 0.30f;

        if (syn.GetValueOrDefault(ItemClass.Shielder) >= 2) pctDef += 0.20f;
        if (syn.GetValueOrDefault(ItemClass.Shielder) >= 4) modified.finalDamageReduction += 0.20f;

        if (syn.GetValueOrDefault(ItemClass.Gunner) >= 2) pctLuck += 0.15f;
        if (syn.GetValueOrDefault(ItemClass.Gunner) >= 4) modified.critRate += 0.15f;

        if (syn.GetValueOrDefault(ItemClass.Assassin) >= 2) pctAP += 0.15f;

        if (syn.GetValueOrDefault(ItemClass.Boxer) >= 2) pctSpd += 0.20f;

        if (syn.GetValueOrDefault(ItemClass.Boxer) >= 4)
        {
            modified.bonusAccuracy += 20f;
            modified.bonusEvasion += 20f;
        }

        if (syn.GetValueOrDefault(ItemClass.Beast) >= 2) pctMaxHp += 0.15f;
        if (syn.GetValueOrDefault(ItemClass.Beast) >= 4) pctBR += 0.20f;

        if (syn.GetValueOrDefault(ItemClass.Caster) >= 2) modified.finalDamageAmp += 0.05f;
        if (syn.GetValueOrDefault(ItemClass.Trickster) >= 2) modified.finalDamageAmp += 0.05f;
        if (syn.GetValueOrDefault(ItemClass.Berserker) >= 2) modified.finalDamageReduction += 0.10f;
        if (syn.GetValueOrDefault(ItemClass.Demon) >= 2) modified.lifeSteal += 0.03f;
        if (syn.GetValueOrDefault(ItemClass.Demon) >= 4) modified.healingReceivedAmp += 0.20f;

        List<OwnedItem> demonEpics = previewInventory.FindAll(x => x != null && x.data != null && x.data.itemClass == ItemClass.Demon && x.data.grade == ItemGrade.Epic);
        foreach (OwnedItem dEpic in demonEpics)
        {
            if (dEpic.starLevel == 1) modified.healingReceivedAmp += 0.07f;
            else if (dEpic.starLevel == 2) modified.healingReceivedAmp += 0.27f;
            else if (dEpic.starLevel >= 3) modified.healingReceivedAmp += 1.00f;
        }

        float[] loneWolfAmps = { 0f, 0.05f, 0.10f, 0.20f, 0.40f, 0.75f, 1.30f, 2.00f };
        int rejectCount = Mathf.Clamp(GetRejectedSupporterCount(), 0, 7);
        float loneWolfBuff = loneWolfAmps[rejectCount];

        if (loneWolfBuff > 0f)
        {
            pctStr += loneWolfBuff;
            pctDef += loneWolfBuff;
            pctSpd += loneWolfBuff;
            pctLuck += loneWolfBuff;
            pctMaxHp += loneWolfBuff;
            pctAP += loneWolfBuff;
            pctBR += loneWolfBuff;
        }

        modified.strength = Mathf.Max(1, Mathf.RoundToInt((baseStats.strength + flatStr) * (1f + pctStr)));
        modified.defense = Mathf.Max(1, Mathf.RoundToInt((baseStats.defense + flatDef) * (1f + pctDef)));
        modified.speed = Mathf.Max(1, Mathf.RoundToInt((baseStats.speed + flatSpd) * (1f + pctSpd)));
        modified.luck = Mathf.Max(1, Mathf.RoundToInt((baseStats.luck + flatLuck) * (1f + pctLuck)));
        modified.ActionPoints = Mathf.Max(1, Mathf.RoundToInt((baseStats.ActionPoints + flatAP) * (1f + pctAP)));
        modified.maxHp = Mathf.Max(1, Mathf.RoundToInt((baseStats.maxHp + flatMaxHp) * (1f + pctMaxHp)));
        modified.breakResistance = Mathf.Max(1, Mathf.RoundToInt((baseStats.breakResistance + flatBR) * (1f + pctBR)));

        if (syn.GetValueOrDefault(ItemClass.Saber) >= 6) modified.trueDamageConversion += synergyBalance.saber6TrueDamageConversion;
        if (previewInventory.Any(x => x != null && x.data != null && x.data.itemClass == ItemClass.Saber && x.data.grade == ItemGrade.Legendary))
            modified.trueDamageConversion += synergyBalance.saberLegendaryTrueDamageConversion;

        float defToStrRatio = 0f;
        if (syn.GetValueOrDefault(ItemClass.Shielder) >= 6) defToStrRatio += synergyBalance.shielder6DefenseToStrengthMultiplier;
        if (previewInventory.Any(x => x != null && x.data != null && x.data.itemClass == ItemClass.Shielder && x.data.grade == ItemGrade.Legendary))
            defToStrRatio += synergyBalance.shielderLegendaryDefenseToStrengthMultiplier;
        modified.strength += Mathf.RoundToInt(modified.defense * defToStrRatio);

        float luckToCritDmg = 0f;
        if (syn.GetValueOrDefault(ItemClass.Gunner) >= 6) luckToCritDmg += synergyBalance.gunner6LuckToCritDamagePercentPerLuck;
        if (previewInventory.Any(x => x != null && x.data != null && x.data.itemClass == ItemClass.Gunner && x.data.grade == ItemGrade.Legendary))
            luckToCritDmg += synergyBalance.gunnerLegendaryLuckToCritDamagePercentPerLuck;
        modified.critDamage += modified.luck * luckToCritDmg * 0.01f;

        if (syn.GetValueOrDefault(ItemClass.Assassin) >= 6) modified.critDamage += modified.ActionPoints * synergyBalance.assassin6ApToCritDamagePercentPerAp * 0.01f;
        if (previewInventory.Any(x => x != null && x.data != null && x.data.itemClass == ItemClass.Assassin && x.data.grade == ItemGrade.Legendary))
            modified.critRate += modified.ActionPoints * synergyBalance.assassinLegendaryApToCritRatePercentPerAp * 0.01f;

        float spdToStrRatio = 0f;
        if (syn.GetValueOrDefault(ItemClass.Boxer) >= 6) spdToStrRatio += synergyBalance.boxer6SpeedToStrengthMultiplier;
        if (previewInventory.Any(x => x != null && x.data != null && x.data.itemClass == ItemClass.Boxer && x.data.grade == ItemGrade.Legendary))
            spdToStrRatio += synergyBalance.boxerLegendarySpeedToStrengthMultiplier;
        modified.strength += Mathf.RoundToInt(modified.speed * spdToStrRatio);

        float hpToStrRatio = 0f;
        if (syn.GetValueOrDefault(ItemClass.Beast) >= 6) hpToStrRatio += synergyBalance.beast6MaxHpToStrengthRatio;
        if (previewInventory.Any(x => x != null && x.data != null && x.data.itemClass == ItemClass.Beast && x.data.grade == ItemGrade.Legendary))
            hpToStrRatio += synergyBalance.beastLegendaryMaxHpToStrengthRatio;
        modified.strength += Mathf.RoundToInt(modified.maxHp * hpToStrRatio);

        modified.currentHp = Mathf.Clamp(baseStats.currentHp, 0, modified.maxHp);

        return modified;
    }
}
