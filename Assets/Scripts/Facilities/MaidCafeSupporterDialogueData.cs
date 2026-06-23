using UnityEngine;

public enum MaidCafeSupporterExpression
{
    Auto,
    Default,
    Happy,
    Embarrassed
}

[CreateAssetMenu(fileName = "MaidCafeSupporterDialogue", menuName = "GameData/MaidCafe Supporter Dialogue")]
public class MaidCafeSupporterDialogueData : ScriptableObject
{
    public SupporterData supporter;
    public Sprite defaultSprite;
    public Sprite happySprite;
    public Sprite embarrassedSprite;
    public MaidCafeSupporterExpression selectedExpression = MaidCafeSupporterExpression.Auto;
    public string selectedTextKey;
    [TextArea] public string selectedText;
    public MaidCafeSupporterExpression giftExpression = MaidCafeSupporterExpression.Auto;
    public string giftTextKey;
    [TextArea] public string giftText;
    public MaidCafeSupporterExpression maxSkillGiftExpression = MaidCafeSupporterExpression.Auto;
    public string maxSkillGiftTextKey;
    [TextArea] public string maxSkillGiftText;
    public MaidCafeSupporterExpression farewellExpression = MaidCafeSupporterExpression.Auto;
    public string farewellTextKey;
    [TextArea] public string farewellText;
}
