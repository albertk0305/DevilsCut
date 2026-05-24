using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillUI_Slot : MonoBehaviour
{
    [Header("UI 연결")]
    public TextMeshProUGUI skillNameText;
    public TextMeshProUGUI skillLevelText;
    public GameObject evolutionBorder;
    public Button slotButton;

    private SkillData mySkill;
    private SkillUI_Manager myManager;

    public void InitSlot(SkillData skill, SkillUI_Manager manager)
    {
        mySkill = skill;
        myManager = manager;

        // Keep the placeholder for missing skill data.
        if (mySkill == null)
        {
            skillNameText.text = "???";
            skillLevelText.text = "";
            evolutionBorder.SetActive(false);
            slotButton.interactable = false;
            return;
        }

        slotButton.interactable = true;

        skillNameText.text = LocalizationManager.Instance != null ? LocalizationManager.Instance.GetText(skill.skillNameKey) : skill.skillNameKey;
        skillLevelText.text = skill.skillLevel.ToString();

        bool isEvolved = skill.currentEvolution != SkillEvolution.None;
        evolutionBorder.SetActive(isEvolved);

        slotButton.onClick.RemoveAllListeners();
        slotButton.onClick.AddListener(OnClickSlot);
    }

    private void OnClickSlot()
    {
        if (myManager != null && mySkill != null)
        {
            myManager.ShowSkillDescription(mySkill);
        }
    }
}
