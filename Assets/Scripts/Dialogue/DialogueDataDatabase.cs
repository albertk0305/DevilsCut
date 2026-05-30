using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DialogueDataDatabase", menuName = "DevilsCut/Dialogue/Dialogue Data Database")]
public class DialogueDataDatabase : ScriptableObject
{
    public List<DialogueData> dialogues = new List<DialogueData>();

    public bool TryGetDialogueData(string dialogueID, out DialogueData data)
    {
        data = null;

        if (string.IsNullOrEmpty(dialogueID))
            return false;

        foreach (DialogueData dialogue in dialogues)
        {
            if (dialogue == null || dialogue.dialogueID != dialogueID)
                continue;

            data = dialogue;
            return true;
        }

        return false;
    }
}
