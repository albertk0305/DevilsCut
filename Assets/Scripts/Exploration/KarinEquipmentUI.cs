using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class KarinEquipmentUI : MonoBehaviour
{
    [Header("메인 디스플레이")]
    public Image mainItemImage;
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI itemDescText;
    public Image karinFaceImage; // (지금은 고정 이미지라도 연결해두면 좋습니다)
    public TextMeshProUGUI karinDialogueText;

    [Header("우측 인벤토리 목록")]
    public Button[] inventoryButtons; // 8개의 슬롯 버튼 (반드시 좌->우, 상->하 순서대로 0~7번 넣으세요!)
    public Button upScrollButton;
    public Button downScrollButton;

    [Header("인벤토리 색상 피드백")] // [추가됨] 장비 상태를 보여줄 색상
    public Color normalColor = Color.white; // 미장착 아이템 (원래 색)
    public Color equippedColor = new Color(0.4f, 0.4f, 0.4f); // 장착 중인 아이템 (어둡게)

    [Header("액션 버튼")]
    public Button equipButton;
    public Button removeButton;
    public Button cancelButton;

    // 내부 상태 관리
    private KarinItemData currentPreview;
    private int currentRow = 0; // 현재 스크롤 맨 윗줄 번호 (0부터 시작)
    private const int columns = 2; // 한 줄에 2칸 (2열)
    private const int visibleRows = 4; // 화면에 보이는 줄 수 (4행)

    private void OnEnable()
    {
        // 탭 열릴 때마다 현재 장착 중인 아이템 띄우기 (없으면 null 처리)
        ShowPreview(PlayerManager.Instance.equippedKarinItem, isEquippedState: true);
        currentRow = 0; // 스크롤 맨 위로 초기화
        RefreshInventory();
    }

    private void ShowPreview(KarinItemData data, bool isEquippedState)
    {
        currentPreview = data;

        if (data == null)
        {
            mainItemImage.gameObject.SetActive(false);

            itemNameText.text = "";
            itemDescText.text = "장착된 장비가 없습니다.";
            karinDialogueText.text = "어떤 장비를 써볼까요?";

            equipButton.interactable = false;
            removeButton.interactable = false;
            cancelButton.gameObject.SetActive(false); // 아무것도 선택 안 했으니 취소 불가
        }
        else
        {
            mainItemImage.gameObject.SetActive(true);

            mainItemImage.sprite = data.itemIcon;
            itemNameText.text = data.itemName;
            itemDescText.text = data.itemDescription;
            karinDialogueText.text = isEquippedState ? data.equipDialogue : data.previewDialogue;

            equipButton.interactable = !isEquippedState;
            removeButton.interactable = isEquippedState;
            cancelButton.gameObject.SetActive(!isEquippedState); // 미리보기 중일 때만 취소 가능
        }
    }

    private void RefreshInventory()
    {
        List<KarinItemData> ownedList = PlayerManager.Instance.ownedKarinItems;

        // 데이터 시작 인덱스 = (현재 줄 번호 * 1줄당 칸 수)
        int startIndex = currentRow * columns;

        for (int i = 0; i < inventoryButtons.Length; i++)
        {
            int dataIndex = startIndex + i;

            if (dataIndex < ownedList.Count)
            {
                KarinItemData itemData = ownedList[dataIndex];

                inventoryButtons[i].gameObject.SetActive(true);
                inventoryButtons[i].image.sprite = ownedList[dataIndex].itemIcon;
                inventoryButtons[i].interactable = true;

                // [핵심 추가] 현재 그려주는 버튼의 데이터가 '착용 중'인 아이템과 똑같다면?
                if (itemData == PlayerManager.Instance.equippedKarinItem)
                {
                    inventoryButtons[i].image.color = equippedColor; // 어둡게 처리
                }
                else
                {
                    inventoryButtons[i].image.color = normalColor; // 기본 색상으로 원상복구
                }
            }
            else
            {
                inventoryButtons[i].gameObject.SetActive(false);
                inventoryButtons[i].interactable = false;
            }
        }

        // 스크롤 버튼 활성화/비활성화 로직
        int totalRows = Mathf.CeilToInt((float)ownedList.Count / columns);

        // 맨 위면 위로 가기 버튼 끄기
        upScrollButton.interactable = (currentRow > 0);
        // (현재 줄 + 보이는 줄)이 전체 줄 수보다 작을 때만 아래로 가기 활성화
        downScrollButton.interactable = (currentRow + visibleRows < totalRows);
    }

    // 우측 인벤토리 슬롯 클릭 시 (0~7번)
    public void OnClickInventorySlot(int slotIndex)
    {
        int dataIndex = (currentRow * columns) + slotIndex;
        if (dataIndex < PlayerManager.Instance.ownedKarinItems.Count)
        {
            KarinItemData clickedItem = PlayerManager.Instance.ownedKarinItems[dataIndex];

            // [방어 코드 추가됨] 클릭한 아이템이 현재 장착 중인 아이템인지 확인!
            bool isAlreadyEquipped = (clickedItem == PlayerManager.Instance.equippedKarinItem);

            // 장착 중인 아이템을 눌렀다면 true(Remove 활성화), 아니면 false(Equip 활성화)를 넘겨줍니다.
            ShowPreview(clickedItem, isEquippedState: isAlreadyEquipped);
        }
    }

    // 스크롤 위로 (한 줄 올리기)
    public void OnClickUpScroll()
    {
        if (currentRow > 0)
        {
            currentRow--;
            RefreshInventory();
        }
    }

    // 스크롤 아래로 (한 줄 내리기)
    public void OnClickDownScroll()
    {
        List<KarinItemData> ownedList = PlayerManager.Instance.ownedKarinItems;
        int totalRows = Mathf.CeilToInt((float)ownedList.Count / columns);

        if (currentRow + visibleRows < totalRows)
        {
            currentRow++;
            RefreshInventory();
        }
    }

    public void OnClickEquip()
    {
        if (currentPreview == null) return;

        PlayerManager.Instance.equippedKarinItem = currentPreview;
        ShowPreview(currentPreview, isEquippedState: true); // 장착 완료 상태로 대사/버튼 전환
        RefreshInventory(); // 혹시 장착 상태 표기가 필요하다면 리프레시
    }

    public void OnClickRemove()
    {
        PlayerManager.Instance.equippedKarinItem = null;
        ShowPreview(null, isEquippedState: false);
        RefreshInventory();
    }

    public void OnClickCancel()
    {
        // 취소하면 다시 원래 장착 중이던 아이템으로 되돌아감!
        ShowPreview(PlayerManager.Instance.equippedKarinItem, isEquippedState: true);
    }
}