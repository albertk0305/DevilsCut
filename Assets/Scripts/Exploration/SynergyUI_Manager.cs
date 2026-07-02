using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class SynergyUI_Manager : MonoBehaviour
{
    private const string SelectSynergyPromptKey = "ui_select_synergy_prompt";
    private const string ActivatedKey = "ui_status_activated";
    private const string DeactivatedKey = "ui_status_deactivated";

    public List<SynergyUI_Column> allColumns;
    public TextMeshProUGUI descriptionText;

    private ClearRecordPlayerProfile previewProfile;

    private void OnEnable()
    {
        RefreshSynergyCanvas();

        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged += RefreshCurrentText;
    }

    private void OnDisable()
    {
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged -= RefreshCurrentText;
    }

    public void RefreshSynergyCanvas()
    {
        if (previewProfile != null)
        {
            RefreshPreviewSynergyCanvas();
            return;
        }

        if (PlayerManager.Instance == null) return;

        var syn = PlayerManager.Instance.GetCurrentSynergies();
        foreach (var column in allColumns)
        {
            int points = 0;

            if (column.myClass == ItemClass.LoneWolf)
            {
                points = PlayerManager.Instance.stats.rejectedSupporterCount;
            }
            else if (syn.ContainsKey(column.myClass))
            {
                points = syn[column.myClass];
            }

            column.UpdateColumn(points, this);
        }

        SetDefaultPrompt();
    }

    public void SetPreviewProfile(ClearRecordPlayerProfile profile)
    {
        previewProfile = profile;

        if (isActiveAndEnabled)
            RefreshSynergyCanvas();
    }

    public void ClearPreviewProfile()
    {
        previewProfile = null;
    }

    private void RefreshPreviewSynergyCanvas()
    {
        Dictionary<ItemClass, int> syn = previewProfile.GetPreviewSynergies();
        foreach (var column in allColumns)
        {
            if (column == null)
                continue;

            int points = 0;
            if (column.myClass == ItemClass.LoneWolf)
            {
                points = previewProfile.GetRejectedSupporterCount();
            }
            else if (syn != null && syn.ContainsKey(column.myClass))
            {
                points = syn[column.myClass];
            }

            column.UpdateColumn(points, this);
        }

        SetDefaultPrompt();
    }

    private void SetDefaultPrompt()
    {
        if (descriptionText == null)
            return;

        descriptionText.text = GetLocalizedText(SelectSynergyPromptKey);
    }

    private void RefreshCurrentText()
    {
        SetDefaultPrompt();
    }

    public void ShowDescription(string nameKey, string descKey, bool isActive)
    {
        string nameStr = GetLocalizedText(nameKey);
        string descStr = GetLocalizedText(descKey);

        string statusKey = isActive ? ActivatedKey : DeactivatedKey;
        string statusText = GetLocalizedText(statusKey);
        string statusColor = isActive ? "#00FF00" : "#888888";
        string statusTag = $"<color={statusColor}>[{statusText}]</color>";

        descriptionText.text = $"<b>{nameStr}</b> {statusTag}\n\n{descStr}";
    }

    private string GetLocalizedText(string key)
    {
        return LocalizationManager.Instance != null
            ? LocalizationManager.Instance.GetText(key)
            : key;
    }
}
