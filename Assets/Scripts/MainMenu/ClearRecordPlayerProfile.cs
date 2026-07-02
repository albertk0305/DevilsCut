using System;
using System.Collections.Generic;
using UnityEngine;

public class ClearRecordPlayerProfile : IDisposable
{
    private readonly List<SkillData> previewSkills = new List<SkillData>();
    private readonly List<OwnedItem> previewInventory = new List<OwnedItem>();

    public GameClearRecordData Record { get; }
    public PlayerGrowthSaveData PlayerGrowth => Record != null ? Record.playerGrowth : null;
    public string ClearId => Record != null ? Record.clearId : "";
    public int ClearNumber => Record != null ? Record.clearNumber : 0;

    public ClearRecordPlayerProfile(GameClearRecordData record, SkillDatabase skillDatabase, ItemDatabase itemDatabase)
    {
        Record = record;
        BuildPreviewSkills(skillDatabase);
        BuildPreviewInventory(itemDatabase);
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

    private EquipmentItemData FindItem(ItemDatabase itemDatabase, string itemID)
    {
        foreach (EquipmentItemData item in itemDatabase.allItems)
        {
            if (item != null && item.itemID == itemID)
                return item;
        }

        return null;
    }
}
