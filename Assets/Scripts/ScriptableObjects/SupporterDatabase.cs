using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SupporterDatabase", menuName = "GameData/SupporterDatabase")]
public class SupporterDatabase : ScriptableObject
{
    public List<SupporterData> allSupporters = new List<SupporterData>();

    public SupporterData GetByID(string supporterID)
    {
        if (string.IsNullOrEmpty(supporterID))
        {
            Debug.LogWarning("SupporterDatabase: empty supporterID.");
            return null;
        }

        SupporterData found = null;

        foreach (SupporterData supporter in allSupporters)
        {
            if (supporter == null || supporter.supporterID != supporterID)
                continue;

            if (found != null)
                Debug.LogWarning($"SupporterDatabase: duplicate supporterID '{supporterID}'.");

            found = supporter;
        }

        return found;
    }
}
