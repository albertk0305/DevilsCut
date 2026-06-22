using UnityEngine;

[CreateAssetMenu(fileName = "MaidCafeSupporterDialogue", menuName = "GameData/MaidCafe Supporter Dialogue")]
public class MaidCafeSupporterDialogueData : ScriptableObject
{
    public SupporterData supporter;
    public Sprite defaultSprite;
    public Sprite happySprite;
    public Sprite embarrassedSprite;
    public string selectedTextKey;
    [TextArea] public string selectedText;
    public string giftTextKey;
    [TextArea] public string giftText;
    public string maxSkillGiftTextKey;
    [TextArea] public string maxSkillGiftText;
    public string farewellTextKey;
    [TextArea] public string farewellText;
}
