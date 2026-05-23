using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "KarinItemDatabase", menuName = "GameData/KarinItemDatabase")]
public class KarinItemDatabase : ScriptableObject
{
    public List<KarinItemData> allItems = new List<KarinItemData>();

    public KarinItemData GetByID(string itemID)
    {
        if (string.IsNullOrEmpty(itemID))
        {
            Debug.LogWarning("KarinItemDatabase: empty itemID.");
            return null;
        }

        KarinItemData found = null;

        foreach (KarinItemData item in allItems)
        {
            if (item == null || item.itemID != itemID)
                continue;

            if (found != null)
                Debug.LogWarning($"KarinItemDatabase: duplicate itemID '{itemID}'.");

            found = item;
        }

        return found;
    }
}
