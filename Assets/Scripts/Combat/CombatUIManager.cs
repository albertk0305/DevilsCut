using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class CombatUIManager : MonoBehaviour
{
    public static CombatUIManager Instance;

    [Header("아군(Player) UI 연결")]
    public Image playerImage;
    public Slider playerHpSlider;
    public Slider playerBreakSlider;
    public TextMeshProUGUI playerHpText;

    [Header("적(Enemy) UI 연결")]
    public Image enemyImage;
    public Slider enemyHpSlider;
    public Slider enemyBreakSlider;
    public TextMeshProUGUI enemyHpText;

    [Header("UI 프로필 이미지 (왼쪽 구역)")]
    public Image karinProfileImage;
    public Image supporterProfileImage;

    [Header("턴 대기열 UI (위에서부터 1~4등)")]
    public Image[] turnOrderIcons;

    [Header("전투 액션 메뉴 UI")]
    public GameObject actionUIPanel;
    public GameObject waitingPanel;
    public Button[] actionButtons;
    public LocalizedText[] actionButtonTexts;

    [Header("스타일 랭크 UI")]
    public Image styleRankImage;
    public Sprite[] styleRankSprites;

    [Header("전투 해설 텍스트 UI")]
    public TextMeshProUGUI commentaryText;

    [Header("컷인(Cut-in) 연출 UI")]
    public Image cutInImage;      // 실제 캐릭터 컷인이 들어갈 이미지 컴포넌트

    [Header("데미지 및 크리티컬 연출 UI")]
    public GameObject damageTextPrefab; // 아까 만든 데미지 텍스트 프리팹
    public Transform playerDamagePos;   // 아군 데미지가 뜰 기준점
    public Transform enemyDamagePos;    // 적 데미지가 뜰 기준점
    public GameObject critAlertImage;

    // 원래 얼굴로 복구하기 위해 기억해둘 변수
    private Sprite defaultPlayerSprite;
    private Sprite defaultEnemySprite;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    // ==========================================
    // 1. 초기 셋업
    // ==========================================
    public void InitPlayerUI(int maxHp, int currentHp, Sprite normalSprite)
    {
        defaultPlayerSprite = normalSprite;
        playerImage.sprite = normalSprite;
        playerHpSlider.maxValue = maxHp;
        playerHpSlider.value = currentHp;
        if (playerHpText != null) playerHpText.text = $"{currentHp}/{maxHp}";
        playerBreakSlider.maxValue = 100;
        playerBreakSlider.value = 0;
    }

    public void InitEnemyUI(int maxHp, int currentHp, Sprite enemySprite)
    {
        defaultEnemySprite = enemySprite;
        enemyImage.sprite = enemySprite;
        enemyHpSlider.maxValue = maxHp;
        enemyHpSlider.value = currentHp;
        if (enemyHpText != null) enemyHpText.text = $"{currentHp}/{maxHp}";
        enemyBreakSlider.maxValue = 100;
        enemyBreakSlider.value = 0;
    }

    public void InitProfiles(Sprite karinSprite, Sprite supporterSprite)
    {
        if (karinProfileImage != null && karinSprite != null)
            karinProfileImage.sprite = karinSprite;

        if (supporterProfileImage != null)
        {
            if (supporterSprite != null)
            {
                supporterProfileImage.gameObject.SetActive(true);
                supporterProfileImage.sprite = supporterSprite;
            }
            else
            {
                supporterProfileImage.gameObject.SetActive(false);
            }
        }
    }

    // ==========================================
    // 2. 턴 대기열 및 패널 조작
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
            else
            {
                turnOrderIcons[i].gameObject.SetActive(false);
            }
        }
    }

    public void SetActionPanelActive(bool isActive)
    {
        actionUIPanel.SetActive(isActive);
    }

    public void SetWaitingPanelActive(bool isActive)
    {
        waitingPanel.SetActive(isActive);
    }

    // ==========================================
    // 3. 하단 메뉴 버튼 조작
    // ==========================================
    public void UpdateActionButtonsForCategory(string[] categoryKeys)
    {
        for (int i = 0; i < 5; i++)
        {
            actionButtons[i].gameObject.SetActive(true);
            actionButtons[i].interactable = true;
            actionButtonTexts[i].SetKey(categoryKeys[i]);
        }
    }

    public void UpdateActionButtonsForSkills(List<SkillData> skills, StyleRank currentRank)
    {
        for (int i = 0; i < 4; i++)
        {
            if (i < skills.Count)
            {
                actionButtons[i].gameObject.SetActive(true);
                actionButtonTexts[i].SetKey(skills[i].skillNameKey);

                // 4번째 스킬(인덱스 3)은 궁극기이므로 SSS 랭크일 때만 클릭 가능!
                if (i == 3)
                {
                    actionButtons[i].interactable = (currentRank == StyleRank.SSS);
                }
                else
                {
                    actionButtons[i].interactable = true; // 일반 스킬은 항상 클릭 가능
                }
            }
            else
            {
                actionButtons[i].gameObject.SetActive(false);
            }
        }
        actionButtons[4].gameObject.SetActive(true);
        actionButtons[4].interactable = true; // 취소 버튼은 항상 클릭 가능
        actionButtonTexts[4].SetKey("btn_cancel");
    }

    // ==========================================
    // 4. 전투 실시간 데이터 갱신
    // ==========================================
    public void SetCasterImage(bool isPlayer, Sprite skillSprite)
    {
        if (skillSprite == null) return;

        if (isPlayer) playerImage.sprite = skillSprite;
        else enemyImage.sprite = skillSprite;
    }

    public void SetDefenderImage(bool isPlayer, Sprite reactionSprite)
    {
        if (reactionSprite == null) return;

        if (isPlayer) playerImage.sprite = reactionSprite;
        else enemyImage.sprite = reactionSprite;
    }

    public void ResetCasterImage(bool isPlayer)
    {
        if (isPlayer) playerImage.sprite = defaultPlayerSprite;
        else enemyImage.sprite = defaultEnemySprite;
    }

    public void ResetDefenderImage(bool isPlayer)
    {
        if (isPlayer) playerImage.sprite = defaultPlayerSprite;
        else enemyImage.sprite = defaultEnemySprite;
    }

    public void UpdatePlayerHP(int currentHp, int maxHp)
    {
        playerHpSlider.value = currentHp;
        if (playerHpText != null) playerHpText.text = $"{currentHp}/{maxHp}";
    }

    public void UpdateEnemyHP(int currentHp, int maxHp)
    {
        enemyHpSlider.value = currentHp;
        if (enemyHpText != null) enemyHpText.text = $"{currentHp}/{maxHp}";
    }

    public void UpdatePlayerBreak(float breakValue)
    {
        playerBreakSlider.value = breakValue;
    }

    public void UpdateEnemyBreak(float breakValue)
    {
        enemyBreakSlider.value = breakValue;
    }

    public void UpdateStyleRankUI(StyleRank rank)
    {
        int rankIndex = (int)rank;

        // None 상태이거나 이미지가 안 채워져 있으면 이미지를 끕니다.
        if (rank == StyleRank.None || styleRankSprites.Length <= rankIndex || styleRankSprites[rankIndex] == null)
        {
            styleRankImage.gameObject.SetActive(false);
        }
        else
        {
            styleRankImage.gameObject.SetActive(true);
            styleRankImage.sprite = styleRankSprites[rankIndex];
        }
    }

    public IEnumerator TypeCommentary(string message, bool autoProceed = true, float delayAfter = 1.5f)
    {
        // TypewriterUtility가 텍스트를 다 칠 때까지 여기서 기다립니다(yield return).
        yield return StartCoroutine(TypewriterUtility.Instance.TypeText(commentaryText, message, autoProceed, delayAfter));
    }

    public IEnumerator ShowCutIn(Sprite cutInSprite)
    {
        if (cutInImage != null && cutInSprite != null)
        {
            cutInImage.gameObject.SetActive(true);
            cutInImage.sprite = cutInSprite;

            // 1초 동안 컷인을 보여줍니다.
            yield return new WaitForSeconds(1.5f);

            cutInImage.gameObject.SetActive(false);
        }
    }

    public void SpawnDamageText(string text, bool isCrit, bool isPlayerTarget)
    {
        if (damageTextPrefab == null) return;

        Transform targetPos = isPlayerTarget ? playerDamagePos : enemyDamagePos;

        // X, Y축으로 랜덤하게 흩뿌릴 오프셋(Offset) 생성
        float randomX = Random.Range(-50f, 50f);
        float randomY = Random.Range(-50f, 50f);
        Vector3 spawnPos = targetPos.position + new Vector3(randomX, randomY, 0);

        // 텍스트 생성
        GameObject dmgObj = Instantiate(damageTextPrefab, spawnPos, Quaternion.identity, targetPos.parent);
        DamageText dmgScript = dmgObj.GetComponent<DamageText>();
        if (dmgScript != null)
        {
            dmgScript.Setup(text, isCrit);
        }
    }

    public IEnumerator ShowCritAlert()
    {
        if (critAlertImage != null)
        {
            critAlertImage.SetActive(true);
            yield return new WaitForSeconds(1.0f); // 1초간 띄움
            critAlertImage.SetActive(false);
        }
    }

    // 턴 종료 시 화면에 남은 전투 연출 UI를 강제로 끄고 지웁니다.
    public void ClearCombatEffects()
    {
        // 1. 크리티컬 알림 이미지 강제 끄기
        if (critAlertImage != null)
        {
            critAlertImage.SetActive(false);
        }

        // 2. 씬에 남아있는 모든 데미지 텍스트 강제 파괴 (최신 유니티 문법 사용)
        DamageText[] activeTexts = FindObjectsByType<DamageText>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (DamageText txt in activeTexts)
        {
            if (txt != null && txt.gameObject != null)
            {
                Destroy(txt.gameObject);
            }
        }
    }
}