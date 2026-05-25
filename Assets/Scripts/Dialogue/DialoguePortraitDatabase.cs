using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DialoguePortraitDatabase", menuName = "DevilsCut/Dialogue/Dialogue Portrait Database")]
public class DialoguePortraitDatabase : ScriptableObject
{
    [Serializable]
    public class SpeakerEntry
    {
        public string speakerID;
        public string speakerNameKey;
    }

    [Serializable]
    public class PortraitEntry
    {
        public string actorID;
        public string expressionID;
        public Sprite portraitSprite;
    }

    public List<SpeakerEntry> speakers = new List<SpeakerEntry>();
    public List<PortraitEntry> portraits = new List<PortraitEntry>();

    public bool TryGetSpeakerNameKey(string speakerID, out string speakerNameKey)
    {
        speakerNameKey = "";

        if (string.IsNullOrEmpty(speakerID))
            return false;

        foreach (SpeakerEntry speaker in speakers)
        {
            if (speaker == null || speaker.speakerID != speakerID)
                continue;

            speakerNameKey = speaker.speakerNameKey;
            return !string.IsNullOrEmpty(speakerNameKey);
        }

        return false;
    }

    public bool TryGetPortrait(string actorID, string expressionID, out PortraitEntry portrait)
    {
        portrait = null;

        if (string.IsNullOrEmpty(actorID))
            return false;

        foreach (PortraitEntry entry in portraits)
        {
            if (entry == null || entry.actorID != actorID)
                continue;

            if (entry.expressionID == expressionID)
            {
                portrait = entry;
                return true;
            }
        }

        foreach (PortraitEntry entry in portraits)
        {
            if (entry == null || entry.actorID != actorID)
                continue;

            if (string.IsNullOrEmpty(entry.expressionID))
            {
                portrait = entry;
                return true;
            }
        }

        return false;
    }

    public Sprite GetPortraitSprite(string actorID, string expressionID)
    {
        if (TryGetPortrait(actorID, expressionID, out PortraitEntry portrait))
            return portrait.portraitSprite;

        return null;
    }
}
