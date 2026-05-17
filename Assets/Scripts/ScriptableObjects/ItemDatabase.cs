using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "GameData/ItemDatabase")]
public class ItemDatabase : ScriptableObject
{
    [Header("게임 내 모든 장비 아이템 리스트")]
    public List<EquipmentItemData> allItems = new List<EquipmentItemData>();

    // =========================================
    // 상점 & 전리품(로그라이크) 시스템을 위한 헬퍼 함수들
    // =========================================

    // 특정 등급의 아이템들만 뽑아오기 (예: 상점에서 에픽만 확률적으로 띄울 때)
    public List<EquipmentItemData> GetItemsByGrade(ItemGrade grade)
    {
        return allItems.FindAll(item => item.grade == grade);
    }

    // 3성을 달성하여 더 이상 등장하면 안 되는 아이템을 필터링해서 뽑아오기
    public List<EquipmentItemData> GetAvailableItemsForDrop(ItemGrade grade)
    {
        // 1. 해당 등급의 모든 아이템을 가져옵니다.
        var pool = GetItemsByGrade(grade);

        if (PlayerManager.Instance == null) return pool;

        // 2. 플레이어 인벤토리를 확인하여, 이미 3성(최종)을 달성했거나 전설(유일)인 아이템은 풀에서 제외합니다.
        var myInventory = PlayerManager.Instance.inventory;

        List<EquipmentItemData> filteredPool = new List<EquipmentItemData>();
        foreach (var item in pool)
        {
            // 인벤토리에 이 아이템이 3성으로 존재하는가? 또는 (전설인데 이미 존재하는가?)
            bool isMaxedOut = myInventory.Exists(x => x.data.itemID == item.itemID && x.starLevel >= 3);
            bool isAlreadyOwnedLegendary = (item.grade == ItemGrade.Legendary) && myInventory.Exists(x => x.data.itemID == item.itemID);

            if (!isMaxedOut && !isAlreadyOwnedLegendary)
            {
                filteredPool.Add(item);
            }
        }

        return filteredPool;
    }

    // 무작위 아이템 뽑기 (가챠용)
    public EquipmentItemData GetRandomItem(ItemGrade grade)
    {
        var pool = GetAvailableItemsForDrop(grade);
        if (pool.Count == 0) return null; // 뽑을 수 있는 아이템이 고갈됨

        return pool[Random.Range(0, pool.Count)];
    }
}