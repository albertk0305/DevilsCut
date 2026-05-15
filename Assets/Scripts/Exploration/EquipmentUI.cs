using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EquipmentUI : MonoBehaviour
{
    [Header("메인 디스플레이 (좌상단 & 우상단)")]
    public Image mainItemImage;
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI itemDescText;
    public TextMeshProUGUI itemStatsText; // [추가] 스탯을 보여줄 텍스트 (우상단)

    [Header("하단 장비 목록")]
    public Button[] inventoryButtons; // 총 30개의 슬롯 버튼 (좌->우, 상->하 순서)
    public Button upScrollButton;
    public Button downScrollButton;

    // 내부 상태 관리
    private EquipmentItemData currentPreview;
    private int currentRow = 0; // 현재 스크롤 맨 윗줄 번호
    private const int columns = 10; // 한 줄에 10칸 (10열)

    private void OnEnable()
    {
        ShowPreview(null); // 처음 열었을 때는 아무것도 선택 안 된 상태로 비워둠
        currentRow = 0;
        RefreshInventory();

        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged += RefreshLanguage;
    }

    private void OnDisable()
    {
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged -= RefreshLanguage;
    }

    private void RefreshLanguage()
    {
        ShowPreview(currentPreview);
    }

    // 상단 패널 갱신 함수
    private void ShowPreview(EquipmentItemData data)
    {
        currentPreview = data;

        if (data == null)
        {
            mainItemImage.gameObject.SetActive(false);

            itemNameText.text = "";
            itemDescText.text = "";
            itemStatsText.text = "";
        }
        else
        {
            mainItemImage.gameObject.SetActive(true);
            mainItemImage.sprite = data.itemIcon;

            // 다국어 매니저를 통해 이름과 설명을 가져옵니다.
            itemNameText.text = LocalizationManager.Instance.GetText(data.itemNameKey);
            itemDescText.text = LocalizationManager.Instance.GetText(data.itemDescKey);
            itemStatsText.text = LocalizationManager.Instance.GetText(data.itemBonusKey);
        }
    }

    // 하단 인벤토리 리스트 갱신 함수
    private void RefreshInventory()
    {
        // [수정] 타입과 리스트 이름을 OwnedItem과 inventory로 변경
        List<OwnedItem> ownedList = PlayerManager.Instance.inventory;
        int startIndex = currentRow * columns;

        for (int i = 0; i < inventoryButtons.Length; i++)
        {
            int dataIndex = startIndex + i;
            bool hasData = dataIndex < ownedList.Count;

            inventoryButtons[i].gameObject.SetActive(hasData);

            if (hasData)
            {
                // [수정] ownedList[dataIndex]는 OwnedItem이므로 .data를 붙여서 원본에 접근
                inventoryButtons[i].image.sprite = ownedList[dataIndex].data.itemIcon;
            }
        }

        int totalRows = Mathf.Max(1, Mathf.CeilToInt((float)ownedList.Count / columns));
        int visibleRows = inventoryButtons.Length / columns;

        upScrollButton.interactable = (currentRow > 0);
        downScrollButton.interactable = (currentRow + visibleRows < totalRows);
    }

    // 버튼 클릭 시 (인스펙터에서 0~29번까지 매핑)
    public void OnClickInventorySlot(int slotIndex)
    {
        int dataIndex = (currentRow * columns) + slotIndex;

        // [수정] inventory로 변경
        if (dataIndex < PlayerManager.Instance.inventory.Count)
        {
            // [수정] ShowPreview는 원본 데이터를 요구하므로 .data를 넘겨줌
            ShowPreview(PlayerManager.Instance.inventory[dataIndex].data);
        }
    }

    public void OnClickUpScroll()
    {
        if (currentRow > 0)
        {
            currentRow--;
            RefreshInventory();
        }
    }

    public void OnClickDownScroll()
    {
        // [수정] 타입과 리스트 이름 변경
        List<OwnedItem> ownedList = PlayerManager.Instance.inventory;
        int totalRows = Mathf.Max(1, Mathf.CeilToInt((float)ownedList.Count / columns));
        int visibleRows = inventoryButtons.Length / columns;

        if (currentRow + visibleRows < totalRows)
        {
            currentRow++;
            RefreshInventory();
        }
    }
}