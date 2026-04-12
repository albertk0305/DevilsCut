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

        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged += RefreshLanguage;
    }

    private void OnDisable()
    {
        // [추가] 창이 꺼질 때는 에러 방지를 위해 구독 취소
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged -= RefreshLanguage;
    }

    private void RefreshLanguage()
    {
        // 현재 띄워진 조력자 상태 그대로 텍스트만 다시 불러옵니다.
        bool isJoined = (currentPreview != null && currentPreview == PlayerManager.Instance.activeSupporter);
        ShowPreview(currentPreview, isJoined);
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
            dialogueText.text = LocalizationManager.Instance.GetText("msg_no_supporter");

            joinButton.interactable = false;
            leaveButton.interactable = false;
            if (cancelButton != null) cancelButton.gameObject.SetActive(false);
        }
        else
        {
            mainImage.gameObject.SetActive(true);

            supporterNameText.text = LocalizationManager.Instance.GetText(data.supporterName);
            mainImage.sprite = data.mainImage;
            passiveText.text = LocalizationManager.Instance.GetText(data.passiveSkillDesc);
            startText.text = LocalizationManager.Instance.GetText(data.startSkillDesc);
            battleText.text = LocalizationManager.Instance.GetText(data.battleSkillDesc);

            string dialogueKey = isJoinedState ? data.joinMessage : data.selectMessage;
            dialogueText.text = LocalizationManager.Instance.GetText(dialogueKey);

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

            // 1. 현재 슬롯에 들어갈 데이터가 존재하는지 여부를 bool로 판별
            bool hasData = dataIndex < displayList.Count;

            // 2. 데이터 유무에 따라 버튼 켜기/끄기, 클릭 활성화/비활성화를 한 번에 처리!
            rosterButtons[i].gameObject.SetActive(hasData);
            rosterButtons[i].interactable = hasData;

            // 3. 데이터가 있을 때만 이미지 교체
            if (hasData)
            {
                rosterButtons[i].image.sprite = displayList[dataIndex].iconImage;
            }

            // 4. 배경 이미지 켜기/끄기도 if/else 없이 한 줄로 압축!
            if (rosterBackgrounds.Length > i && rosterBackgrounds[i] != null)
            {
                rosterBackgrounds[i].SetActive(hasData);
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

        ShowPreview(currentPreview, isJoinedState: true);

        RefreshRosterList();
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