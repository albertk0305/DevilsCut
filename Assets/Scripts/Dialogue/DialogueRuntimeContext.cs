public static class DialogueRuntimeContext
{
    public static string PendingDialogueID { get; private set; }
    public static bool ForceFastForwardForPendingDialogue { get; private set; }

    public static void SetPendingDialogueID(string dialogueID, bool forceFastForward = false)
    {
        PendingDialogueID = dialogueID;
        ForceFastForwardForPendingDialogue = forceFastForward;
    }

    public static string ConsumePendingDialogueID()
    {
        string dialogueID = PendingDialogueID;
        PendingDialogueID = "";
        if (string.IsNullOrEmpty(dialogueID))
            ForceFastForwardForPendingDialogue = false;

        return dialogueID;
    }

    public static bool ConsumeForceFastForwardForPendingDialogue()
    {
        bool forceFastForward = ForceFastForwardForPendingDialogue;
        ForceFastForwardForPendingDialogue = false;
        return forceFastForward;
    }
}
