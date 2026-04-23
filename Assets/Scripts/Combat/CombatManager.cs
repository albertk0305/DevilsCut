using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance;

    [Header("아군(Player) UI 연결")]
    public Image playerImage;
    public Slider playerHpSlider;
    public Slider playerBreakSlider;
    public TextMeshProUGUI playerHpText;

    [Header("적(Enemy) UI 연결")]
    public Image enemyImage;
    public Slider enemyHpSlider;
    public Slider enemyBreakSlider;
    public TextMeshProUGUI enemyHpText; // [수정] 중복 선언 해결

    [Header("아군 스프라이트 설정")]
    public Sprite playerNormal; // 기본
    public Sprite playerHit;    // 피격
    public Sprite playerEvade;  // 회피

    // 적의 실시간 체력 및 브레이크 값
    private int currentEnemyHp;
    private float currentPlayerBreak = 0;
    private float currentEnemyBreak = 0;

    [Header("UI 프로필 이미지 (왼쪽 구역)")]
    public Image karinProfileImage;
    public Image supporterProfileImage;

    [Header("데이터 연결")]
    public KarinData karinData; // 방금 만든 카린 데이터 연결

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        SetupCombatScene();
    }

    private void SetupCombatScene()
    {
        // 1. 아군 데이터 초기화
        if (PlayerManager.Instance != null)
        {
            PlayerStats pStats = PlayerManager.Instance.stats;

            // 기본 이미지 세팅
            playerImage.sprite = playerNormal;

            // HP 바 세팅
            playerHpSlider.maxValue = pStats.maxHp;
            playerHpSlider.value = pStats.currentHp;
            if (playerHpText != null)
                playerHpText.text = $"{pStats.currentHp}/{pStats.maxHp}";

            // 브레이크 바: 100 고정
            playerBreakSlider.maxValue = 100;
            playerBreakSlider.value = 0;
            currentPlayerBreak = 0;
        }

        if (karinData != null && karinProfileImage != null)
        {
            karinProfileImage.sprite = karinData.normal;
        }

        SupporterData activeSup = PlayerManager.Instance.activeSupporter;
        if (activeSup != null && supporterProfileImage != null)
        {
            supporterProfileImage.gameObject.SetActive(true);
            supporterProfileImage.sprite = activeSup.mainImage; 
        }
        else
        {
            // 합류한 조력자가 없으면 이미지를 꺼둡니다.
            if (supporterProfileImage != null)
                supporterProfileImage.gameObject.SetActive(false);
        }

        // 2. 적군 데이터 초기화
        EnemyData eData = PlayerManager.Instance.currentEnemyToFight;

        if (eData != null)
        {
            enemyImage.sprite = eData.enemyImage;

            // 적 HP 바 세팅
            enemyHpSlider.maxValue = eData.maxHp;
            enemyHpSlider.value = eData.maxHp;
            currentEnemyHp = eData.maxHp;
            if (enemyHpText != null)
                enemyHpText.text = $"{currentEnemyHp}/{eData.maxHp}";

            // 적 브레이크 바: 100 고정
            enemyBreakSlider.maxValue = 100;
            enemyBreakSlider.value = 0;
            currentEnemyBreak = 0;
        }
        else
        {
            DevLog.LogError("전투 진입 실패: 적 데이터가 없습니다.");
        }
    }

    // [참고용] 나중에 이미지를 바꿀 때 쓸 함수 예시
    public void ChangePlayerImage(Sprite newSprite)
    {
        if (playerImage != null && newSprite != null)
            playerImage.sprite = newSprite;
    }
}