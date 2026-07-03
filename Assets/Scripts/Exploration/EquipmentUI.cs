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
    public TextMeshProUGUI itemClassText;

    [Header("미리보기 별 UI")]
    public GameObject[] previewStars;

    [Header("하단 장비 목록")]
    public Button[] inventoryButtons;
    public Button upScrollButton;
    public Button downScrollButton;

    [Header("인벤토리 슬롯 테두리 (성급별 이미지)")]
    public Image[] inventoryBorders;
    public Sprite border1Star;
    public Sprite border2Star;
    public Sprite border3Star;

    private OwnedItem currentPreviewItem;
    private int currentRow = 0;
    private const int columns = 10;
    private ClearRecordPlayerProfile previewProfile;

    private void OnEnable()
    {
        if (previewProfile != null)
        {
            ShowPreview(null);
            currentRow = 0;
            RefreshPreviewInventory();
            SubscribeLanguageChanged();
            return;
        }

        ShowPreview(null);
        currentRow = 0;
        RefreshInventory();

        SubscribeLanguageChanged();
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
            if (itemClassText != null) itemClassText.text = "";

            foreach (var star in previewStars)
                if (star != null) star.SetActive(false);
        }
        else
        {
            mainItemImage.gameObject.SetActive(true);
            mainItemImage.sprite = item.data.itemIcon;

            itemNameText.text = LocalizationManager.Instance.GetText(item.data.itemNameKey);
            if (itemClassText != null)
            {
                itemClassText.text = $"<color=#FFD700>[ {item.data.itemClass.ToString()} ]</color>";

                // itemClassText.text = LocalizationManager.Instance.GetText("class_" + item.data.itemClass.ToString().ToLower());
            }
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
        if (previewProfile != null)
        {
            RefreshPreviewInventory();
            return;
        }

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

                if (inventoryBorders.Length > i && inventoryBorders[i] != null)
                {
                    int star = ownedList[dataIndex].starLevel;

                    // Reset color before applying the star border sprite.
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
        if (previewProfile != null)
        {
            OnClickPreviewInventorySlot(slotIndex);
            return;
        }

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
        if (previewProfile != null)
        {
            IReadOnlyList<OwnedItem> previewList = previewProfile.Inventory;
            int previewTotalRows = Mathf.Max(1, Mathf.CeilToInt((float)previewList.Count / columns));
            int previewVisibleRows = inventoryButtons.Length / columns;

            if (currentRow + previewVisibleRows < previewTotalRows)
            {
                currentRow++;
                RefreshPreviewInventory();
            }

            return;
        }

        List<OwnedItem> ownedList = PlayerManager.Instance.inventory;
        int totalRows = Mathf.Max(1, Mathf.CeilToInt((float)ownedList.Count / columns));
        int visibleRows = inventoryButtons.Length / columns;

        if (currentRow + visibleRows < totalRows)
        {
            currentRow++;
            RefreshInventory();
        }
    }

    public void SetPreviewProfile(ClearRecordPlayerProfile profile)
    {
        previewProfile = profile;
        currentRow = 0;

        if (isActiveAndEnabled)
        {
            ShowPreview(null);
            RefreshPreviewInventory();
        }
    }

    public void ClearPreviewProfile()
    {
        previewProfile = null;
    }

    private void RefreshPreviewInventory()
    {
        IReadOnlyList<OwnedItem> ownedList = previewProfile != null ? previewProfile.Inventory : null;
        int ownedCount = ownedList != null ? ownedList.Count : 0;
        int startIndex = currentRow * columns;

        for (int i = 0; i < inventoryButtons.Length; i++)
        {
            int dataIndex = startIndex + i;
            bool hasData = dataIndex < ownedCount;

            inventoryButtons[i].gameObject.SetActive(hasData);

            if (inventoryBorders.Length > i && inventoryBorders[i] != null)
                inventoryBorders[i].gameObject.SetActive(hasData);

            if (hasData)
            {
                OwnedItem item = ownedList[dataIndex];
                inventoryButtons[i].image.sprite = item != null && item.data != null ? item.data.itemIcon : null;

                if (inventoryBorders.Length > i && inventoryBorders[i] != null)
                {
                    int star = item != null ? item.starLevel : 1;
                    inventoryBorders[i].color = Color.white;

                    if (star == 1) inventoryBorders[i].sprite = border1Star;
                    else if (star == 2) inventoryBorders[i].sprite = border2Star;
                    else if (star >= 3) inventoryBorders[i].sprite = border3Star;
                }
            }
        }

        int totalRows = Mathf.Max(1, Mathf.CeilToInt((float)ownedCount / columns));
        int visibleRows = inventoryButtons.Length / columns;

        upScrollButton.interactable = currentRow > 0;
        downScrollButton.interactable = currentRow + visibleRows < totalRows;
    }

    private void OnClickPreviewInventorySlot(int slotIndex)
    {
        IReadOnlyList<OwnedItem> ownedList = previewProfile != null ? previewProfile.Inventory : null;
        if (ownedList == null)
            return;

        int dataIndex = (currentRow * columns) + slotIndex;
        if (dataIndex < ownedList.Count)
            ShowPreview(ownedList[dataIndex]);
    }

    private void SubscribeLanguageChanged()
    {
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged += RefreshLanguage;
    }
}
