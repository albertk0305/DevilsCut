using UnityEngine;

[CreateAssetMenu(fileName = "NewKarinItem", menuName = "GameData/KarinItem")]
public class KarinItemData : ScriptableObject
{
    public string itemName;
    public Sprite itemIcon; // 우측 버튼과 메인 화면에 뜰 이미지

    [TextArea] public string itemDescription; // 장비 설명 텍스트

    [Header("카린 대사")]
    public string previewDialogue; // 목록에서 눌러서 고민 중일 때 나오는 대사
    public string equipDialogue;   // 장비(Equip) 했을 때 나오는 대사

    [Header("아이템 효과 로직")]
    public KarinItemLogicBase itemLogic;
}