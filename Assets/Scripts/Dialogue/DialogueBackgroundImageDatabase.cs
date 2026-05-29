using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DialogueBackgroundImageDatabase", menuName = "DevilsCut/Dialogue/Background Image Database")]
public class DialogueBackgroundImageDatabase : ScriptableObject
{
    public List<DialogueBackgroundImageEntry> backgrounds = new List<DialogueBackgroundImageEntry>();

    public bool TryGetSprite(string backgroundID, out Sprite sprite)
    {
        sprite = null;

        if (string.IsNullOrEmpty(backgroundID))
            return false;

        foreach (DialogueBackgroundImageEntry entry in backgrounds)
        {
            if (entry == null || entry.backgroundID != backgroundID || entry.sprite == null)
                continue;

            sprite = entry.sprite;
            return true;
        }

        return false;
    }
}
