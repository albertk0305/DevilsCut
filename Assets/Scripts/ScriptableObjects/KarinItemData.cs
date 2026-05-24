using UnityEngine;

[CreateAssetMenu(fileName = "NewKarinItem", menuName = "GameData/KarinItem")]
public class KarinItemData : ScriptableObject
{
    [Header("저장용 고유 ID")]
    public string itemID;

    public string itemName;
    public Sprite itemIcon;

    [TextArea] public string itemDescription;

    [Header("카린 대사")]
    public string previewDialogue;
    public string equipDialogue;

    [Header("아이템 효과 로직")]
    public KarinItemLogicBase itemLogic;
}
