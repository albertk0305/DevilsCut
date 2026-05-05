using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance;

    [Header("데이터 연결")]
    public PlayerData playerData;
    public KarinData karinData;

    // 전투 실시간 데이터
    private PlayerStats currentPlayerStats;
    private EnemyData currentEnemyData;
    private int currentEnemyHp;
    private float currentPlayerBreak = 0;
    private float currentEnemyBreak = 0;
    private int enemyTurnCount = 0;

    // 메뉴 상태
    private enum MenuState { Hidden, CategorySelect, SkillSelect }
    private MenuState currentMenuState = MenuState.Hidden;
    private string[] categoryKeys = { "cat_sword", "cat_gun", "cat_martial", "cat_magic", "cat_oni" };
    private SkillCategory selectedCategory;
    private List<SkillData> currentDisplaySkills;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        SetupCombatScene();
        InitializeTurnQueue();
        CalculateNextTurn();
    }

    private void SetupCombatScene()
    {
        if (PlayerManager.Instance != null)
        {
            currentPlayerStats = PlayerManager.Instance.stats;
            currentEnemyData = PlayerManager.Instance.currentEnemyToFight;
        }

        // [UI 위임] CombatUIManager에게 화면을 초기화해달라고 지시합니다.
        if (currentPlayerStats != null && playerData != null)
        {
            CombatUIManager.Instance.InitPlayerUI(currentPlayerStats.maxHp, currentPlayerStats.currentHp, playerData.normal);
        }

        if (currentEnemyData != null)
        {
            currentEnemyHp = currentEnemyData.maxHp;
            CombatUIManager.Instance.InitEnemyUI(currentEnemyData.maxHp, currentEnemyHp, currentEnemyData.enemyImage);
        }

        Sprite karinSpr = karinData != null ? karinData.normal : null;
        Sprite supSpr = PlayerManager.Instance.activeSupporter != null ? PlayerManager.Instance.activeSupporter.mainImage : null;
        CombatUIManager.Instance.InitProfiles(karinSpr, supSpr);

        currentPlayerBreak = 0;
        currentEnemyBreak = 0;
        enemyTurnCount = 0;

        if (StyleRankManager.Instance != null)
        {
            StyleRankManager.Instance.InitCombat();
        }
    }

    private void InitializeTurnQueue()
    {
        TurnManager.Instance.ClearQueue();

        if (playerData != null)
        {
            TurnManager.Instance.AddEntity("Player", currentPlayerStats.ActionPoints, true, 1.0f, playerData.cutIn);
        }

        Sprite kIcon = karinData != null ? karinData.CutIn : null;
        TurnManager.Instance.AddEntity("Karin", currentPlayerStats.ActionPoints, false, 0.333f, kIcon);

        if (PlayerManager.Instance.activeSupporter != null)
        {
            Sprite sIcon = PlayerManager.Instance.activeSupporter.CutIn;
            TurnManager.Instance.AddEntity("Supporter", currentPlayerStats.ActionPoints, false, 0.2f, sIcon);
        }

        if (currentEnemyData != null)
        {
            TurnManager.Instance.AddEntity("Enemy", currentEnemyData.ActionPoints, false, 1.0f, currentEnemyData.CutIn);
        }

        UpdateTurnOrderUI();
    }

    public void CalculateNextTurn()
    {
        TurnEntity nextTurnEntity = TurnManager.Instance.CalculateAndGetNextTurn();
        UpdateTurnOrderUI();

        DevLog.Log($"[턴 알림] {nextTurnEntity.entityName}의 턴!");
        ProcessTurn(nextTurnEntity);
    }

    private void UpdateTurnOrderUI()
    {
        // 턴 대기열 아이콘이 5개라고 가정하고 TurnManager에서 가져옵니다.
        List<Sprite> icons = TurnManager.Instance.GetFutureTurnIcons(5);
        // [UI 위임] 
        CombatUIManager.Instance.UpdateTurnOrderUI(icons);
    }

    private void ProcessTurn(TurnEntity currentTurnOwner)
    {
        string pName = playerData != null ? GetTranslatedText(playerData.playerNamekey) : "주인공";
        string eName = currentEnemyData != null ? GetTranslatedText(currentEnemyData.enemyNameKey) : "적";

        if (currentTurnOwner.entityName == "Enemy")
        {
            // [UI 위임] 
            CombatUIManager.Instance.SetActionPanelActive(false);
            CombatUIManager.Instance.SetWaitingPanelActive(true);
            currentMenuState = MenuState.Hidden;
            CombatUIManager.Instance.UpdateCommentary($"{eName}의 턴!");
            StartCoroutine(EnemyTurnRoutine());
        }
        else if (currentTurnOwner.isPlayer)
        {
            CombatUIManager.Instance.SetWaitingPanelActive(false);
            CombatUIManager.Instance.UpdateCommentary($"{pName}, 무슨 공격을 할까요?");
            ShowCategoryMenu();
        }
        else
        {
            CombatUIManager.Instance.SetActionPanelActive(false);
            CombatUIManager.Instance.SetWaitingPanelActive(true);
            // TODO: 조력자 턴 처리
        }
    }

    private IEnumerator EnemyTurnRoutine()
    {
        DevLog.Log("적이 어떻게 공격할지 생각 중입니다...");
        yield return new WaitForSeconds(1.5f);

        SkillData skillToUse = null;

        // 적 데이터에 '뇌(aiBrain)'가 장착되어 있다면, 스킬을 물어봅니다!
        if (currentEnemyData != null && currentEnemyData.aiBrain != null)
        {
            skillToUse = currentEnemyData.aiBrain.DecideNextSkill(enemyTurnCount, currentPlayerStats, currentEnemyData);
            enemyTurnCount++; // 다음번엔 다음 패턴을 쓰도록 턴 카운트를 1 올립니다.
        }

        // 받아온 스킬이 정상적으로 존재한다면 실행합니다.
        if (skillToUse != null)
        {
            StartCoroutine(PerformSkillRoutine(skillToUse, false, false));
        }
        else
        {
            DevLog.LogError("적의 AI 두뇌가 없거나 스킬 패턴이 비어있습니다! 인스펙터를 확인하세요.");
            CalculateNextTurn(); // 에러 방지용으로 턴 강제 넘기기
        }
    }

    public void ShowCategoryMenu()
    {
        // [UI 위임] 
        CombatUIManager.Instance.SetActionPanelActive(true);
        currentMenuState = MenuState.CategorySelect;
        CombatUIManager.Instance.UpdateActionButtonsForCategory(categoryKeys);
    }

    public void ShowSkillMenu(int categoryIndex)
    {
        currentMenuState = MenuState.SkillSelect;
        selectedCategory = (SkillCategory)categoryIndex;
        currentDisplaySkills = PlayerManager.Instance.GetSkillsByCategory(selectedCategory);

        StyleRank currentRank = StyleRankManager.Instance.currentRank;
        CombatUIManager.Instance.UpdateActionButtonsForSkills(currentDisplaySkills, currentRank);
    }

    public void OnActionSlotClicked(int slotIndex)
    {
        if (currentMenuState == MenuState.CategorySelect)
        {
            ShowSkillMenu(slotIndex);
        }
        else if (currentMenuState == MenuState.SkillSelect)
        {
            if (slotIndex == 4) ShowCategoryMenu();
            else
            {
                if (slotIndex < currentDisplaySkills.Count)
                {
                    bool isUltimate = (slotIndex == 3);
                    ExecuteSkill(currentDisplaySkills[slotIndex], true, isUltimate);
                }
            }
        }
    }

    private void ExecuteSkill(SkillData skill, bool isPlayerAttacking, bool isUltimate = false)
    {
        CombatUIManager.Instance.SetActionPanelActive(false);
        CombatUIManager.Instance.SetWaitingPanelActive(true);
        currentMenuState = MenuState.Hidden;

        // 코루틴에도 isUltimate 정보를 넘겨줍니다.
        StartCoroutine(PerformSkillRoutine(skill, isPlayerAttacking, isUltimate));
    }

    private IEnumerator PerformSkillRoutine(SkillData skill, bool isPlayerAttacking, bool isUltimate = false)
    {
        int attackerSpeed = isPlayerAttacking ? currentPlayerStats.speed : currentEnemyData.speed;
        int defenderSpeed = isPlayerAttacking ? currentEnemyData.speed : currentPlayerStats.speed;
        int attackerStrength = isPlayerAttacking ? currentPlayerStats.strength : currentEnemyData.strength;
        int attackerLuck = isPlayerAttacking ? currentPlayerStats.luck : currentEnemyData.luck;
        int defenderDefense = isPlayerAttacking ? currentEnemyData.defense : currentPlayerStats.defense;
        int defenderBR = isPlayerAttacking ? currentEnemyData.breakResistance : currentPlayerStats.breakResistance;

        CombatUIManager.Instance.SetCasterImage(isPlayerAttacking, skill.skillActionImage);

        string attackerName = isPlayerAttacking
            ? (playerData != null ? GetTranslatedText(playerData.playerNamekey) : "주인공")
            : (currentEnemyData != null ? GetTranslatedText(currentEnemyData.enemyNameKey) : "적");
        string skillName = GetTranslatedText(skill.skillNameKey);
        CombatUIManager.Instance.UpdateCommentary($"{attackerName}의 {skillName} 발동!");
        DevLog.Log($"[스킬 시전] {attackerName}이(가) {skillName} 발동!");

        if (isPlayerAttacking)
        {
            StyleRankManager.Instance.OnSkillUsed(selectedCategory);
        }

        yield return new WaitForSeconds(1.0f);

        bool isHit = CombatMath.CheckHitSuccess(skill.baseAccuracy, attackerSpeed, defenderSpeed);

        if (!isHit)
        {
            DevLog.Log("빗나감 (Miss)!");
            CombatUIManager.Instance.UpdateCommentary($"{attackerName}의 {skillName}이(가) 빗나갔습니다!");

            if (!isPlayerAttacking)
            {
                StyleRankManager.Instance.OnEvade();
            }
        }
        else
        {
            // ==========================================
            // 데미지 연산 최적화 로직
            // ==========================================

            // 1. 기본 데미지 (힘 * 스킬 계수)
            // 소수점 유실을 막기 위해 중간 계산은 모두 float으로 진행합니다.
            float calculatedDamage = attackerStrength * skill.damageMultiplier;

            // 2. 스킬 고유 피해 증폭 (SkillLogicBase 활용)
            float specialDamageMultiplier = 1.0f;
            if (skill.skillLogic != null)
            {
                specialDamageMultiplier = skill.skillLogic.GetDamageMultiplier(currentPlayerStats, currentEnemyData, isPlayerAttacking);
            }
            calculatedDamage *= specialDamageMultiplier;

            // 3. 크리티컬 판정 및 증폭
            bool isCrit = CombatMath.CheckCriticalSuccess(skill.bonusCritRate, attackerLuck);
            if (isCrit)
            {
                calculatedDamage *= 1.5f;
                DevLog.Log("크리티컬 적중!");
                CombatUIManager.Instance.UpdateCommentary($"{attackerName}의 {skillName} 치명적으로 적중!");

                if (isPlayerAttacking)
                {
                    StyleRankManager.Instance.OnCriticalHit();
                }
            }
            else
            {
                CombatUIManager.Instance.UpdateCommentary($"{attackerName}의 {skillName} 적중!");
            }

            // 4. 방어력 감소율 적용
            float damageReduction = CombatMath.GetDamageReduction(defenderDefense);
            calculatedDamage *= (1f - damageReduction);

            // 5. 최종 데미지 정수화 (최소 1 보장)
            int finalDamage = Mathf.RoundToInt(calculatedDamage);
            if (finalDamage <= 0) finalDamage = 1;

            // ==========================================
            // 체력 갱신 및 UI 위임
            // ==========================================
            if (isPlayerAttacking)
            {
                currentEnemyHp -= finalDamage;
                if (currentEnemyHp < 0) currentEnemyHp = 0;
                CombatUIManager.Instance.UpdateEnemyHP(currentEnemyHp, currentEnemyData.maxHp);
            }
            else
            {
                currentPlayerStats.currentHp -= finalDamage;
                if (currentPlayerStats.currentHp < 0) currentPlayerStats.currentHp = 0;
                CombatUIManager.Instance.UpdatePlayerHP(currentPlayerStats.currentHp, currentPlayerStats.maxHp);

                if (finalDamage > 0)
                {
                    StyleRankManager.Instance.OnPlayerHit();
                }
            }

            // 브레이크 연산 및 UI 위임
            float specialBreakMultiplier = 1.0f;
            if (skill.skillLogic != null)
            {
                specialBreakMultiplier = skill.skillLogic.GetBreakMultiplier(currentPlayerStats, currentEnemyData, isPlayerAttacking);
            }
            float finalBreakDamage = skill.breakPower * specialBreakMultiplier;
            finalBreakDamage *= (1f - CombatMath.GetBreakDamageReduction(defenderBR));

            if (isPlayerAttacking)
            {
                finalBreakDamage *= CombatMath.GetBreakSnowballMultiplier(currentEnemyBreak);
                currentEnemyBreak += finalBreakDamage;
                if (currentEnemyBreak >= 100f)
                {
                    currentEnemyBreak = 100f;
                    DevLog.Log("적 그로기(Break) 발생!");
                    StyleRankManager.Instance.OnEnemyBreak();
                }
                CombatUIManager.Instance.UpdateEnemyBreak(currentEnemyBreak);
            }
            else
            {
                finalBreakDamage *= CombatMath.GetBreakSnowballMultiplier(currentPlayerBreak);
                currentPlayerBreak += finalBreakDamage;
                if (currentPlayerBreak >= 100f) { currentPlayerBreak = 100f; DevLog.Log("아군 그로기(Break) 발생!"); }
                CombatUIManager.Instance.UpdatePlayerBreak(currentPlayerBreak);
            }

            DevLog.Log($"[공격 적중] 최종 딜: {finalDamage} / 브레이크 누적: {finalBreakDamage:F1}");
        }

        // 스킬 고유 특수 효과 발동
        if (skill.skillLogic != null)
        {
            skill.skillLogic.ApplyEffect(currentPlayerStats, currentEnemyData, isPlayerAttacking);
        }

        if (isPlayerAttacking)
        {
            StyleRankManager.Instance.ResetTurnState();
            if (isUltimate)
            {
                StyleRankManager.Instance.ResetRankForUltimate();
            }
        }

        yield return new WaitForSeconds(1.5f);
        CombatUIManager.Instance.ResetCasterImage(isPlayerAttacking);

        if (isPlayerAttacking && currentEnemyHp == 0)
        {
            DevLog.Log("적을 물리쳤습니다! 전투 승리!");
        }
        else if (!isPlayerAttacking && currentPlayerStats.currentHp == 0)
        {
            DevLog.Log("주인공이 쓰러졌습니다... 게임 오버!");
        }
        else
        {
            CalculateNextTurn();
        }
    }

    private string GetTranslatedText(string key)
    {
        // 1. 빈 키값이 들어오면 빈 글자를 반환합니다.
        if (string.IsNullOrEmpty(key)) return "";

        // 2. LocalizationManager가 정상적으로 켜져 있다면 번역된 텍스트를 가져옵니다!
        if (LocalizationManager.Instance != null)
        {
            return LocalizationManager.Instance.GetText(key);
        }

        // 3. 만약 씬 테스트 중이라 매니저가 없다면 에러가 나지 않게 키값을 그대로 반환합니다.
        return key;
    }
}