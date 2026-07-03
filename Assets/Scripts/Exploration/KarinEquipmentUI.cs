using System.Collections;
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
    public Image karinFaceImage;
    public TextMeshProUGUI karinDialogueText;

    [Header("우측 인벤토리 목록")]
    public Button[] inventoryButtons;
    public Button upScrollButton;
    public Button downScrollButton;

    [Header("인벤토리 색상 피드백")]
    public Color normalColor = Color.white;
    public Color equippedColor = new Color(0.4f, 0.4f, 0.4f);

    [Header("액션 버튼")]
    public Button equipButton;
    public Button removeButton;
    public Button cancelButton;

    public Sprite karinNormal;
    public Sprite karinReady;

    private KarinItemData currentPreview;
    private int currentRow = 0;
    private const int columns = 2;
    private const int visibleRows = 4;
    private ClearRecordPlayerProfile previewProfile;

    private void OnEnable()
    {
        if (previewProfile != null)
        {
            currentRow = 0;
            RefreshPreview();
            SubscribeLanguageChanged();
            return;
        }

        if (PlayerManager.Instance == null) return;

        currentRow = 0;
        RefreshInventory();

        SubscribeLanguageChanged();

        // Wait one frame so the UI is ready before applying the preview.
        StartCoroutine(InitDelayedPreviewRoutine());
    }

    private void OnDisable()
    {
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged -= RefreshLanguage;
    }

    // Applies the equipped preview after one frame.
    private IEnumerator InitDelayedPreviewRoutine()
    {
        yield return null;

        if (previewProfile != null)
            yield break;

        if (PlayerManager.Instance != null)
        {
            KarinItemData equipped = PlayerManager.Instance.equippedKarinItem;
            ShowPreview(equipped, isEquippedState: true);
        }
    }

    private void RefreshLanguage()
    {
        if (previewProfile != null)
        {
            bool previewEquipped = currentPreview != null && previewProfile.IsEquippedKarinItem(currentPreview.itemID);
            ShowPreview(currentPreview, previewEquipped);
            return;
        }

        bool isEquipped = (currentPreview != null && currentPreview == PlayerManager.Instance.equippedKarinItem);
        ShowPreview(currentPreview, isEquipped);
    }

    // Falls back to the key when localization is missing.
    private string GetSafeText(string key)
    {
        if (string.IsNullOrEmpty(key)) return "";

        if (LocalizationManager.Instance != null)
        {
            string translated = LocalizationManager.Instance.GetText(key);
            return string.IsNullOrEmpty(translated) ? key : translated;
        }
        return key;
    }

    private void ShowPreview(KarinItemData data, bool isEquippedState)
    {
        currentPreview = data;
        bool isExploration = ExplorationManager.Instance != null;

        if (data == null)
        {
            mainItemImage.gameObject.SetActive(false);

            itemNameText.text = "";
            itemDescText.text = GetSafeText("msg_no_equipped_item");
            karinDialogueText.text = GetSafeText("msg_supporter_join_ready");

            if (karinFaceImage != null) karinFaceImage.sprite = karinNormal;

            equipButton.interactable = false;
            removeButton.interactable = false;
            cancelButton.gameObject.SetActive(false);
        }
        else
        {
            mainItemImage.gameObject.SetActive(true);
            mainItemImage.sprite = data.itemIcon;

            itemNameText.text = GetSafeText(data.itemName);
            itemDescText.text = GetSafeText(data.itemDescription);

            string dialogueKey = isEquippedState ? data.equipDialogue : data.previewDialogue;
            karinDialogueText.text = GetSafeText(dialogueKey);

            if (karinFaceImage != null)
                karinFaceImage.sprite = isEquippedState ? karinReady : karinNormal;

            bool canChangeEquipment = previewProfile != null || isExploration;
            equipButton.interactable = !isEquippedState && canChangeEquipment;
            removeButton.interactable = isEquippedState && canChangeEquipment;
            cancelButton.gameObject.SetActive(!isEquippedState);
        }
    }

    private void RefreshInventory()
    {
        if (previewProfile != null)
        {
            RefreshPreviewInventory();
            return;
        }

        List<KarinItemData> ownedList = PlayerManager.Instance.ownedKarinItems;
        int startIndex = currentRow * columns;

        for (int i = 0; i < inventoryButtons.Length; i++)
        {
            int dataIndex = startIndex + i;
            bool hasData = dataIndex < ownedList.Count;

            inventoryButtons[i].image.enabled = hasData;
            inventoryButtons[i].interactable = hasData;

            if (hasData)
            {
                inventoryButtons[i].image.sprite = ownedList[dataIndex].itemIcon;
            }
        }

        int totalRows = Mathf.CeilToInt((float)ownedList.Count / columns);
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
        if (dataIndex < PlayerManager.Instance.ownedKarinItems.Count)
        {
            KarinItemData clickedItem = PlayerManager.Instance.ownedKarinItems[dataIndex];
            bool isAlreadyEquipped = (clickedItem == PlayerManager.Instance.equippedKarinItem);
            ShowPreview(clickedItem, isEquippedState: isAlreadyEquipped);
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
            int previewTotalRows = Mathf.CeilToInt((float)previewProfile.OwnedKarinItems.Count / columns);
            if (currentRow + visibleRows < previewTotalRows)
            {
                currentRow++;
                RefreshPreviewInventory();
            }

            return;
        }

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

        if (previewProfile != null)
        {
            EquipPreviewItem();
            return;
        }

        PlayerManager.Instance.equippedKarinItem = currentPreview;
        ShowPreview(currentPreview, isEquippedState: true);
        RefreshInventory();
    }

    public void OnClickRemove()
    {
        if (previewProfile != null)
        {
            RemovePreviewItem();
            return;
        }

        PlayerManager.Instance.equippedKarinItem = null;
        ShowPreview(null, isEquippedState: false);
        RefreshInventory();
    }

    public void OnClickCancel()
    {
        if (previewProfile != null)
        {
            ShowPreview(previewProfile.GetEquippedKarinItem(), isEquippedState: true);
            return;
        }

        ShowPreview(PlayerManager.Instance.equippedKarinItem, isEquippedState: true);
    }

    public void SetPreviewProfile(ClearRecordPlayerProfile profile)
    {
        previewProfile = profile;
        currentRow = 0;

        if (isActiveAndEnabled)
            RefreshPreview();
    }

    public void ClearPreviewProfile()
    {
        previewProfile = null;
    }

    private void RefreshPreview()
    {
        if (previewProfile == null)
            return;

        RefreshPreviewInventory();
        ShowPreview(previewProfile.GetEquippedKarinItem(), isEquippedState: true);
    }

    private void RefreshPreviewInventory()
    {
        IReadOnlyList<KarinItemData> ownedList = previewProfile.OwnedKarinItems;
        int startIndex = currentRow * columns;

        for (int i = 0; i < inventoryButtons.Length; i++)
        {
            int dataIndex = startIndex + i;
            bool hasData = dataIndex < ownedList.Count;

            inventoryButtons[i].image.enabled = hasData;
            inventoryButtons[i].interactable = hasData;

            if (hasData)
                inventoryButtons[i].image.sprite = ownedList[dataIndex].itemIcon;
        }

        int totalRows = Mathf.CeilToInt((float)ownedList.Count / columns);
        upScrollButton.interactable = currentRow > 0;
        downScrollButton.interactable = currentRow + visibleRows < totalRows;
    }

    private void OnClickPreviewInventorySlot(int slotIndex)
    {
        int dataIndex = (currentRow * columns) + slotIndex;
        if (dataIndex < previewProfile.OwnedKarinItems.Count)
        {
            KarinItemData clickedItem = previewProfile.OwnedKarinItems[dataIndex];
            bool isAlreadyEquipped = clickedItem != null && previewProfile.IsEquippedKarinItem(clickedItem.itemID);
            ShowPreview(clickedItem, isEquippedState: isAlreadyEquipped);
        }
    }

    private void EquipPreviewItem()
    {
        string previousEquippedItemID = previewProfile.GetEquippedKarinItem() != null
            ? previewProfile.GetEquippedKarinItem().itemID
            : null;

        if (!previewProfile.SetEquippedKarinItem(currentPreview.itemID))
        {
            DevLog.LogWarning($"[MainMenu] ClearRecord Karin equip failed: itemID={currentPreview.itemID}");
            RefreshPreview();
            return;
        }

        if (!SavePreviewRecord())
        {
            RestorePreviewEquippedItem(previousEquippedItemID);
            RefreshPreview();
            return;
        }

        ShowPreview(currentPreview, isEquippedState: true);
        RefreshPreviewInventory();
    }

    private void RemovePreviewItem()
    {
        string previousEquippedItemID = previewProfile.GetEquippedKarinItem() != null
            ? previewProfile.GetEquippedKarinItem().itemID
            : null;

        if (!previewProfile.ClearEquippedKarinItem())
        {
            RefreshPreview();
            return;
        }

        if (!SavePreviewRecord())
        {
            RestorePreviewEquippedItem(previousEquippedItemID);
            RefreshPreview();
            return;
        }

        ShowPreview(null, isEquippedState: false);
        RefreshPreviewInventory();
    }

    private bool SavePreviewRecord()
    {
        if (SaveManager.Instance == null || previewProfile == null || previewProfile.Record == null)
        {
            DevLog.LogWarning("[MainMenu] ClearRecord Karin preview save failed: SaveManager or record missing.");
            return false;
        }

        bool saved = SaveManager.Instance.UpdateGameClearRecord(previewProfile.Record);
        if (!saved)
            DevLog.LogWarning($"[MainMenu] ClearRecord Karin preview save failed: clearId={previewProfile.ClearId}");

        return saved;
    }

    private void RestorePreviewEquippedItem(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
            previewProfile.ClearEquippedKarinItem();
        else
            previewProfile.SetEquippedKarinItem(itemId);
    }

    private void SubscribeLanguageChanged()
    {
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged -= RefreshLanguage;
            LocalizationManager.Instance.OnLanguageChanged += RefreshLanguage;
        }
    }
}
