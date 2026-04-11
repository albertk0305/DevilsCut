using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

//서포터 선택 UI 제어 코드
public class SupporterUI : MonoBehaviour
{
    [Header("메인 디스플레이")]
    public Image mainImage;
    public TextMeshProUGUI passiveText;
    public TextMeshProUGUI startText;
    public TextMeshProUGUI battleText;
    public TextMeshProUGUI dialogueText;
    public TextMeshProUGUI supporterNameText;

    [Header("하단 대기열 목록")]
    public Button[] rosterButtons;
    public GameObject[] rosterBackgrounds;
    public GameObject leftArrow;
    public GameObject rightArrow;

    [Header("버튼")]
    public Button joinButton;
    public Button leaveButton;
    public Button cancelButton;

    // 내부 상태 관리
    private SupporterData currentPreview;
    private List<SupporterData> displayList = new List<SupporterData>();
    private int currentPage = 0;

    private void OnEnable()
    {
        ShowPreview(PlayerManager.Instance.activeSupporter, isJoinedState: true);
        RefreshRosterList();
    }

    // 메인 화면 업데이트
    private void ShowPreview(SupporterData data, bool isJoinedState)
    {
        currentPreview = data;

        if (data == null)
        {
            mainImage.gameObject.SetActive(false);

            supporterNameText.text = "";
            passiveText.text = "";
            startText.text = "";
            battleText.text = "";
            dialogueText.text = "현재 조력자가 선택되어 있지 않습니다.";

            joinButton.interactable = false;
            leaveButton.interactable = false;
            if (cancelButton != null) cancelButton.gameObject.SetActive(false);
        }
        else
        {
            mainImage.gameObject.SetActive(true);

            supporterNameText.text = data.supporterName;
            mainImage.sprite = data.mainImage;
            passiveText.text = data.passiveSkillDesc;
            startText.text = data.startSkillDesc;
            battleText.text = data.battleSkillDesc;

            dialogueText.text = isJoinedState ? data.joinMessage : data.selectMessage;

            joinButton.interactable = !isJoinedState;
            leaveButton.interactable = isJoinedState;
            if (cancelButton != null) cancelButton.gameObject.SetActive(!isJoinedState);
        }
    }

    // 하단 목록 업데이트
    private void RefreshRosterList()
    {
        displayList = PlayerManager.Instance.unlockedSupporters
            .Where(s => s != PlayerManager.Instance.activeSupporter)
            .ToList();

        int totalPages = GetTotalPages();
        if (currentPage >= totalPages && currentPage > 0) currentPage = totalPages - 1;

        // [수정됨] rosterIcons.Length 대신 rosterButtons.Length 사용
        int startIndex = currentPage * rosterButtons.Length;

        for (int i = 0; i < rosterButtons.Length; i++)
        {
            int dataIndex = startIndex + i;
            if (dataIndex < displayList.Count)
            {
                // [수정됨] 버튼 자체를 켜고, 버튼의 image에 접근해서 스프라이트를 바꿉니다!
                rosterButtons[i].gameObject.SetActive(true);
                rosterButtons[i].image.sprite = displayList[dataIndex].iconImage;
                rosterButtons[i].interactable = true;

                if (rosterBackgrounds.Length > i && rosterBackgrounds[i] != null)
                    rosterBackgrounds[i].SetActive(true);
            }
            else
            {
                // 빈 칸이면 버튼과 배경 숨기기
                rosterButtons[i].gameObject.SetActive(false);
                rosterButtons[i].interactable = false;

                if (rosterBackgrounds.Length > i && rosterBackgrounds[i] != null)
                    rosterBackgrounds[i].SetActive(false);
            }
        }

        bool hasMultiplePages = totalPages > 1;
        leftArrow.SetActive(hasMultiplePages);
        rightArrow.SetActive(hasMultiplePages);
    }

    public void OnClickRosterIcon(int slotIndex)
    {
        // [수정됨] rosterButtons.Length 사용
        int dataIndex = (currentPage * rosterButtons.Length) + slotIndex;
        if (dataIndex < displayList.Count)
        {
            ShowPreview(displayList[dataIndex], isJoinedState: false);
        }
    }

    public void OnClickLeftArrow()
    {
        currentPage--;
        int totalPages = GetTotalPages();
        if (currentPage < 0) currentPage = totalPages - 1;
        RefreshRosterList();
    }

    public void OnClickRightArrow()
    {
        currentPage++;
        int totalPages = GetTotalPages();
        if (currentPage >= totalPages) currentPage = 0;
        RefreshRosterList();
    }

    public void OnClickJoin()
    {
        if (currentPreview == null) return;

        PlayerManager.Instance.activeSupporter = currentPreview;
        dialogueText.text = currentPreview.joinMessage;

        RefreshRosterList();

        joinButton.interactable = false;
        leaveButton.interactable = true;
    }

    public void OnClickLeave()
    {
        PlayerManager.Instance.activeSupporter = null;
        ShowPreview(null, isJoinedState: false);
        RefreshRosterList();
    }

    public void OnClickCancel()
    {
        // 원래 내 파티에 있던 진짜 조력자(아무도 없었다면 null)를 다시 화면에 띄워줍니다!
        ShowPreview(PlayerManager.Instance.activeSupporter, isJoinedState: true);
    }

    private int GetTotalPages()
    {
        // [수정됨] rosterButtons.Length 사용
        return Mathf.Max(1, Mathf.CeilToInt((float)displayList.Count / rosterButtons.Length));
    }
}