using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ClearDataSlotUI : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private GameObject highlightBackground;
    [SerializeField] private GameObject normalBackground;
    [SerializeField] private TextMeshProUGUI clearNumberText;
    [SerializeField] private TextMeshProUGUI floorRecordText;
    [SerializeField] private Button partyButton;
    [SerializeField] private Button skillButton;
    [SerializeField] private Button useButton;

    private ClearRecordSummary boundSummary;
    private Action<ClearRecordSummary> onUse;
    private Action<ClearRecordSummary> onPartyPreview;
    private Action<ClearRecordSummary> onSkillPreview;

    private void Awake()
    {
        if (partyButton != null)
        {
            partyButton.onClick.RemoveListener(OnPartyClicked);
            partyButton.onClick.AddListener(OnPartyClicked);
        }

        if (skillButton != null)
        {
            skillButton.onClick.RemoveListener(OnSkillClicked);
            skillButton.onClick.AddListener(OnSkillClicked);
        }

        if (useButton != null)
        {
            useButton.onClick.RemoveListener(OnUseClicked);
            useButton.onClick.AddListener(OnUseClicked);
        }
    }

    private void OnDestroy()
    {
        if (partyButton != null)
            partyButton.onClick.RemoveListener(OnPartyClicked);

        if (skillButton != null)
            skillButton.onClick.RemoveListener(OnSkillClicked);

        if (useButton != null)
            useButton.onClick.RemoveListener(OnUseClicked);
    }

    public void Bind(
        ClearRecordSummary summary,
        bool selected,
        Action<ClearRecordSummary> useCallback,
        Action<ClearRecordSummary> partyPreviewCallback,
        Action<ClearRecordSummary> skillPreviewCallback)
    {
        boundSummary = summary;
        onUse = useCallback;
        onPartyPreview = partyPreviewCallback;
        onSkillPreview = skillPreviewCallback;

        if (root != null)
            root.SetActive(summary != null);

        if (normalBackground != null)
            normalBackground.SetActive(summary != null);

        SetSelected(selected);

        if (summary == null)
        {
            SetButtonsInteractable(false);
            return;
        }

        if (clearNumberText != null)
            clearNumberText.text = summary.clearNumber.ToString();

        if (floorRecordText != null)
            floorRecordText.text = Mathf.Max(0, summary.infiniteBattleBestFloor).ToString();

        if (partyButton != null)
            partyButton.interactable = true;

        if (skillButton != null)
            skillButton.interactable = true;

        if (useButton != null)
            useButton.interactable = true;
    }

    public void Clear()
    {
        Bind(null, false, null, null, null);
    }

    public void SetSelected(bool selected)
    {
        if (highlightBackground != null)
            highlightBackground.SetActive(selected);
    }

    private void SetButtonsInteractable(bool interactable)
    {
        if (partyButton != null)
            partyButton.interactable = interactable;

        if (skillButton != null)
            skillButton.interactable = interactable;

        if (useButton != null)
            useButton.interactable = interactable;
    }

    private void OnPartyClicked()
    {
        if (boundSummary == null)
            return;

        onPartyPreview?.Invoke(boundSummary);
    }

    private void OnSkillClicked()
    {
        if (boundSummary == null)
            return;

        onSkillPreview?.Invoke(boundSummary);
    }

    private void OnUseClicked()
    {
        if (boundSummary == null)
            return;

        onUse?.Invoke(boundSummary);
    }
}
