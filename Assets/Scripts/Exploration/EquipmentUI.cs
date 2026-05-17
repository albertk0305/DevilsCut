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
    public TextMeshProUGUI itemStatsText;

    [Header("미리보기 별 UI")]
    public GameObject[] previewStars; // 3개의 별 오브젝트를 순서대로 넣으세요.

    [Header("하단 장비 목록")]
    public Button[] inventoryButtons;
    public Button upScrollButton;
    public Button downScrollButton;

    // [수정된 부분] 색상 대신 Sprite(이미지 원본)를 받습니다.
    [Header("인벤토리 슬롯 테두리 (성급별 이미지)")]
    public Image[] inventoryBorders; // 30개의 테두리(배경) 이미지를 넣으세요.
    public Sprite border1Star; // 1성 전용 테두리 이미지
    public Sprite border2Star; // 2성 전용 테두리 이미지
    public Sprite border3Star; // 3성 전용 테두리 이미지

    private OwnedItem currentPreviewItem;
    private int currentRow = 0;
    private const int columns = 10;

    private void OnEnable()
    {
        ShowPreview(null);
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
        ShowPreview(currentPreviewItem);
    }

    private void ShowPreview(OwnedItem item)
    {
        currentPreviewItem = item;

        if (item == null || item.data == null)
        {
            mainItemImage.gameObject.SetActive(false);
            itemNameText.text = "";
            itemDescText.text = "";
            itemStatsText.text = "";

            foreach (var star in previewStars)
                if (star != null) star.SetActive(false);
        }
        else
        {
            mainItemImage.gameObject.SetActive(true);
            mainItemImage.sprite = item.data.itemIcon;

            itemNameText.text = LocalizationManager.Instance.GetText(item.data.itemNameKey);
            itemDescText.text = LocalizationManager.Instance.GetText(item.data.itemDescKey);
            itemStatsText.text = LocalizationManager.Instance.GetText(item.data.itemBonusKey);

            for (int i = 0; i < previewStars.Length; i++)
            {
                if (previewStars[i] != null)
                {
                    previewStars[i].SetActive(i < item.starLevel);
                }
            }
        }
    }

    private void RefreshInventory()
    {
        List<OwnedItem> ownedList = PlayerManager.Instance.inventory;
        int startIndex = currentRow * columns;

        for (int i = 0; i < inventoryButtons.Length; i++)
        {
            int dataIndex = startIndex + i;
            bool hasData = dataIndex < ownedList.Count;

            inventoryButtons[i].gameObject.SetActive(hasData);

            if (inventoryBorders.Length > i && inventoryBorders[i] != null)
                inventoryBorders[i].gameObject.SetActive(hasData);

            if (hasData)
            {
                inventoryButtons[i].image.sprite = ownedList[dataIndex].data.itemIcon;

                // [핵심 수정] 성급에 따라 스프라이트 원본을 교체합니다.
                if (inventoryBorders.Length > i && inventoryBorders[i] != null)
                {
                    int star = ownedList[dataIndex].starLevel;

                    // 만약 이전에 색상을 건드렸을 경우를 대비해 흰색(원래 색)으로 초기화
                    inventoryBorders[i].color = Color.white;

                    if (star == 1) inventoryBorders[i].sprite = border1Star;
                    else if (star == 2) inventoryBorders[i].sprite = border2Star;
                    else if (star >= 3) inventoryBorders[i].sprite = border3Star;
                }
            }
        }

        int totalRows = Mathf.Max(1, Mathf.CeilToInt((float)ownedList.Count / columns));
        int visibleRows = inventoryButtons.Length / columns;

        upScrollButton.interactable = (currentRow > 0);
        downScrollButton.interactable = (currentRow + visibleRows < totalRows);
    }

    public void OnClickInventorySlot(int slotIndex)
    {
        int dataIndex = (currentRow * columns) + slotIndex;

        if (dataIndex < PlayerManager.Instance.inventory.Count)
        {
            ShowPreview(PlayerManager.Instance.inventory[dataIndex]);
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