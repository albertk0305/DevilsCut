using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillUI_Slot : MonoBehaviour
{
    [Header("UI 연결")]
    public TextMeshProUGUI skillNameText;   // 스킬 이름
    public TextMeshProUGUI skillLevelText;  // 우측 상단 1,2,3 레벨
    public GameObject evolutionBorder;      // 노란색 테두리 이미지 오브젝트
    public Button slotButton;               // 클릭 감지용 버튼

    private SkillData mySkill;              // 이 슬롯이 담당하는 스킬 데이터
    private SkillUI_Manager myManager;      // 중앙 통제 매니저

    // 매니저가 이 슬롯을 초기화할 때 부르는 함수
    public void InitSlot(SkillData skill, SkillUI_Manager manager)
    {
        mySkill = skill;
        myManager = manager;

        // 스킬이 없거나 데이터가 누락된 경우 (방어 코드)
        if (mySkill == null)
        {
            skillNameText.text = "???";
            skillLevelText.text = "";
            evolutionBorder.SetActive(false);
            slotButton.interactable = false;
            return;
        }

        slotButton.interactable = true;

        // 1. 이름과 레벨 세팅
        skillNameText.text = LocalizationManager.Instance != null ? LocalizationManager.Instance.GetText(skill.skillNameKey) : skill.skillNameKey;
        skillLevelText.text = skill.skillLevel.ToString();

        // 2. 진화 테두리 ON/OFF (None이 아니면 켜기)
        bool isEvolved = skill.currentEvolution != SkillEvolution.None;
        evolutionBorder.SetActive(isEvolved);

        // 3. 버튼 클릭 이벤트 연결 (기존 연결 지우고 새로 달기)
        slotButton.onClick.RemoveAllListeners();
        slotButton.onClick.AddListener(OnClickSlot);
    }

    // 유저가 이 스킬 버튼을 눌렀을 때!
    private void OnClickSlot()
    {
        if (myManager != null && mySkill != null)
        {
            myManager.ShowSkillDescription(mySkill);
        }
    }
}