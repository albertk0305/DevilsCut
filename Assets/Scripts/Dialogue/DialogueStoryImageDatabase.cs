using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DialogueStoryImageDatabase", menuName = "DevilsCut/Dialogue/Story Image Database")]
public class DialogueStoryImageDatabase : ScriptableObject
{
    public List<DialogueStoryImageEntry> images = new List<DialogueStoryImageEntry>();

    public bool TryGetSprite(string imageID, out Sprite sprite)
    {
        sprite = null;

        if (string.IsNullOrEmpty(imageID))
            return false;

        foreach (DialogueStoryImageEntry entry in images)
        {
            if (entry == null || entry.imageID != imageID || entry.sprite == null)
                continue;

            sprite = entry.sprite;
            return true;
        }

        return false;
    }
}
