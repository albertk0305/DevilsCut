public static class DialogueRuntimeContext
{
    public static string PendingDialogueID { get; private set; }

    public static void SetPendingDialogueID(string dialogueID)
    {
        PendingDialogueID = dialogueID;
    }

    public static string ConsumePendingDialogueID()
    {
        string dialogueID = PendingDialogueID;
        PendingDialogueID = "";
        return dialogueID;
    }
}
