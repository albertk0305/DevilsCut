using UnityEngine;

[CreateAssetMenu(fileName = "NewEquipment", menuName = "GameData/EquipmentItem")]
public class EquipmentItemData : ScriptableObject
{
    [Header("기본 정보")]
    public string itemNameKey; // 다국어 Key
    public Sprite itemIcon;
    [TextArea] public string itemDescKey; // 다국어 Key
    [Header("장비 스탯 효과")]
    public string itemBonusKey; // 다국어 Key
}