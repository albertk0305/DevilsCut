using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class CombatUIManager : MonoBehaviour
{
    public static CombatUIManager Instance;

    [Header("UI 모듈 연결 (신설 부서)")]
    public EntityStatusUI playerStatusUI;
    public EntityStatusUI enemyStatusUI;
    public ActionMenuUI actionMenuUI;
    public CombatVisualUI visualUI;

    [Header("기타 UI (매니저 직속)")]
    public Image[] turnOrderIcons;
    public Image karinProfileImage;
    public Image supporterProfileImage;

    [Header("2배속 UI")]
    public GameObject fastCombatIcon;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        // 전투 씬이 시작될 때, 현재 2배속 상태를 확인하고 즉시 적용합니다.
        bool isFast = PlayerPrefs.GetInt("FastCombat", 0) == 1;
        Time.timeScale = isFast ? 2.0f : 1.0f;

        UpdateFastCombatIcon(isFast);
    }

    private void OnDestroy()
    {
        Time.timeScale = 1.0f;
        DevLog.Log("전투 종료: 타임스케일을 1.0으로 복구합니다.");
    }

    public void UpdateFastCombatIcon(bool isFast)
    {
        if (fastCombatIcon != null)
        {
            fastCombatIcon.SetActive(isFast);
        }
    }

    // ==========================================
    // 0. 이벤트 방송 구독 (수신 후 각 부서로 전달)
    // ==========================================
    private void OnEnable()
    {
        BattleEventSystem.OnHpChanged += HandleHpChanged;
        BattleEventSystem.OnDamageTaken += HandleDamageTaken;
        BattleEventSystem.OnEvaded += HandleEvaded;
    }

    private void OnDisable()
    {
        BattleEventSystem.OnHpChanged -= HandleHpChanged;
        BattleEventSystem.OnDamageTaken -= HandleDamageTaken;
        BattleEventSystem.OnEvaded -= HandleEvaded;
    }

    private void HandleHpChanged(bool isPlayer, int currentHp, int maxHp)
    {
        if (isPlayer) playerStatusUI.UpdateHP(currentHp, maxHp);
        else enemyStatusUI.UpdateHP(currentHp, maxHp);
    }

    private void HandleDamageTaken(bool isPlayerTarget, int damage, bool isCrit) => visualUI.SpawnDamageText(damage.ToString(), isCrit, isPlayerTarget);
    private void HandleEvaded(bool isPlayerTarget) => visualUI.SpawnDamageText("Miss", false, isPlayerTarget);

    // ==========================================
    // 1. 초기 셋업 위임
    // ==========================================
    public void InitPlayerUI(int maxHp, int currentHp, Sprite normalSprite) => playerStatusUI.InitUI(maxHp, currentHp, normalSprite);
    public void InitEnemyUI(int maxHp, int currentHp, Sprite enemySprite) => enemyStatusUI.InitUI(maxHp, currentHp, enemySprite);

    public void InitProfiles(Sprite karinSprite, Sprite supporterSprite)
    {
        if (karinProfileImage != null && karinSprite != null) karinProfileImage.sprite = karinSprite;
        if (supporterProfileImage != null)
        {
            supporterProfileImage.gameObject.SetActive(supporterSprite != null);
            if (supporterSprite != null) supporterProfileImage.sprite = supporterSprite;
        }
    }

    public void ShowFantasticDreamerDice(int count, bool isPlayerCaster)
    {
        visualUI.ShowDiceVisual(count, isPlayerCaster);
    }

    public void ClearCombatEffects()
    {
        if (visualUI != null)
        {
            visualUI.ClearCombatEffects();
            visualUI.ClearDiceVisual(); // [추가됨]
        }
    }

    public void UpdateProfileImages(Sprite karinSprite, Sprite supporterSprite)
    {
        if (karinProfileImage != null && karinSprite != null) karinProfileImage.sprite = karinSprite;
        if (supporterProfileImage != null && supporterSprite != null) supporterProfileImage.sprite = supporterSprite;
    }

    // ==========================================
    // 2. 기타 직속 기능 (턴 대기열)
    // ==========================================
    public void UpdateTurnOrderUI(List<Sprite> icons)
    {
        for (int i = 0; i < turnOrderIcons.Length; i++)
        {
            if (i < icons.Count && icons[i] != null)
            {
                turnOrderIcons[i].gameObject.SetActive(true);
                turnOrderIcons[i].sprite = icons[i];
            }
            else turnOrderIcons[i].gameObject.SetActive(false);
        }
    }

    // ==========================================
    // 3. 하단 메뉴 버튼 위임
    // ==========================================
    public void SetActionPanelActive(bool isActive) => actionMenuUI.SetActionPanelActive(isActive);
    public void SetWaitingPanelActive(bool isActive) => actionMenuUI.SetWaitingPanelActive(isActive);
    public void UpdateActionButtonsForCategory(string[] categoryKeys) => actionMenuUI.UpdateCategoryButtons(categoryKeys);
    public void UpdateActionButtonsForSkills(List<SkillData> skills, StyleRank currentRank) => actionMenuUI.UpdateSkillButtons(skills, currentRank);
    public void UpdateStyleRankUI(StyleRank rank) => actionMenuUI.UpdateStyleRank(rank);

    // ==========================================
    // 4. 전투 데이터 및 연출 위임
    // ==========================================
    public void SetCasterImage(bool isPlayer, Sprite skillSprite)
    {
        if (isPlayer) playerStatusUI.SetProfileImage(skillSprite);
        else enemyStatusUI.SetProfileImage(skillSprite);
    }

    public void SetDefenderImage(bool isPlayer, Sprite reactionSprite)
    {
        if (isPlayer) playerStatusUI.SetProfileImage(reactionSprite);
        else enemyStatusUI.SetProfileImage(reactionSprite);
    }

    public void ResetCasterImage(bool isPlayer)
    {
        if (isPlayer) playerStatusUI.ResetProfileImage();
        else enemyStatusUI.ResetProfileImage();
    }

    public void ResetDefenderImage(bool isPlayer)
    {
        if (isPlayer) playerStatusUI.ResetProfileImage();
        else enemyStatusUI.ResetProfileImage();
    }

    public void UpdatePlayerBreak(float breakValue) => playerStatusUI.UpdateBreak(breakValue);
    public void UpdateEnemyBreak(float breakValue) => enemyStatusUI.UpdateBreak(breakValue);

    public IEnumerator TypeCommentary(string message, bool autoProceed = true, float delayAfter = 1.5f) => visualUI.TypeCommentary(message, autoProceed, delayAfter);
    public void InterruptAndTypeCommentary(string message) => visualUI.InterruptAndTypeCommentary(message);
    public IEnumerator ShowCutIn(Sprite cutInSprite) => visualUI.ShowCutIn(cutInSprite);
    public void SpawnDamageText(string text, bool isCrit, bool isPlayerTarget) => visualUI.SpawnDamageText(text, isCrit, isPlayerTarget);
    public IEnumerator ShowCritAlert() => visualUI.ShowCritAlert();

    public void RefreshBuffUI()
    {
        playerStatusUI.UpdateBuffs(BuffManager.Instance.GetGroupedEffects(true), true);
        enemyStatusUI.UpdateBuffs(BuffManager.Instance.GetGroupedEffects(false), false);
    }
}