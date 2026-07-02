using UnityEngine;
using UnityEngine.UI;

public class SkillClassTabManager : MonoBehaviour
{
    [Header("패널 연결")]
    public GameObject skillPanel;  // 스킬 조회 패널
    public GameObject classPanel;  // 클래스 시너지 조회 패널
    [SerializeField] private SkillUI_Manager skillUIManager;
    [SerializeField] private SynergyUI_Manager synergyUIManager;

    [Header("버튼 컴포넌트 연결")]
    public Button showSkillButton;
    public Button showClassButton;

    [Header("버튼 배경 이미지 연결 (색상 변경용)")]
    public Image showSkillImage;
    public Image showClassImage;

    [Header("버튼 색상 설정")]
    public Color normalColor = Color.white;
    public Color activeColor = new Color(0.6f, 0.6f, 0.6f);

    private bool isPreviewMode;

    private void OnEnable()
    {
        // 탭이 열릴 때 강제로 스킬 패널을 먼저 띄우기 위해 초기화합니다.
        if (skillPanel != null) skillPanel.SetActive(false);
        OnClickShowSkill();
    }

    private void OnDisable()
    {
        TimeScalePauseManager.ReleasePause(this);
        ClearPreviewMode();
    }

    // =========================================================
    // [신규 추가] 캔버스를 열 때 호출할 함수 (시간 정지)
    // =========================================================
    public void OpenCanvas()
    {
        ClearPreviewMode();
        TimeScalePauseManager.RequestPause(this);
        DevLog.Log("[메뉴] 스킬/시너지 창 열기: 시간 정지");

        gameObject.SetActive(true);
    }

    public void OpenPreview(ClearRecordPlayerProfile profile)
    {
        isPreviewMode = true;
        ResolveSkillUIManager();
        ResolveSynergyUIManager();
        if (skillUIManager != null)
            skillUIManager.SetPreviewProfile(profile);
        if (synergyUIManager != null)
            synergyUIManager.SetPreviewProfile(profile);

        TimeScalePauseManager.RequestPause(this);
        DevLog.Log($"[MainMenu] ClearRecord skill preview opened: clearId={profile?.ClearId}");

        gameObject.SetActive(true);
        OnClickShowSkill();
        if (skillUIManager != null)
            skillUIManager.RefreshSkillCanvas();
    }

    // =========================================================
    // [수정] 캔버스를 닫을 때 시간 복구 로직 추가
    // =========================================================
    public void CloseCanvas()
    {
        TimeScalePauseManager.ReleasePause(this);
        DevLog.Log("[메뉴] 스킬/시너지 창 닫기: 시간 갱신");

        ClearPreviewMode();
        gameObject.SetActive(false);
    }

    // [ShowSkill] 버튼을 눌렀을 때 호출
    public void OnClickShowSkill()
    {
        if (skillPanel != null && skillPanel.activeSelf) return;

        if (skillPanel != null) skillPanel.SetActive(true);
        if (classPanel != null) classPanel.SetActive(false);

        if (showSkillImage != null) showSkillImage.color = activeColor;
        if (showClassImage != null) showClassImage.color = normalColor;
    }

    // [ShowClass] 버튼을 눌렀을 때 호출
    public void OnClickShowClass()
    {
        if (classPanel != null && classPanel.activeSelf) return;

        if (skillPanel != null) skillPanel.SetActive(false);
        if (classPanel != null) classPanel.SetActive(true);

        if (showSkillImage != null) showSkillImage.color = normalColor;
        if (showClassImage != null) showClassImage.color = activeColor;

        if (isPreviewMode)
        {
            ResolveSynergyUIManager();
            if (synergyUIManager != null)
                synergyUIManager.RefreshSynergyCanvas();
        }
    }

    private void ClearPreviewMode()
    {
        isPreviewMode = false;
        ResolveSynergyUIManager();
        if (synergyUIManager != null)
            synergyUIManager.ClearPreviewProfile();

        ResolveSkillUIManager();
        if (skillUIManager != null)
            skillUIManager.ClearPreviewProfile();

        if (showClassButton != null)
            showClassButton.interactable = true;
    }

    private void ResolveSkillUIManager()
    {
        if (skillUIManager == null && skillPanel != null)
            skillUIManager = skillPanel.GetComponentInChildren<SkillUI_Manager>(true);
    }

    private void ResolveSynergyUIManager()
    {
        if (synergyUIManager == null && classPanel != null)
            synergyUIManager = classPanel.GetComponentInChildren<SynergyUI_Manager>(true);
    }
}
