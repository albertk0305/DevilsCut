using System;
using UnityEngine;

[Serializable]
public class DialogueLine
{
    public string lineID;
    public string speakerID;
    public string leftActorID;
    public string rightActorID;
    public string expressionID;
    public string leftExpressionID;
    public string rightExpressionID;
    public string speakerNameKey;
    public string bodyTextKey;
    public DialogueChoiceAction lineEndAction;
    public string lineEndActionValue;
    public Sprite leftCharacterImage;
    public Sprite rightCharacterImage;
    public string backgroundID;
    public string storyImageID;
    public string choiceID;
    public bool clearBackground;
    public bool showStoryImage;
    public Sprite storyImage;
    public DialogueChoice choice = new DialogueChoice();
}
