using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class TurnEntity
{
    public string entityName;
    public int ap;
    public float actionGauge;
    public bool isPlayer;
    public float speedMultiplier = 1f; // [핵심 추가] 턴 비율을 강제로 맞추기 위한 속도 배수
    public Sprite portraitIcon; // [추가] 턴 대기열 UI에 띄울 얼굴 이미지!
}

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

    [Header("턴 대기열 UI (위에서부터 1~4등)")]
    public Image[] turnOrderIcons; // 크기 5짜리 Image 배열

    [Header("데이터 연결")]
    public KarinData karinData; // 방금 만든 카린 데이터 연결

    [Header("턴 관리 시스템")]
    public List<TurnEntity> turnQueue = new List<TurnEntity>(); // 전투에 참여하는 캐릭터들

    // [최적화 1] 매번 new List를 하지 않고, 미리 만들어둔 바구니를 재활용합니다!
    private List<Sprite> futureTurnIcons = new List<Sprite>();
    private List<TurnEntity> simQueue = new List<TurnEntity>();

    [Header("전투 액션 메뉴 UI")]
    public GameObject actionUIPanel; // 5개 버튼을 묶어둔 부모 패널 (플레이어 턴에만 켬)
    public GameObject waitingPanel;
    public Button[] actionButtons;   // 5개의 버튼 배열
    public LocalizedText[] actionButtonTexts; // 5개의 버튼에 붙어있는 다국어 스크립트

    // 메뉴의 현재 상태를 나타내는 열거형(Enum)
    private enum MenuState { Hidden, CategorySelect, SkillSelect }
    private MenuState currentMenuState = MenuState.Hidden;

    // 카테고리 이름 다국어 키 (Inspector에서 편하게 수정 가능)
    private string[] categoryKeys = { "cat_sword", "cat_gun", "cat_martial", "cat_magic", "cat_oni" };

    private SkillCategory selectedCategory;
    private List<SkillData> currentDisplaySkills; // 현재 화면에 띄워진 4개의 스킬 데이터

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        SetupCombatScene();
        InitializeTurnQueue();
        CalculateNextTurn(); // 씬 세팅 직후 바로 첫 턴 계산 시작!
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

    private void InitializeTurnQueue()
    {
        turnQueue.Clear();
        PlayerStats pStats = PlayerManager.Instance.stats;

        // 1. 주인공 (배수 1.0 = 100% 속도)
        turnQueue.Add(new TurnEntity { entityName = "Player", ap = pStats.ActionPoints, actionGauge = 0, isPlayer = true, speedMultiplier = 1.0f, portraitIcon = playerNormal });

        // 2. 카린 (주인공과 똑같은 AP를 넣되, 게이지 차는 속도를 1/3로 깎음 -> 정확히 3번에 1번 행동!)
        Sprite kIcon = karinData != null ? karinData.normal : null;
        turnQueue.Add(new TurnEntity { entityName = "Karin", ap = pStats.ActionPoints, actionGauge = 0, isPlayer = false, speedMultiplier = 0.333f, portraitIcon = kIcon });

        // 3. 조력자 (주인공의 1/5 속도 = 0.2f 배수)
        if (PlayerManager.Instance.activeSupporter != null)
        {
            Sprite sIcon = PlayerManager.Instance.activeSupporter.mainImage; // 혹은 sdImage 등 초상화용 변수
            turnQueue.Add(new TurnEntity { entityName = "Supporter", ap = pStats.ActionPoints, actionGauge = 0, isPlayer = false, speedMultiplier = 0.2f, portraitIcon = sIcon });
        }

        // 4. 적 (배수 1.0) - 적은 자신의 고유 AP 스탯을 그대로 사용합니다.
        if (PlayerManager.Instance.currentEnemyToFight != null)
        {
            int enemyAP = PlayerManager.Instance.currentEnemyToFight.ActionPoints;
            Sprite eIcon = PlayerManager.Instance.currentEnemyToFight.enemyImage;
            turnQueue.Add(new TurnEntity { entityName = "Enemy", ap = enemyAP, actionGauge = 0, isPlayer = false, speedMultiplier = 1.0f, portraitIcon = eIcon });
        }

        UpdateTurnOrderUI();
    }

    // 점감 공식이 적용된 게이지 상승량 계산 함수 (핵심 밸런스 공식)
    private float GetGaugeFillAmount(int ap)
    {
        float maxFill = 20f;
        // [설정 완료] 100을 넘어가면서부터 효율이 완만해지도록(소프트캡) 100f로 설정합니다.
        float softCapReference = 100f;

        return maxFill * (ap / (ap + softCapReference));
    }

    private float GetTicksToNextTurn(TurnEntity entity)
    {
        float fillPerTick = GetGaugeFillAmount(entity.ap) * entity.speedMultiplier;
        if (fillPerTick <= 0) return 9999f; // 에러 방지용 방어 코드

        // (목표치 - 현재치) / 속도 = 도달하기까지 남은 시간
        return (100f - entity.actionGauge) / fillPerTick;
    }

    // [최적화 2] 찔끔찔끔 더하던 while문을 버리고, 시뮬레이터의 '시간 워프'를 도입!
    public void CalculateNextTurn()
    {
        // 1. 남은 시간이 가장 적은(가장 먼저 100에 도달할) 캐릭터 찾기
        turnQueue.Sort((a, b) =>
        {
            float aTicks = GetTicksToNextTurn(a);
            float bTicks = GetTicksToNextTurn(b);

            int compare = aTicks.CompareTo(bTicks);
            if (compare == 0) return a.isPlayer ? -1 : 1;
            return compare;
        });

        TurnEntity nextTurnEntity = turnQueue[0];

        // 2. 1등이 100에 도달하기까지 필요한 시간(틱)
        float ticksToAdvance = GetTicksToNextTurn(nextTurnEntity);

        // 3. 실제 시간 워프 (필요한 시간만큼 모든 캐릭터의 게이지를 한 방에 증가!)
        // 이미 게이지가 100을 넘은 상태(초과분)라면 ticksToAdvance가 0 이하가 되어 자연스럽게 패스됩니다.
        if (ticksToAdvance > 0)
        {
            foreach (var entity in turnQueue)
            {
                float fillAmount = GetGaugeFillAmount(entity.ap) * entity.speedMultiplier;
                entity.actionGauge += fillAmount * ticksToAdvance;
            }
        }

        // 4. 턴 획득자 처리
        nextTurnEntity.actionGauge -= 100f;

        UpdateTurnOrderUI(); // 시각화 업데이트

        DevLog.Log($"[턴 알림] {nextTurnEntity.entityName}의 턴! (이월된 수치: {nextTurnEntity.actionGauge:F1})");
        ProcessTurn(nextTurnEntity);
    }

    // [최적화 3] 메모리 쓰레기(GC)를 만들지 않는 시뮬레이터 UI 로직
    private void UpdateTurnOrderUI()
    {
        futureTurnIcons.Clear(); // 기존 바구니 비우기

        // 1. simQueue 재활용 (값만 덮어씌우기)
        while (simQueue.Count < turnQueue.Count) simQueue.Add(new TurnEntity()); // 칸이 모자라면 늘려줌
        for (int i = 0; i < turnQueue.Count; i++)
        {
            simQueue[i].entityName = turnQueue[i].entityName;
            simQueue[i].ap = turnQueue[i].ap;
            simQueue[i].actionGauge = turnQueue[i].actionGauge;
            simQueue[i].isPlayer = turnQueue[i].isPlayer;
            simQueue[i].speedMultiplier = turnQueue[i].speedMultiplier;
            simQueue[i].portraitIcon = turnQueue[i].portraitIcon;
        }

        // 2. 미래 예측 (기존과 동일한 로직)
        while (futureTurnIcons.Count < turnOrderIcons.Length)
        {
            simQueue.Sort((a, b) =>
            {
                float aTicks = GetTicksToNextTurn(a);
                float bTicks = GetTicksToNextTurn(b);
                int compare = aTicks.CompareTo(bTicks);
                if (compare == 0) return a.isPlayer ? -1 : 1;
                return compare;
            });

            TurnEntity nextSimEntity = simQueue[0];
            float ticksToAdvance = GetTicksToNextTurn(nextSimEntity);

            foreach (var e in simQueue)
            {
                float fillAmount = GetGaugeFillAmount(e.ap) * e.speedMultiplier;
                e.actionGauge += fillAmount * ticksToAdvance;
            }

            futureTurnIcons.Add(nextSimEntity.portraitIcon);
            nextSimEntity.actionGauge -= 100f;
        }

        // 3. UI 적용 (기존과 동일)
        for (int i = 0; i < turnOrderIcons.Length; i++)
        {
            if (i < futureTurnIcons.Count && futureTurnIcons[i] != null)
            {
                turnOrderIcons[i].gameObject.SetActive(true);
                turnOrderIcons[i].sprite = futureTurnIcons[i];
            }
            else
            {
                turnOrderIcons[i].gameObject.SetActive(false);
            }
        }
    }

    private void ProcessTurn(TurnEntity currentTurnOwner)
    {
        if (currentTurnOwner.entityName == "Enemy")
        {
            // 적 턴 로직 (AI 공격)
            actionUIPanel.SetActive(false); // 적 턴이므로 플레이어 버튼 숨김
            waitingPanel.SetActive(true); // [추가] 적 턴일 때 대기 화면 켬
            currentMenuState = MenuState.Hidden;

            // TODO: 적 공격 후 다시 CalculateNextTurn() 호출
        }
        else if (currentTurnOwner.isPlayer)
        {
            // 아군 턴 로직 (주인공 턴)
            waitingPanel.SetActive(false);
            ShowCategoryMenu(); // 주인공 턴이 오면 최상위 카테고리 메뉴를 띄움
        }
        else
        {
            // 카린이나 조력자 턴
            actionUIPanel.SetActive(false);
            waitingPanel.SetActive(true);
            // TODO: 카린/조력자 자동 행동 로직 (혹은 별도 UI)
        }
    }

    // 1. 최상위 메뉴 (검술, 사격, 격투, 요술, 오니) 보여주기
    public void ShowCategoryMenu()
    {
        actionUIPanel.SetActive(true);
        currentMenuState = MenuState.CategorySelect;

        for (int i = 0; i < 5; i++)
        {
            actionButtons[i].gameObject.SetActive(true);
            actionButtonTexts[i].SetKey(categoryKeys[i]);
        }
    }

    // 2. 하위 메뉴 (선택한 계열의 스킬 4개 + 취소 버튼) 보여주기
    public void ShowSkillMenu(int categoryIndex)
    {
        currentMenuState = MenuState.SkillSelect;
        selectedCategory = (SkillCategory)categoryIndex;

        // PlayerManager에서 해당 카테고리의 스킬들만 가져옵니다!
        currentDisplaySkills = PlayerManager.Instance.GetSkillsByCategory(selectedCategory);

        // 1~4번 버튼 갱신
        for (int i = 0; i < 4; i++)
        {
            if (i < currentDisplaySkills.Count)
            {
                actionButtons[i].gameObject.SetActive(true);
                // SO 안에 있는 다국어 Key를 꺼내서 번역기(SetKey)에 넘겨줍니다!
                actionButtonTexts[i].SetKey(currentDisplaySkills[i].skillNameKey);
            }
            else
            {
                // 스킬을 4개 다 안 배웠다면 빈 버튼은 끕니다.
                actionButtons[i].gameObject.SetActive(false);
            }
        }

        // 5번째 버튼(인덱스 4)은 항상 취소 버튼
        actionButtons[4].gameObject.SetActive(true);
        actionButtonTexts[4].SetKey("btn_cancel");
    }

    // 3. 5개의 버튼 중 하나를 클릭했을 때 호출될 함수
    public void OnActionSlotClicked(int slotIndex)
    {
        if (currentMenuState == MenuState.CategorySelect)
        {
            ShowSkillMenu(slotIndex);
        }
        else if (currentMenuState == MenuState.SkillSelect)
        {
            if (slotIndex == 4)
            {
                // 5번째 버튼(취소)을 눌렀다면 다시 상위 카테고리로 돌아감
                ShowCategoryMenu();
            }
            else
            {
                // [최적화] 인덱스를 넘기는 게 아니라, 미리 찾아둔 스킬 데이터를 직접 꺼냅니다!
                if (slotIndex < currentDisplaySkills.Count)
                {
                    SkillData selectedSkill = currentDisplaySkills[slotIndex];
                    DevLog.Log($"[스킬 사용] 카테고리: {selectedCategory}, 사용한 스킬: {selectedSkill.skillNameKey}");

                    // 스킬 데이터 자체를 함수로 넘겨줍니다.
                    ExecuteSkill(selectedSkill);
                }
            }
        }
    }

    // [최적화] 실제 스킬 효과를 적용하는 함수 (매개변수가 SkillData로 변경됨!)
    private void ExecuteSkill(SkillData skill)
    {
        // 1. 데미지 계산 및 적 체력 감소
        // 예시: int damage = Mathf.RoundToInt(PlayerManager.Instance.stats.strength * skill.damageMultiplier);
        // PlayerManager.Instance.currentEnemyToFight.TakeDamage(damage); // (적 데미지 처리 함수가 있다면)

        // 2. 브레이크 데미지 적용
        // float breakDmg = skill.breakPower; ...

        // 3. 스킬 이펙트 연출 재생
        // ...

        actionUIPanel.SetActive(false); // 행동을 마쳤으니 버튼 패널 닫기
        waitingPanel.SetActive(true);
        currentMenuState = MenuState.Hidden;

        // 연출이 끝난 뒤에 다시 다음 턴 계산 시작 (임시로 바로 호출)
        CalculateNextTurn();
    }
}