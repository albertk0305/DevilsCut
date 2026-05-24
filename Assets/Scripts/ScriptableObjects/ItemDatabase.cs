using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "GameData/ItemDatabase")]
public class ItemDatabase : ScriptableObject
{
    [Header("게임 내 모든 장비 아이템 리스트")]
    public List<EquipmentItemData> allItems = new List<EquipmentItemData>();

    public List<EquipmentItemData> GetItemsByGrade(ItemGrade grade)
    {
        return allItems.FindAll(item => item.grade == grade);
    }

    // Excludes maxed items and already-owned legendaries from future drops.
    public List<EquipmentItemData> GetAvailableItemsForDrop(ItemGrade grade)
    {
        var pool = GetItemsByGrade(grade);

        if (PlayerManager.Instance == null) return pool;

        var myInventory = PlayerManager.Instance.inventory;

        List<EquipmentItemData> filteredPool = new List<EquipmentItemData>();
        foreach (var item in pool)
        {
            bool isMaxedOut = myInventory.Exists(x => x.data.itemID == item.itemID && x.starLevel >= 3);
            bool isAlreadyOwnedLegendary = (item.grade == ItemGrade.Legendary) && myInventory.Exists(x => x.data.itemID == item.itemID);

            if (!isMaxedOut && !isAlreadyOwnedLegendary)
            {
                filteredPool.Add(item);
            }
        }

        return filteredPool;
    }

    public EquipmentItemData GetRandomItem(ItemGrade grade)
    {
        var pool = GetAvailableItemsForDrop(grade);
        if (pool.Count == 0) return null;

        return pool[Random.Range(0, pool.Count)];
    }
}