using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
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
    public Sprite PlayerCutIn;

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

    // 전투 내내 돌려쓸 플레이어와 적 데이터를 담아둘 전용 상자!
    private PlayerStats currentPlayerStats;
    private EnemyData currentEnemyData;

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
        if (PlayerManager.Instance != null)
        {
            currentPlayerStats = PlayerManager.Instance.stats;
            currentEnemyData = PlayerManager.Instance.currentEnemyToFight;
        }

        // 1. 아군 데이터 초기화
        if (currentPlayerStats != null)
        {
            // 기본 이미지 세팅
            playerImage.sprite = playerNormal;

            // HP 바 세팅
            playerHpSlider.maxValue = currentPlayerStats.maxHp;
            playerHpSlider.value = currentPlayerStats.currentHp;
            if (playerHpText != null)
                playerHpText.text = $"{currentPlayerStats.currentHp}/{currentPlayerStats.maxHp}";

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
        if (currentEnemyData != null)
        {
            enemyImage.sprite = currentEnemyData.enemyImage;

            // 적 HP 바 세팅
            enemyHpSlider.maxValue = currentEnemyData.maxHp;
            enemyHpSlider.value = currentEnemyData.maxHp;
            currentEnemyHp = currentEnemyData.maxHp;
            if (enemyHpText != null)
                enemyHpText.text = $"{currentEnemyHp}/{currentEnemyData.maxHp}";

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

        // 1. 주인공 (배수 1.0 = 100% 속도)
        turnQueue.Add(new TurnEntity { entityName = "Player", ap = currentPlayerStats.ActionPoints, actionGauge = 0, isPlayer = true, speedMultiplier = 1.0f, portraitIcon = PlayerCutIn });

        // 2. 카린 (주인공과 똑같은 AP를 넣되, 게이지 차는 속도를 1/3로 깎음 -> 정확히 3번에 1번 행동!)
        Sprite kIcon = karinData != null ? karinData.CutIn : null;
        turnQueue.Add(new TurnEntity { entityName = "Karin", ap = currentPlayerStats.ActionPoints, actionGauge = 0, isPlayer = false, speedMultiplier = 0.333f, portraitIcon = kIcon });

        // 3. 조력자 (주인공의 1/5 속도 = 0.2f 배수)
        if (PlayerManager.Instance.activeSupporter != null)
        {
            Sprite sIcon = PlayerManager.Instance.activeSupporter.CutIn; 
            turnQueue.Add(new TurnEntity { entityName = "Supporter", ap = currentPlayerStats.ActionPoints, actionGauge = 0, isPlayer = false, speedMultiplier = 0.2f, portraitIcon = sIcon });
        }

        // 4. 적 (배수 1.0) - 적은 자신의 고유 AP 스탯을 그대로 사용합니다.
        if (currentEnemyData != null)
        {
            int enemyAP = currentEnemyData.ActionPoints;
            Sprite eIcon = currentEnemyData.CutIn;
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

    private void ExecuteSkill(SkillData skill)
    {
        actionUIPanel.SetActive(false);
        waitingPanel.SetActive(true); // 버튼은 즉시 끄고 대기 패널을 켭니다.
        currentMenuState = MenuState.Hidden;

        // 코루틴을 실행하여 '시간의 흐름'이 있는 연출을 만듭니다.
        StartCoroutine(PerformSkillRoutine(skill));
    }
    private IEnumerator PerformSkillRoutine(SkillData skill)
    {
        // 1. 주인공 이미지 교체 (스킬에 할당된 이미지가 있다면)
        if (skill.skillActionImage != null)
        {
            playerImage.sprite = skill.skillActionImage;
        }

        DevLog.Log($"[스킬 시전] {skill.skillNameKey} 발동 준비!");

        // 2. 스킬 연출 대기 시간 (임시로 1초 대기)
        // 나중에 이펙트 프리팹을 생성하고 타격음 사운드를 재생하는 코드가 들어갈 자리입니다.
        yield return new WaitForSeconds(1.0f);

        // ==========================================
        // 3. 명중 / 회피 판정 (Hit or Miss 주사위 굴리기)
        // ==========================================
        float skillBaseAccuracy = skill.baseAccuracy;
        int enemySpeed = currentEnemyData.speed;

        bool isHit = CheckHitSuccess(skillBaseAccuracy, currentPlayerStats.speed, enemySpeed);

        if (!isHit)
        {
            DevLog.Log("빗나감 (Miss)! 데미지와 브레이크 수치가 적용되지 않습니다.");
            // TODO: 화면에 "빗나감" 텍스트 연출 애니메이션 추가
        }
        else
        {
            // ==========================================
            // 4. 실제 데미지 및 브레이크 계산 (명중했을 때만 실행!)
            // ==========================================
            int rawDamage = Mathf.RoundToInt(currentPlayerStats.strength * skill.damageMultiplier);

            float specialMultiplier = GetSpecialBreakMultiplier(skill, currentPlayerStats);
            float finalBreakDamage = skill.breakPower * specialMultiplier;

            int enemyBR = currentEnemyData.breakResistance;
            float dr = GetBreakDamageReduction(enemyBR);
            finalBreakDamage *= (1f - dr);

            float snowball = GetBreakSnowballMultiplier(currentEnemyBreak);
            finalBreakDamage *= snowball;

            currentEnemyBreak += finalBreakDamage;
            if (currentEnemyBreak >= 100f)
            {
                currentEnemyBreak = 100f;
                DevLog.Log("적 그로기(Break) 발생!");
            }

            enemyBreakSlider.value = currentEnemyBreak;

            DevLog.Log($"[공격 적중] 데미지: {rawDamage} / 브레이크 누적: {finalBreakDamage:F1} (현재 게이지: {currentEnemyBreak:F1}%)");
        }

        // 5. 고유 특수 효과 발동 (버프 등은 빗나가도 본인에게 걸려야 하는 경우 함수 안에서 분기 처리)
        ApplySpecialSkillEffects(skill);

        // 6. 행동 종료 및 원상 복구
        playerImage.sprite = playerNormal;
        CalculateNextTurn();
    }

    // 스킬의 고유한 효과를 분기 처리하는 함수
    private void ApplySpecialSkillEffects(SkillData skill)
    {
        switch (skill.specificId)
        {
            case SkillID.Sword_Deflect:
                // TODO: 가드 버프 획득 로직
                break;
            case SkillID.Sword_SpaceSlash:
                // TODO: 적 방어력 감소 디버프 부여 로직
                break;
                // ... 기획하신 다른 스킬들의 특수 효과도 이곳에 하나씩 추가해 나갑니다.
        }
    }

    // ==========================================
    // [전투 수학 공식 로직]
    // ==========================================

    // 명중, 회피 함수(속도)
    private float GetEffectiveSpeed(int speed)
    {
        if (speed <= 100) return speed;
        if (speed <= 200) return 100f + (speed - 100f) / 2f;
        return 150f + (speed - 200f) / 10f;
    }

    private bool CheckHitSuccess(float baseAccuracy, int attackerSpeed, int defenderSpeed)
    {
        float attackerES = GetEffectiveSpeed(attackerSpeed);
        float defenderES = GetEffectiveSpeed(defenderSpeed);

        float deltaES = attackerES - defenderES;
        float M = 120f;
        float C = 30f;

        // 명중률 보정 공식 (점감형 곡선)
        float hitModifier = M * (deltaES / (Mathf.Abs(deltaES) + C));
        float finalHitRate = baseAccuracy + hitModifier;

        // 최소 5%, 최대 95% 클램프 처리
        finalHitRate = Mathf.Clamp(finalHitRate, 5f, 95f);

        // 0.0 ~ 100.0 사이의 랜덤 주사위 굴리기!
        float randomRoll = Random.Range(0f, 100f);

        DevLog.Log($"[명중 연산] 유효속도 차이: {deltaES:F1} / 보정치: {hitModifier:F1}% / 최종 명중률: {finalHitRate:F1}% / 주사위 결과: {randomRoll:F1}");

        return randomRoll <= finalHitRate; // 주사위가 명중률보다 낮거나 같으면 타격 성공!
    }

    // 브레이크 함수(그로기)
    private float GetBreakDamageReduction(int br)
    {
        if (br <= 100) return br / 200f;
        else if (br <= 200) return 0.5f + ((br - 100f) / 400f);
        else return 0.75f + ((br - 200f) / 2000f);
    }

    private float GetBreakSnowballMultiplier(float currentGauge)
    {
        // 최대치 100을 기준으로 현재 비율의 제곱을 더함
        float ratio = currentGauge / 100f;
        return 1.0f + (ratio * ratio);
    }

    // ==========================================
    // [스킬 특수 처리 로직 분리]
    // ==========================================
    private float GetSpecialBreakMultiplier(SkillData skill, PlayerStats pStats)
    {
        float multiplier = 1.0f; // 기본 배율은 1.0 (변화 없음)

        switch (skill.specificId)
        {
            case SkillID.Oni_WitchHunt:
                // 마녀사냥: 잃은 체력에 비례해 브레이크 수치 최대 2배(1.0 + 1.0) 증가
                float missingHpRatio = (float)(pStats.maxHp - pStats.currentHp) / pStats.maxHp;
                multiplier += (missingHpRatio * 1.0f);
                break;

                // 나중에 브레이크 수치를 뻥튀기하는 다른 스킬이 생기면 여기에 case만 추가하면 됩니다!
        }

        return multiplier;
    }
}