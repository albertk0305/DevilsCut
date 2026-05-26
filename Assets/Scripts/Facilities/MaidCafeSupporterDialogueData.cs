using UnityEngine;

[CreateAssetMenu(fileName = "MaidCafeSupporterDialogue", menuName = "GameData/MaidCafe Supporter Dialogue")]
public class MaidCafeSupporterDialogueData : ScriptableObject
{
    public SupporterData supporter;
    public Sprite defaultSprite;
    public Sprite happySprite;
    public Sprite embarrassedSprite;
    [TextArea] public string selectedText;
    [TextArea] public string giftText;
    [TextArea] public string maxSkillGiftText;
    [TextArea] public string farewellText;
}
