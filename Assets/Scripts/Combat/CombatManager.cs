using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CombatState
{
    // 기(Ki) 관련
    public bool isPlayerCharging = false;
    public bool isUnleashingCharge = false;
    public SkillData chargingSkill = null;
    public bool hasUsedKiExtraTurn = false;

    // 크루세이더/폭탄 관련
    public bool isBombActive = false;
    public int savedBombDamage = 0;

    // 스탯/데미지 기록 관련
    public int accumulatedDamage = 0;
    public int lastSuccessfulHits = 0;
    public bool wasEnemyBrokenAtSkillStart = false; // 진화 B 페이백용 스냅샷

    public bool hasRewardedCritThisSkill = false;
    public bool isMorningStarApRecoveredThisSkill = false;
}

public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance;

    [Header("데이터 연결")]
    public PlayerData playerData;

    private PlayerStats currentPlayerStats;
    public PlayerStats GetCurrentPlayerStats() => currentPlayerStats;

    private EnemyData currentEnemyData;
    public EnemyData GetCurrentEnemyData() => currentEnemyData;

    private int currentEnemyHp;
    private int enemyTurnCount = 0;
    private int playerHpAtTurnStart;
    private int enemyHpAtTurnStart;
    private TurnEntity currentActiveEntity;

    private enum MenuState { Hidden, CategorySelect, SkillSelect }
    private MenuState currentMenuState = MenuState.Hidden;
    private string GetCategoryLocalizationKey(SkillCategory category)
    {
        return category switch
        {
            SkillCategory.Sword => "cat_sword",
            SkillCategory.Gun => "cat_gun",
            SkillCategory.Martial => "cat_martial",
            SkillCategory.Magic => "cat_magic",
            SkillCategory.Oni => "cat_oni",
            _ => "cat_unknown" // 예외 상황 대비
        };
    }
    private SkillCategory selectedCategory;
    private List<SkillData> currentDisplaySkills;
    private readonly List<SkillCategory> categoryMenuOrder = new List<SkillCategory>
    {
        SkillCategory.Sword,
        SkillCategory.Gun,
        SkillCategory.Martial,
        SkillCategory.Magic,
        SkillCategory.Oni
    };

    public CombatState currentState = new CombatState();

    // [최적화] 코루틴 대기 객체 캐싱
    private readonly WaitForSeconds oneSecondWait = new WaitForSeconds(1.0f);

    public bool IsPlayerSelectingPhase => currentMenuState == MenuState.CategorySelect || currentMenuState == MenuState.SkillSelect;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        SetupCombatScene();
        InitializeTurnQueue();
        StartCoroutine(CombatStartPhaseRoutine());
    }

    private IEnumerator CombatStartPhaseRoutine()
    {
        string eName = currentEnemyData != null ? GetTranslatedText(currentEnemyData.enemyNameKey) : "적";

        yield return CombatUIManager.Instance.TypeCommentary($"{eName} 조우!", true, 1.0f);
        yield return oneSecondWait;

        SupporterData activeSup = PlayerManager.Instance.activeSupporter;
        if (activeSup != null && activeSup.startSkillLogic != null)
        {
            yield return CompanionManager.Instance.ExecuteSupporterTurn(activeSup, true);
        }

        CompanionManager.Instance.UpdateEmotion(CompanionManager.Emotion.Normal);
        CalculateNextTurn();
    }

    private void SetupCombatScene()
    {
        if (PlayerManager.Instance != null)
        {
            currentPlayerStats = PlayerManager.Instance.stats.Clone();
            currentEnemyData = PlayerManager.Instance.currentEnemyToFight;

            // 1. [주의] StatManager가 원본 스탯을 먼저 세팅해야 합니다.
            if (StatManager.Instance != null)
                StatManager.Instance.InitStats(currentPlayerStats, currentEnemyData);
        }

        // 2. 그 이후에 UI가 세팅된 스탯을 기반으로 체력바를 그립니다.
        if (currentPlayerStats != null && playerData != null)
            CombatUIManager.Instance.InitPlayerUI(currentPlayerStats.maxHp, currentPlayerStats.currentHp, playerData.normal);

        if (currentEnemyData != null)
        {
            currentEnemyHp = currentEnemyData.maxHp;
            CombatUIManager.Instance.InitEnemyUI(currentEnemyData.maxHp, currentEnemyHp, currentEnemyData.enemyImage);
        }

        Sprite karinSpr = CompanionManager.Instance.karinData?.normal;
        Sprite supSpr = PlayerManager.Instance.activeSupporter?.mainImage;
        CombatUIManager.Instance.InitProfiles(karinSpr, supSpr);
        currentState = new CombatState();

        enemyTurnCount = 0;
        BreakManager.Instance?.InitBreakState();
        BuffManager.Instance?.ClearAllEffects();
        StyleRankManager.Instance?.InitCombat();

        CombatUIManager.Instance.SetActionPanelActive(false);
        CombatUIManager.Instance.SetWaitingPanelActive(true);
        currentMenuState = MenuState.Hidden;
    }

    private void InitializeTurnQueue()
    {
        TurnManager.Instance.ClearQueue();

        if (playerData != null)
            TurnManager.Instance.AddEntity(EntityType.Player, currentPlayerStats.ActionPoints, true, 1.0f, playerData.cutIn);

        if (CompanionManager.Instance.karinData != null && PlayerManager.Instance.equippedKarinItem != null)
            TurnManager.Instance.AddEntity(EntityType.Karin, currentPlayerStats.ActionPoints, false, 0.333f, CompanionManager.Instance.karinData.CutIn);

        if (PlayerManager.Instance.activeSupporter != null)
            TurnManager.Instance.AddEntity(EntityType.Supporter, currentPlayerStats.ActionPoints, false, 0.2f, PlayerManager.Instance.activeSupporter.CutIn);

        if (currentEnemyData != null)
            TurnManager.Instance.AddEntity(EntityType.Enemy, currentEnemyData.ActionPoints, false, 1.0f, currentEnemyData.CutIn);

        UpdateTurnOrderUI();
    }

    public void CalculateNextTurn()
    {
        TurnEntity nextTurnEntity = TurnManager.Instance.CalculateAndGetNextTurn();
        UpdateTurnOrderUI();
        StartCoroutine(ProcessTurnRoutine(nextTurnEntity));
    }

    private void UpdateTurnOrderUI()
    {
        List<Sprite> icons = TurnManager.Instance.GetFutureTurnIcons(5);
        CombatUIManager.Instance.UpdateTurnOrderUI(icons);
    }

    // ==========================================================
    // 1. 메인 턴 분배기 (Switch 문으로 가독성 극대화)
    // ==========================================================
    private IEnumerator ProcessTurnRoutine(TurnEntity currentTurnOwner)
    {
        currentActiveEntity = currentTurnOwner;
        playerHpAtTurnStart = currentPlayerStats.currentHp;
        enemyHpAtTurnStart = currentEnemyHp;

        CombatUIManager.Instance.SetActionPanelActive(false);
        CombatUIManager.Instance.SetWaitingPanelActive(true);
        currentMenuState = MenuState.Hidden;

        yield return HandlePreTurnEffects(currentTurnOwner);

        if (currentEnemyHp <= 0 || currentPlayerStats.currentHp <= 0) yield break;

        switch (currentTurnOwner.type)
        {
            case EntityType.Enemy:
                yield return HandleEnemyTurn(); 
                break;
            case EntityType.Player:
                yield return HandlePlayerTurn(); 
                break;
            case EntityType.Karin:
                yield return CompanionManager.Instance.ExecuteKarinTurn();
                break;
            case EntityType.Supporter:
                yield return HandleSupporterTurn(); 
                break;
        }
    }

    // ==========================================================
    // 2. 턴 시작 전 공통 효과 처리 (도트 딜, 시한폭탄 등)
    // ==========================================================
    private IEnumerator HandlePreTurnEffects(TurnEntity owner)
    {
        string eName = currentEnemyData != null ? GetTranslatedText(currentEnemyData.enemyNameKey) : "적";

        if (owner.type == EntityType.Enemy)
        {
            var enemyEffects = BuffManager.Instance.GetEffects(false);
            var bleedEffect = enemyEffects.Find(e => e.effectData.specialType == SpecialEffectType.Bleed);
            var burnEffect = enemyEffects.Find(e => e.effectData.specialType == SpecialEffectType.Burn);

            if (bleedEffect != null)
            {
                int bleedDmg = Mathf.Max(1, Mathf.RoundToInt(currentPlayerStats.strength * bleedEffect.value));
                ApplyDamageToEntity(false, bleedDmg);

                CombatUIManager.Instance.SetDefenderImage(false, currentEnemyData.hit);
                CombatUIManager.Instance.SpawnDamageText(bleedDmg.ToString(), true, false);

                yield return CombatUIManager.Instance.TypeCommentary($"심연의 출혈! {eName}이(가) {bleedDmg}의 지속 피해를 입습니다.", true, 0.5f);

                yield return new WaitForSeconds(1.0f);
                CombatUIManager.Instance.ResetDefenderImage(false);

                if (currentEnemyHp <= 0) { EndCombat(false); yield break; }
            }

            if (burnEffect != null)
            {
                // 최대 체력에 비례한 고정 피해
                int burnDmg = Mathf.Max(1, Mathf.RoundToInt(currentEnemyData.maxHp * burnEffect.value));
                ApplyDamageToEntity(false, burnDmg);

                CombatUIManager.Instance.SetDefenderImage(false, currentEnemyData.hit);
                CombatUIManager.Instance.SpawnDamageText(burnDmg.ToString(), true, false);

                yield return CombatUIManager.Instance.TypeCommentary($"지옥의 플람베! {eName}이(가) {burnDmg}의 화상 피해를 입습니다.", true, 0.5f);

                yield return new WaitForSeconds(1.0f);
                CombatUIManager.Instance.ResetDefenderImage(false);

                if (currentEnemyHp <= 0) { EndCombat(false); yield break; }
            }

            if (currentState.isBombActive)
            {
                currentState.isBombActive = false;
                enemyEffects.RemoveAll(e => e.effectData != null && e.effectData.specialType == SpecialEffectType.TimeBomb);
                CombatUIManager.Instance.RefreshBuffUI();

                CombatUIManager.Instance.SetDefenderImage(false, currentEnemyData.hit);

                yield return CombatUIManager.Instance.TypeCommentary("라스트 트레인 홈 발동!!", true, 0.5f);

                ApplyDamageToEntity(false, currentState.savedBombDamage);
                CombatUIManager.Instance.SpawnDamageText(currentState.savedBombDamage.ToString(), true, false);
                DevLog.Log($"[라스트 트레인 홈] 적에게 {currentState.savedBombDamage}의 확정 피해를 입힙니다!");

                yield return new WaitForSeconds(1.0f);
                CombatUIManager.Instance.ResetDefenderImage(false);

                if (currentEnemyHp <= 0) { EndCombat(false); yield break; }
            }
        }
    }

    // ==========================================================
    // 3. 적(Enemy) 턴 로직
    // ==========================================================
    private IEnumerator HandleEnemyTurn()
    {
        string eName = currentEnemyData != null ? GetTranslatedText(currentEnemyData.enemyNameKey) : "적";
        var enemyEffects = BuffManager.Instance.GetEffects(false);
        var stunEffect = enemyEffects.Find(e => e.effectData.specialType == SpecialEffectType.Stun);
        if (stunEffect != null)
        {
            enemyEffects.Remove(stunEffect);
            CombatUIManager.Instance.RefreshBuffUI();
            yield return CombatUIManager.Instance.TypeCommentary($"{eName}은(는) 무량공처의 효과로 행동할 수 없습니다!", true, 1.0f);
            ResolveTurnEnd();
            yield break;
        }

        if (BreakManager.Instance.IsBroken(false))
        {
            yield return CombatUIManager.Instance.TypeCommentary($"{eName}이(가) 그로기 상태에서 정신을 차렸습니다.");
            BreakManager.Instance.WakeUpFromBreak(false);
            CombatUIManager.Instance.ResetDefenderImage(false);
            ResolveTurnEnd();
            yield break;
        }

        yield return CombatUIManager.Instance.TypeCommentary($"{eName}의 차례입니다!");
        yield return EnemyTurnRoutine();
    }

    // ==========================================================
    // 4. 플레이어(Player) 턴 로직
    // ==========================================================
    private IEnumerator HandlePlayerTurn()
    {
        string pName = playerData != null ? GetTranslatedText(playerData.playerNamekey) : "주인공";

        if (BreakManager.Instance.IsBroken(true))
        {
            yield return CombatUIManager.Instance.TypeCommentary($"{pName}이(가) 그로기 상태에서 정신을 차렸습니다.");
            BreakManager.Instance.WakeUpFromBreak(true);
            CombatUIManager.Instance.ResetDefenderImage(true);
            CombatUIManager.Instance.ResetCasterImage(true);
            ResolveTurnEnd();
            yield break;
        }

        if (currentState.isPlayerCharging && currentState.chargingSkill != null)
        {
            currentState.isPlayerCharging = false;
            currentState.isUnleashingCharge = true;
            yield return CombatUIManager.Instance.TypeCommentary($"{pName}이(가) 모아둔 기를 방출합니다!", true, 1.0f);
            DevLog.Log("[원기옥] 모은 기를 발사합니다!");
            PerformSkillRoutine(currentState.chargingSkill, true);
        }
        else
        {
            CombatUIManager.Instance.SetWaitingPanelActive(false);
            ShowCategoryMenu();
            yield return CombatUIManager.Instance.TypeCommentary($"{pName}, 무슨 공격을 할까요?", false);
        }
    }

    // ==========================================================
    // 5. 조력자(Supporter) 턴 로직
    // ==========================================================
    private IEnumerator HandleSupporterTurn()
    {
        SupporterData activeSup = PlayerManager.Instance.activeSupporter;
        if (activeSup != null && activeSup.battleSkillLogic != null)
        {
            yield return CompanionManager.Instance.ExecuteSupporterTurn(activeSup, false);
        }
        else
        {
            ResolveTurnEnd();
        }
    }

    private IEnumerator EnemyTurnRoutine()
    {
        yield return new WaitForSeconds(0.3f);

        EnemyActionIntent intent = null;

        // 1. AI 뇌(Brain)에게 이번 턴의 '행동 계획서'를 결재받습니다.
        if (currentEnemyData?.aiBrain != null)
        {
            // 주의: 추후 EnemyAIBrain 스크립트를 수정하여 DecideNextAction 함수를 만들어야 합니다!
            intent = currentEnemyData.aiBrain.DecideNextAction(enemyTurnCount, currentPlayerStats, currentEnemyData);
            enemyTurnCount++;
        }

        if (intent != null)
        {
            // 2. 보스 전용 대사 출력 연출
            if (!string.IsNullOrEmpty(intent.dialogueKey))
            {
                string diagText = GetTranslatedText(intent.dialogueKey);
                float duration = intent.dialogueDuration > 0f ? intent.dialogueDuration : 1.5f;
                yield return CombatUIManager.Instance.TypeCommentary(diagText, true, duration);
            }

            // 3. 기 모으기 (Charge) 연출
            if (intent.isCharging)
            {
                string chargeMsg = !string.IsNullOrEmpty(intent.chargeComment)
                    ? GetTranslatedText(intent.chargeComment)
                    : $"{GetTranslatedText(currentEnemyData.enemyNameKey)}이(가) 강력한 공격을 준비합니다!";

                // (선택) currentEnemyData.chargeImage 같은 기 모으기 전용 이미지가 있다면 여기서 교체
                // CombatUIManager.Instance.SetDefenderImage(false, currentEnemyData.chargeImage);

                yield return CombatUIManager.Instance.TypeCommentary(chargeMsg, true, 1.0f);

                ResolveTurnEnd(); // 스킬을 쓰지 않고 턴을 넘깁니다.
                yield break;
            }

            // 4. 실제 스킬 사용
            if (intent.skillToUse != null)
            {
                PerformSkillRoutine(intent.skillToUse, false, intent.skillToUse.isUltimate);
            }
            else
            {
                ResolveTurnEnd(); // 스킬도 없고 차지(Charge)도 아니면 그냥 턴 넘김 (대기)
            }
        }
        else
        {
            // AI가 없거나 깡통인 경우 (기본 턴 넘김)
            ResolveTurnEnd();
        }
    }

    public void ShowCategoryMenu()
    {
        CombatUIManager.Instance.SetActionPanelActive(true);
        currentMenuState = MenuState.CategorySelect;

        string[] keys = new string[categoryMenuOrder.Count];
        for (int i = 0; i < categoryMenuOrder.Count; i++)
        {
            keys[i] = GetCategoryLocalizationKey(categoryMenuOrder[i]);
        }

        CombatUIManager.Instance.UpdateActionButtonsForCategory(keys);
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
            if (slotIndex >= 0 && slotIndex < categoryMenuOrder.Count)
            {
                selectedCategory = categoryMenuOrder[slotIndex];
                ShowSkillMenu(slotIndex);
            }
        }
        else if (currentMenuState == MenuState.SkillSelect)
        {
            if (slotIndex == 4) ShowCategoryMenu();
            else if (slotIndex < currentDisplaySkills.Count)
            {
                bool isUltimate = (slotIndex == 3);
                ExecuteSkill(currentDisplaySkills[slotIndex], true, isUltimate);
            }
        }
    }

    private void ExecuteSkill(SkillData skill, bool isPlayerAttacking, bool isUltimate = false)
    {
        CombatUIManager.Instance.SetActionPanelActive(false);
        CombatUIManager.Instance.SetWaitingPanelActive(true);
        currentMenuState = MenuState.Hidden;
        PerformSkillRoutine(skill, isPlayerAttacking, isUltimate);
    }

    // 스킬 처리 프로세스 (연산 -> 큐 적재 -> 실행)
    private void PerformSkillRoutine(SkillData skill, bool isPlayerAttacking, bool isUltimate = false)
    {
        // 1. 상태 스냅샷 및 초기화
        currentState.wasEnemyBrokenAtSkillStart = BreakManager.Instance.IsBroken(false);
        currentState.hasRewardedCritThisSkill = false;
        currentState.isMorningStarApRecoveredThisSkill = false;

        // ==========================================================
        //  [복구됨] 기(Ki) 차지(원기옥) 시작 판정
        // ==========================================================
        if (isPlayerAttacking && skill.skillLogic is SkillLogic_Gi && skill.currentEvolution == SkillEvolution.PathC && !currentState.isUnleashingCharge)
        {
            currentState.isPlayerCharging = true;
            currentState.chargingSkill = skill;
            string pName = playerData != null ? GetTranslatedText(playerData.playerNamekey) : "주인공";
            StartCoroutine(CombatUIManager.Instance.TypeCommentary($"{pName}이(가) 기를 모으기 시작합니다!"));
            ResolveTurnEnd();
            return;
        }

        // ==========================================================
        //  [복구됨] 실시간 스탯 산출 및 BattleCalculator 연산 (skillResult 생성)
        // ==========================================================
        int atkStr = StatManager.Instance.GetEffectiveStat(isPlayerAttacking, TargetStat.Strength);
        int atkDef = StatManager.Instance.GetEffectiveStat(isPlayerAttacking, TargetStat.Defense);
        int atkLck = StatManager.Instance.GetEffectiveStat(isPlayerAttacking, TargetStat.Luck);
        int atkSpd = StatManager.Instance.GetEffectiveStat(isPlayerAttacking, TargetStat.Speed);

        int defDef = StatManager.Instance.GetEffectiveStat(!isPlayerAttacking, TargetStat.Defense);
        int defSpd = StatManager.Instance.GetEffectiveStat(!isPlayerAttacking, TargetStat.Speed);
        int defBR = StatManager.Instance.GetEffectiveStat(!isPlayerAttacking, TargetStat.BreakResistance);

        // 연산 결과를 skillResult 변수에 담습니다!
        SkillResult skillResult = BattleCalculator.CalculateSkill(
            skill, isPlayerAttacking,
            currentPlayerStats, currentEnemyData,
            atkStr, atkDef, atkLck, atkSpd,
            defDef, defSpd, defBR
        );

        // ==========================================================
        // 2. 연출 대본 작성 (BattleVisualizer)
        // ==========================================================
        bool isPlayerDefending = !isPlayerAttacking;
        string attackerName = isPlayerAttacking ? (playerData != null ? GetTranslatedText(playerData.playerNamekey) : "주인공") : (currentEnemyData != null ? GetTranslatedText(currentEnemyData.enemyNameKey) : "적");
        string skillName = GetTranslatedText(skill.skillNameKey);

        if (isUltimate)
        {
            Sprite cutInSprite = isPlayerAttacking ? playerData?.cutIn : currentEnemyData?.CutIn;
            if (cutInSprite != null)
            {
                BattleVisualizer.Instance.EnqueueAction(() => CombatUIManager.Instance.InterruptAndTypeCommentary($"{attackerName}의 필살기!"));
                BattleVisualizer.Instance.EnqueueCutIn(cutInSprite);
            }
        }

        string commentary = !skillResult.anyHit ? $"{attackerName}의 {skillName}이(가) 빗나갔습니다!" :
                            (skillResult.anyCrit ? $"{attackerName}의 {skillName} 치명적으로 적중!" : $"{attackerName}의 {skillName} 적중!");

        float baseMultForUI = skill.GetCurrentDamageMultiplier();
        float logicMultForUI = skill.skillLogic != null ? skill.skillLogic.GetDamageMultiplier(skill, currentPlayerStats, currentEnemyData, isPlayerAttacking) : 1f;

        bool isAttackForUI = baseMultForUI > 0f || (baseMultForUI <= 0f && logicMultForUI > 0f && logicMultForUI != 1.0f);
        bool isPureUtility = !isAttackForUI && !skill.forceHitReaction;

        // ① 스킬 시전 초기 연출
        BattleVisualizer.Instance.EnqueueAction(() => ApplySkillCastUI(skill, isPlayerAttacking, skillResult, commentary, isPureUtility));

        // ② 다단 히트 연출 루프
        foreach (var hit in skillResult.hits)
        {
            BattleVisualizer.Instance.EnqueueAction(() =>
            {
                if (!hit.isHit) ProcessMissAction(isPlayerAttacking, isPlayerDefending, isPureUtility);
                else ProcessHitAction(hit, isPlayerAttacking, isPlayerDefending, isPureUtility, skillResult);
            });
            BattleVisualizer.Instance.EnqueueDelay(0.15f);
        }

        int successCount = 0;
        foreach (var hit in skillResult.hits) { if (hit.isHit) successCount++; }
        currentState.lastSuccessfulHits = successCount;

        // ③ 명중 시 특수효과 발동 로직 호출
        BattleVisualizer.Instance.EnqueueAction(() => skill.skillLogic?.ApplyEffectOnHit(skill, currentPlayerStats, currentEnemyData, isPlayerAttacking, skillResult.anyHit));

        // ④ 카운터 반격 판정
        bool isCounterTriggered = false;
        if (!skillResult.anyHit && !isPureUtility && isPlayerDefending)
        {
            var martialSkill = PlayerManager.Instance.unlockedSkills.Find(s => s.category == SkillCategory.Martial);
            if (martialSkill != null && martialSkill.skillLogic is SkillLogic_MorningStar msLogic)
            {
                if (BuffManager.Instance.GetEffects(true).Exists(e => e.effectData == msLogic.evasionBuffData) && martialSkill.currentEvolution == SkillEvolution.PathA)
                {
                    isCounterTriggered = true;
                    int levelIdx = Mathf.Clamp(martialSkill.skillLevel - 1, 0, msLogic.pathA_CounterRates.Length - 1);
                    int counterDmg = Mathf.RoundToInt(currentPlayerStats.strength * msLogic.pathA_CounterRates[levelIdx]);
                    Sprite counterImage = msLogic.GetCounterActionImage(martialSkill);

                    BattleVisualizer.Instance.EnqueueDelay(2.0f);
                    BattleVisualizer.Instance.EnqueueAction(() => ApplyCounterAndReflectUI(counterDmg, counterImage, false));
                    BattleVisualizer.Instance.EnqueueDelay(2.0f);
                }
            }
        }
        if (!isCounterTriggered) BattleVisualizer.Instance.EnqueueDelay(2.0f);

        // ⑤ 가드 버프 차감 및 인과율(반사) 판정
        if (skillResult.isGuardTriggered)
        {
            BattleVisualizer.Instance.EnqueueAction(() => { StyleRankManager.Instance?.OnSupportActionUsed(); BuffManager.Instance.ConsumeGuardEffect(true); });

            if (isPlayerDefending)
            {
                float reflectRatio = 0f;
                // 방어력 비례 반사 비율 (예시 로직 - PlayerManager에 구현되어 있다면 호출)
                if (PlayerManager.Instance != null) reflectRatio = PlayerManager.Instance.GetReflectRatio();

                if (reflectRatio > 0f)
                {
                    int reflectDamage = Mathf.Max(1, Mathf.RoundToInt(skillResult.totalMitigatedDamage * reflectRatio));
                    Sprite reflectSprite = playerData.reflectImage != null ? playerData.reflectImage : playerData.guardImage;

                    BattleVisualizer.Instance.EnqueueAction(() => ApplyCounterAndReflectUI(reflectDamage, reflectSprite, true));
                    BattleVisualizer.Instance.EnqueueDelay(2.0f);
                }
            }
        }

        // ⑥ 화면 및 상태 리셋
        BattleVisualizer.Instance.EnqueueAction(() => ResetCombatUI(isPlayerAttacking, isPlayerDefending, isUltimate, skill));

        // ==========================================================
        // 3. 지휘관 권한 위임 및 턴 종료 대기
        // ==========================================================
        BattleVisualizer.Instance.StartSequence(() =>
        {
            if (isPlayerAttacking && currentState.isUnleashingCharge) currentState.isUnleashingCharge = false;

            if (currentEnemyHp <= 0 || currentPlayerStats.currentHp <= 0) EndCombat(currentEnemyHp <= 0);
            else ResolveTurnEnd();
        });
    }

    // 스킬 시전 초기 연출 (이미지, 대사, 코스트 지불 등)
    private void ApplySkillCastUI(SkillData skill, bool isPlayerAttacking, SkillResult skillResult, string commentary, bool isPureUtility)
    {
        if (skill.skillLogic is SkillLogic_FantasticDreamer dreamLogic)
            CombatUIManager.Instance.ShowFantasticDreamerDice(dreamLogic.LastRolledStage, isPlayerAttacking);

        // 1. 내 이미지 변경 및 코스트 지불
        CombatUIManager.Instance.SetCasterImage(isPlayerAttacking, skill.skillActionImage);
        skill.skillLogic?.PaySkillCost(skill, currentPlayerStats, currentEnemyData, isPlayerAttacking);
        CompanionManager.Instance.UpdateEmotion(skillResult.anyHit ?
            (isPlayerAttacking ? CompanionManager.Emotion.Happy : CompanionManager.Emotion.Worried) :
            (isPlayerAttacking ? CompanionManager.Emotion.Worried : CompanionManager.Emotion.Happy));

        // 2. 방어자 이미지 변경
        Sprite reactionSprite = null;
        bool isDefenderInvincible = BuffManager.Instance.GetEffects(!isPlayerAttacking).Exists(e => e.effectData.specialType == SpecialEffectType.Invincible);

        if (skillResult.anyHit)
        {
            if (!isPureUtility)
            {
                if (isDefenderInvincible) reactionSprite = null;
                else reactionSprite = skillResult.isGuardTriggered
                    ? (isPlayerAttacking ? currentEnemyData?.guardImage : playerData?.guardImage)
                    : (isPlayerAttacking ? currentEnemyData?.hit : playerData?.hit);
            }
        }
        else
        {
            if (!isPureUtility) reactionSprite = isPlayerAttacking ? currentEnemyData?.evade : playerData?.evade;
        }

        CombatUIManager.Instance.SetDefenderImage(!isPlayerAttacking, reactionSprite);

        // 3. 텍스트 및 크리티컬 연출
        CombatUIManager.Instance.InterruptAndTypeCommentary(commentary);
        if (skillResult.anyCrit)
            CombatUIManager.Instance.StartCoroutine(CombatUIManager.Instance.ShowCritAlert());
    }

    // 단일 타격 실패(회피) 연출
    // ==========================================================
    private void ProcessMissAction(bool isPlayerAttacking, bool isPlayerDefending, bool isPureUtility)
    {
        if (isPureUtility) return;

        BattleEventSystem.CallEvaded(isPlayerDefending);

        Sprite evadeSprite = isPlayerDefending ? playerData?.evade : currentEnemyData?.evade;
        CombatUIManager.Instance.SetDefenderImage(!isPlayerAttacking, evadeSprite);

        if (isPlayerDefending)
        {
            StyleRankManager.Instance.OnEvade();

            // [진화 B] 난식 턴 당기기
            var martialSkill = PlayerManager.Instance.unlockedSkills.Find(s => s.category == SkillCategory.Martial);
            if (martialSkill != null && martialSkill.skillLogic is SkillLogic_MorningStar msLogic)
            {
                bool hasEvasionBuff = BuffManager.Instance.GetEffects(true).Exists(e => e.effectData == msLogic.evasionBuffData);
                if (hasEvasionBuff && martialSkill.currentEvolution == SkillEvolution.PathB && !currentState.isMorningStarApRecoveredThisSkill)
                {
                    var playerEntity = TurnManager.Instance.turnQueue.Find(e => e.isPlayer);
                    if (playerEntity != null)
                    {
                        playerEntity.actionGauge += msLogic.pathB_ApRecovery;
                        currentState.isMorningStarApRecoveredThisSkill = true;
                        DevLog.Log($"[새벽별:난식] 회피 성공! 행동 게이지 {msLogic.pathB_ApRecovery} 회복.");
                    }
                }
            }
        }
    }

    // 단일 타격 성공(명중) 연출
    // ==========================================================
    private void ProcessHitAction(HitResult hit, bool isPlayerAttacking, bool isPlayerDefending, bool isPureUtility, SkillResult skillResult)
    {
        if (hit.isCrit && isPlayerAttacking && !currentState.hasRewardedCritThisSkill)
        {
            StyleRankManager.Instance.OnCriticalHit();
            currentState.hasRewardedCritThisSkill = true;
        }

        if (isPlayerAttacking)
        {
            ApplyDamageToEntity(false, hit.damage);
            if (!currentState.isBombActive) currentState.accumulatedDamage += hit.damage;
        }
        else
        {
            ApplyDamageToEntity(true, hit.damage);
            bool isInvincible = BuffManager.Instance.GetEffects(true).Exists(e => e.effectData.specialType == SpecialEffectType.Invincible);

            if (!skillResult.isGuardTriggered && !isInvincible) StyleRankManager.Instance.OnPlayerHit();
            else if (isInvincible) DevLog.Log("[무하한] 무적 상태이므로 스타일 랭크가 감소하지 않습니다.");

            if (currentState.isPlayerCharging && hit.damage > 0)
            {
                currentState.isPlayerCharging = false;
                currentState.chargingSkill = null;
                CombatUIManager.Instance.SpawnDamageText("Broken!", false, true);
                DevLog.Log("[원기옥] 피격당하여 기 모으기가 취소되었습니다!");
            }
        }

        if (isPlayerAttacking && !BreakManager.Instance.IsBroken(false))
            if (BreakManager.Instance.AddBreakDamage(false, hit.breakDamage)) UpdateTurnOrderUI();

        if (!isPlayerAttacking && !BreakManager.Instance.IsBroken(true))
            if (BreakManager.Instance.AddBreakDamage(true, hit.breakDamage)) UpdateTurnOrderUI();

        if (!isPureUtility) BattleEventSystem.CallDamageTaken(isPlayerDefending, hit.damage, hit.isCrit);
    }

    // 새벽별 카운터 및 인과율 반사 연출
    // ==========================================================
    private void ApplyCounterAndReflectUI(int damage, Sprite defenderImage, bool isReflect)
    {
        CombatUIManager.Instance.SetDefenderImage(true, defenderImage);
        CombatUIManager.Instance.SetDefenderImage(false, currentEnemyData?.hit);
        ApplyDamageToEntity(false, damage);

        if (isReflect)
        {
            BattleEventSystem.CallDamageTaken(false, damage, false);
            CombatUIManager.Instance.InterruptAndTypeCommentary($"[인과율 발동!] 튕겨낸 힘으로 적에게 {damage}의 고정 피해를 반사합니다!");
        }
        else
        {
            CombatUIManager.Instance.SpawnDamageText(damage.ToString(), false, false);
            DevLog.Log($"[새벽별:멸식] 카운터 발동! {damage} 피해");
        }
    }

    // 화면 복구 (이펙트, 랭크, 이미지 초기화)
    // ==========================================================
    private void ResetCombatUI(bool isPlayerAttacking, bool isPlayerDefending, bool isUltimate, SkillData skill)
    {
        CombatUIManager.Instance.ClearCombatEffects();

        if (isPlayerAttacking)
        {
            StyleRankManager.Instance.OnSkillUsed(selectedCategory);
            StyleRankManager.Instance.ResetTurnState();
            if (isUltimate) StyleRankManager.Instance.ResetRankForUltimate();
        }

        if (!(isPlayerAttacking && currentState.isPlayerCharging))
        {
            CombatUIManager.Instance.ResetCasterImage(isPlayerAttacking);
        }

        bool isDefenderBroken = (!isPlayerAttacking && BreakManager.Instance.IsBroken(true)) || (isPlayerAttacking && BreakManager.Instance.IsBroken(false));

        if (!isDefenderBroken)
        {
            if (isPlayerDefending && currentState.isPlayerCharging && currentState.chargingSkill != null)
                CombatUIManager.Instance.SetDefenderImage(true, currentState.chargingSkill.skillActionImage);
            else
                CombatUIManager.Instance.ResetDefenderImage(isPlayerDefending);
        }
        else
        {
            Sprite groggySprite = isPlayerDefending ? playerData?.breakImage : currentEnemyData?.breakImage;
            if (groggySprite != null) CombatUIManager.Instance.SetDefenderImage(isPlayerDefending, groggySprite);
            DevLog.Log($"[{(isPlayerDefending ? "주인공" : "적")}]가 아직 그로기 상태이므로 전용 Break 이미지로 복구합니다.");
        }
    }

    public bool ApplyDamageToEnemy(int damage)
    {
        return ApplyDamageToEntity(false, damage);
    }

    public bool ApplyDamageToEntity(bool isPlayerTarget, int damage)
    {
        if (isPlayerTarget)
        {
            currentPlayerStats.currentHp = Mathf.Max(0, currentPlayerStats.currentHp - damage);
            BattleEventSystem.CallHpChanged(true, currentPlayerStats.currentHp, currentPlayerStats.maxHp);
            return currentPlayerStats.currentHp <= 0; // 죽었는지 여부 반환
        }
        else
        {
            currentEnemyHp = Mathf.Max(0, currentEnemyHp - damage);
            BattleEventSystem.CallHpChanged(false, currentEnemyHp, currentEnemyData.maxHp);
            return currentEnemyHp <= 0;
        }
    }

    public void EndCombat(bool isWin)
    {
        if (PlayerManager.Instance != null && currentPlayerStats != null)
        {
            PlayerManager.Instance.stats.currentHp = currentPlayerStats.currentHp;
        }

        // TODO: 전투 종료 씬 전환 및 보상 로직
    }

    public void ResolveTurnEnd()
    {
        StartCoroutine(ResolveTurnEndRoutine());
    }

    private IEnumerator ResolveTurnEndRoutine()
    {
        yield return HandleSpecialExpirations();

        if (currentActiveEntity != null && currentActiveEntity.type == EntityType.Enemy)
        {
            var pEffects = BuffManager.Instance.GetEffects(true);
            int removed = pEffects.RemoveAll(e => e.effectData.specialType == SpecialEffectType.Invincible);
            if (removed > 0)
            {
                CombatUIManager.Instance.RefreshBuffUI();
                DevLog.Log("[무하한] 적의 턴이 종료되어 무적 효과가 해제되었습니다.");
            }
        }

        CompanionManager.Instance.UpdateEmotion(CompanionManager.Emotion.Normal);

        bool playerTookDamage = currentPlayerStats.currentHp < playerHpAtTurnStart;
        bool enemyTookDamage = currentEnemyHp < enemyHpAtTurnStart;

        BreakManager.Instance.RecoverBreakOnTurnEnd(true, playerTookDamage);
        BreakManager.Instance.RecoverBreakOnTurnEnd(false, enemyTookDamage);

        if (currentActiveEntity != null)
        {
            bool isPlayerTurn = currentActiveEntity.isPlayer;
            var effects = BuffManager.Instance.GetEffects(isPlayerTurn);
            float hpRegenRate = 0f;
            float breakRegenRate = 0f;

            foreach (var eff in effects)
            {
                if (eff.effectData.specialType == SpecialEffectType.HpRegen)
                    hpRegenRate += eff.value;
                else if (eff.effectData.specialType == SpecialEffectType.BreakRegen)
                    breakRegenRate += eff.value;
            }

            if (hpRegenRate > 0f || breakRegenRate > 0f)
            {
                string targetName = isPlayerTurn ? (playerData != null ? GetTranslatedText(playerData.playerNamekey) : "셰리") : "적";

                // 1. 회복 알림 텍스트 출력 (0.5초간 타자 치듯 출력)
                yield return CombatUIManager.Instance.TypeCommentary($"{targetName}의 지속 회복 효과 발동!", true, 0.5f);

                // 2. 실제 회복 수치 연산 및 데미지 텍스트 팝업
                if (hpRegenRate > 0f)
                {
                    if (isPlayerTurn)
                    {
                        int healAmount = Mathf.RoundToInt(currentPlayerStats.maxHp * hpRegenRate);
                        currentPlayerStats.currentHp = Mathf.Clamp(currentPlayerStats.currentHp + healAmount, 0, currentPlayerStats.maxHp);
                        CombatUIManager.Instance.playerStatusUI.UpdateHP(currentPlayerStats.currentHp, currentPlayerStats.maxHp);
                        CombatUIManager.Instance.SpawnDamageText($"<color=#00FF00>+{healAmount}</color>", false, true);
                        DevLog.Log($"[재생] 턴 종료! 셰리의 체력이 {healAmount} 회복되었습니다.");
                    }
                    else
                    {
                        int healAmount = Mathf.RoundToInt(currentEnemyData.maxHp * hpRegenRate);
                        currentEnemyHp = Mathf.Clamp(currentEnemyHp + healAmount, 0, currentEnemyData.maxHp);
                        CombatUIManager.Instance.enemyStatusUI.UpdateHP(currentEnemyHp, currentEnemyData.maxHp);
                        CombatUIManager.Instance.SpawnDamageText($"<color=#00FF00>+{healAmount}</color>", false, false);
                    }
                }

                if (breakRegenRate > 0f)
                {
                    // 턴 종료 시 그로기 게이지 즉시 회복
                    BreakManager.Instance.RecoverBreakInstantly(isPlayerTurn, breakRegenRate);
                }

                // 3. 유저가 초록색 회복 데미지 텍스트와 UI 바가 차오르는 것을 감상할 수 있도록 1초 대기!
                yield return new WaitForSeconds(1.0f);
            }

            if (currentActiveEntity.isPlayer) BuffManager.Instance.UpdateEffectsOnTurnEnd(true);
            else if (currentActiveEntity.type == EntityType.Enemy) BuffManager.Instance.UpdateEffectsOnTurnEnd(false);
        }

        CalculateNextTurn();
    }

    private IEnumerator HandleSpecialExpirations()
    {
        bool isPlayerTurn = currentActiveEntity.isPlayer;
        var effects = BuffManager.Instance.GetEffects(isPlayerTurn);

        // 만료될 효과들 찾기 (turnsLeft가 1이고 isNewlyApplied가 false인 것)
        for (int i = effects.Count - 1; i >= 0; i--)
        {
            var e = effects[i];
            if (e.turnsLeft == 1 && !e.isNewlyApplied)
            {
                // 1. [진화 A] 과열 폭발 (주인공 피격)
                if (e.effectData.specialType == SpecialEffectType.Overheat)
                {
                    yield return CombatUIManager.Instance.TypeCommentary("과열(Overheat) 디버프 발동!!", true, 0.5f);

                    int selfDamage = Mathf.RoundToInt(currentPlayerStats.currentHp * 0.4f);
                    ApplyDamageToEntity(true, selfDamage);

                    CombatUIManager.Instance.SetDefenderImage(true, playerData.hit); // 주인공 피격 이미지
                    CombatUIManager.Instance.SpawnDamageText(selfDamage.ToString(), false, true);
                    BattleEventSystem.CallHpChanged(true, currentPlayerStats.currentHp, currentPlayerStats.maxHp);

                    yield return new WaitForSeconds(1.0f);
                    CombatUIManager.Instance.ResetDefenderImage(true);
                }

                // 2. [진화 B] 피해 누적 폭발 (적 피격)
                if (e.effectData.specialType == SpecialEffectType.DamageAccumulator)
                {
                    yield return CombatUIManager.Instance.TypeCommentary("렛 유 다운(Let You Down) 추가 피해 발동!", true, 0.5f);

                    // 기록된 피해의 50%를 추가로 입힘
                    int extraDmg = Mathf.RoundToInt(currentState.accumulatedDamage * 0.5f);
                    ApplyDamageToEntity(false, extraDmg);

                    CombatUIManager.Instance.SetDefenderImage(false, currentEnemyData.hit); // 적 피격 이미지
                    CombatUIManager.Instance.SpawnDamageText(extraDmg.ToString(), true, false);
                    BattleEventSystem.CallHpChanged(false, currentEnemyHp, currentEnemyData.maxHp);

                    currentState.accumulatedDamage = 0; // 초기화
                    yield return new WaitForSeconds(1.0f);
                    CombatUIManager.Instance.ResetDefenderImage(false);
                }
            }
        }
    }

    public bool IsCurrentTurnOwner(bool isPlayerTarget)
    {
        if (currentActiveEntity == null) return false;
        if (isPlayerTarget && currentActiveEntity.isPlayer) return true;
        if (!isPlayerTarget && currentActiveEntity.type == EntityType.Enemy) return true;
        return false;
    }

    private string GetTranslatedText(string key)
    {
        if (string.IsNullOrEmpty(key)) return "";
        if (LocalizationManager.Instance != null) return LocalizationManager.Instance.GetText(key);
        return key;
    }
}