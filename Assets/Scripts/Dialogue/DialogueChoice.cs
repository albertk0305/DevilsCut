using System;

[Serializable]
public class DialogueChoice
{
    public bool hasChoice;
    public string yesTextKey;
    public string noTextKey;
    public DialogueChoiceAction yesAction;
    public DialogueChoiceAction noAction;
    public string yesActionValue;
    public string noActionValue;
    public string yesNextLineID;
    public string noNextLineID;
}
