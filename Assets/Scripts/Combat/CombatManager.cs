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
    public int totalExcessHealThisSkill = 0;

    public bool hasResurrected = false;
}

public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance;

    [Header("데이터 연결")]
    public PlayerData playerData;

    [Header("분석창")]
    public AnalysisUI analysisUI;

    [Header("특수 스탯 표시용 에셋 매핑 (Passives)")]
    public StatusEffectData pEffect_DamageAmp;
    public StatusEffectData pEffect_DamageReduction;
    public StatusEffectData pEffect_CritRate;
    public StatusEffectData pEffect_CritDamage;
    public StatusEffectData pEffect_LifeSteal;
    public StatusEffectData pEffect_TrueDamage;
    public StatusEffectData pEffect_Accuracy;
    public StatusEffectData pEffect_Evasion;
    public StatusEffectData pEffect_HealAmp;

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

    public void RefreshSpecialStatsProgressUI()
    {
        if (BuffManager.Instance == null) return;

        // 1. 아군(Player) 특수 스탯 동기화
        if (currentPlayerStats != null)
        {
            BuffManager.Instance.UpdatePermanentPassive(true, pEffect_DamageAmp, currentPlayerStats.finalDamageAmp);
            BuffManager.Instance.UpdatePermanentPassive(true, pEffect_DamageReduction, currentPlayerStats.finalDamageReduction);
            BuffManager.Instance.UpdatePermanentPassive(true, pEffect_CritRate, currentPlayerStats.critRate);
            BuffManager.Instance.UpdatePermanentPassive(true, pEffect_CritDamage, currentPlayerStats.critDamage, 1.5f); // 기본값 1.5f 기준
            BuffManager.Instance.UpdatePermanentPassive(true, pEffect_LifeSteal, currentPlayerStats.lifeSteal);
            BuffManager.Instance.UpdatePermanentPassive(true, pEffect_TrueDamage, currentPlayerStats.trueDamageConversion);
            BuffManager.Instance.UpdatePermanentPassive(true, pEffect_Accuracy, currentPlayerStats.bonusAccuracy);
            BuffManager.Instance.UpdatePermanentPassive(true, pEffect_Evasion, currentPlayerStats.bonusEvasion);
            BuffManager.Instance.UpdatePermanentPassive(true, pEffect_HealAmp, currentPlayerStats.healingReceivedAmp);
        }

        // 2. 적군(Enemy) 특수 스탯 동기화
        if (currentEnemyData != null)
        {
            BuffManager.Instance.UpdatePermanentPassive(false, pEffect_DamageAmp, currentEnemyData.damageGivenAmp);
            BuffManager.Instance.UpdatePermanentPassive(false, pEffect_DamageReduction, currentEnemyData.damageReduction);
            BuffManager.Instance.UpdatePermanentPassive(false, pEffect_CritRate, currentEnemyData.critRate);
            BuffManager.Instance.UpdatePermanentPassive(false, pEffect_CritDamage, currentEnemyData.critDamage, 1.5f);
            BuffManager.Instance.UpdatePermanentPassive(false, pEffect_LifeSteal, currentEnemyData.lifeSteal);
            BuffManager.Instance.UpdatePermanentPassive(false, pEffect_TrueDamage, currentEnemyData.trueDamageConversion);
            BuffManager.Instance.UpdatePermanentPassive(false, pEffect_Accuracy, currentEnemyData.bonusAccuracy);
            BuffManager.Instance.UpdatePermanentPassive(false, pEffect_Evasion, currentEnemyData.bonusEvasion);
            BuffManager.Instance.UpdatePermanentPassive(false, pEffect_HealAmp, currentEnemyData.healingReceivedAmp);
        }

        if (CombatUIManager.Instance != null)
        {
            CombatUIManager.Instance.RefreshBuffUI();
        }
    }

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
            // 1. 순수 스탯 대신 '아이템이 적용된 스냅샷'을 전투 시작 데이터로 가져옵니다!
            currentPlayerStats = PlayerManager.Instance.GetItemModifiedStats();
            currentEnemyData = Instantiate(PlayerManager.Instance.currentEnemyToFight);
            if (currentEnemyData.aiBrain != null)
            {
                currentEnemyData.aiBrain = Instantiate(currentEnemyData.aiBrain);
            }
            currentEnemyData.currentHp = currentEnemyData.maxHp;

            // StatManager는 이제 이 '아이템 적용 스탯'을 베이스로 삼고 전투 버프를 계산합니다.
            if (StatManager.Instance != null)
                StatManager.Instance.InitStats(currentPlayerStats, currentEnemyData);
        }

        // 2. 그 이후에 UI가 세팅된 스탯을 기반으로 체력바를 그립니다.
        if (currentPlayerStats != null && playerData != null)
            CombatUIManager.Instance.InitPlayerUI(currentPlayerStats.maxHp, currentPlayerStats.currentHp, playerData.normal);

        if (currentEnemyData != null)
        {
            currentEnemyHp = currentEnemyData.currentHp;
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

        bool isFastCombat = PlayerPrefs.GetInt("FastCombat", 0) == 1;
        Time.timeScale = isFastCombat ? 2.0f : 1.0f;

        if (CombatUIManager.Instance != null)
        {
            CombatUIManager.Instance.UpdateFastCombatIcon(isFastCombat);
        }
        RefreshSpecialStatsProgressUI();
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

        RefreshSpecialStatsProgressUI();

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

        if (owner.type == EntityType.Player && PlayerManager.Instance != null)
        {
            var syn = PlayerManager.Instance.GetCurrentSynergies();
            var inventory = PlayerManager.Instance.inventory;

            // [트릭스터 4점]
            if (syn.GetValueOrDefault(ItemClass.Trickster) >= 4) ApplyRandomTricksterStatDebuff(0.05f);

            // [트릭스터 희귀] 가짜 웃음 수치 합산
            var trickRares = inventory.FindAll(x => x.data.itemClass == ItemClass.Trickster && x.data.grade == ItemGrade.Rare);
            float trickRareVal = 0f;
            foreach (var r in trickRares) trickRareVal += r.starLevel == 1 ? 0.02f : (r.starLevel == 2 ? 0.08f : 0.25f);
            if (trickRareVal > 0f) ApplyRandomTricksterStatDebuff(trickRareVal);

            // [트릭스터 에픽] 기괴한 가면 수치 합산 (출혈, 화상 계수 분리)
            var trickEpics = inventory.FindAll(x => x.data.itemClass == ItemClass.Trickster && x.data.grade == ItemGrade.Epic);
            float trickEpicVal = 0f, trickBleedVal = 0f, trickBurnVal = 0f;
            foreach (var e in trickEpics)
            {
                trickEpicVal += e.starLevel == 1 ? 0.02f : (e.starLevel == 2 ? 0.08f : 0.30f);
                trickBleedVal += e.starLevel == 1 ? 1.0f : (e.starLevel == 2 ? 2.0f : 3.0f); // 100/200/300%
                trickBurnVal += e.starLevel == 1 ? 0.02f : (e.starLevel == 2 ? 0.03f : 0.04f); // 2/3/4%
            }
            if (trickEpics.Count > 0) ApplyRandomTricksterEpicDebuff(trickEpicVal, trickBleedVal, trickBurnVal);
        }

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
                CombatUIManager.Instance.SpawnDamageText("★" + bleedDmg.ToString(), false, false);

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
                CombatUIManager.Instance.SpawnDamageText("★" + burnDmg.ToString(), false, false);

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
                CombatUIManager.Instance.SpawnDamageText("★" + currentState.savedBombDamage.ToString(), false, false);
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
            intent = currentEnemyData.aiBrain.DecideNextAction(enemyTurnCount, currentPlayerStats, currentEnemyData);
            enemyTurnCount++;
        }

        // 2. 계획서에 스킬이 정상적으로 들어있다면 실행합니다. (미카엘의 모든 행동)
        if (intent != null && intent.skillToUse != null)
        {
            PerformSkillRoutine(intent.skillToUse, false, intent.skillToUse.isUltimate);
        }
        else
        {
            // AI가 없거나 깡통인 경우, 혹은 쉴 때 (대기)
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
        if (analysisUI != null) analysisUI.Close();

        // 1. 상태 스냅샷 및 초기화
        currentState.wasEnemyBrokenAtSkillStart = BreakManager.Instance.IsBroken(false);
        currentState.hasRewardedCritThisSkill = false;
        currentState.isMorningStarApRecoveredThisSkill = false;
        currentState.totalExcessHealThisSkill = 0;

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
        int defCurrentHp = isPlayerAttacking ? currentEnemyHp : currentPlayerStats.currentHp;
        int defMaxHp = isPlayerAttacking ? currentEnemyData.maxHp : currentPlayerStats.maxHp;

        // 연산 결과를 skillResult 변수에 담습니다!
        SkillResult skillResult = BattleCalculator.CalculateSkill(
            skill, isPlayerAttacking,
            currentPlayerStats, currentEnemyData,
            atkStr, atkDef, atkLck, atkSpd,
            defDef, defSpd, defBR, defCurrentHp, defMaxHp
        );

        // ==========================================================
        // 2. 연출 대본 작성 (BattleVisualizer)
        // ==========================================================
        float baseMultForUI = skill.GetCurrentDamageMultiplier();
        float logicMultForUI = skill.skillLogic != null ? skill.skillLogic.GetDamageMultiplier(skill, currentPlayerStats, currentEnemyData, isPlayerAttacking) : 1f;

        bool isAttackForUI = baseMultForUI > 0f || (baseMultForUI <= 0f && logicMultForUI > 0f && logicMultForUI != 1.0f);
        bool isPureUtility = !isAttackForUI && !skill.forceHitReaction; // 여기서 선언!

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

        string commentary = "";
        if (isPureUtility)
        {
            commentary = $"{attackerName}이(가) {skillName}을(를) 시전합니다!";
        }
        else
        {
            commentary = !skillResult.anyHit ? $"{attackerName}의 {skillName}이(가) 빗나갔습니다!" :
                         (skillResult.anyCrit ? $"{attackerName}의 {skillName} 치명적으로 적중!" : $"{attackerName}의 {skillName} 적중!");
        }

        // ① 스킬 시전 초기 연출
        BattleVisualizer.Instance.EnqueueAction(() => ApplySkillCastUI(skill, isPlayerAttacking, skillResult, commentary, isPureUtility));

        // ②-1. [신규 추가] 전체 스킬 결과에 대한 1회성 판정 (스타일 랭크 및 회피 특수 효과)
        if (isPlayerDefending && !isPureUtility)
        {
            bool isInvincible = BuffManager.Instance.GetEffects(true).Exists(e => e.effectData.specialType == SpecialEffectType.Invincible);

            // [완전 회피] 모든 타격이 빗나갔을 때 딱 한 번만 발동!
            if (!skillResult.anyHit)
            {
                StyleRankManager.Instance.OnEvade();

                // 새벽별 난식 턴 당기기 (1회만 발동)
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
                            DevLog.Log($"[새벽별:난식] 완벽 회피 성공! 행동 게이지 {msLogic.pathB_ApRecovery} 회복.");
                        }
                    }
                }
            }
            // [피격] 1타라도 스쳤을 때 딱 한 번만 발동!
            else
            {
                if (!skillResult.isGuardTriggered && !isInvincible)
                {
                    StyleRankManager.Instance.OnPlayerHit();
                }
                else if (isInvincible)
                {
                    DevLog.Log("[무하한] 무적 상태이므로 스타일 랭크가 감소하지 않습니다.");
                }
            }
        }

        // ②-2. 다단 히트 연출 루프 (데미지 및 화면 텍스트 전담)
        foreach (var hit in skillResult.hits)
        {
            BattleVisualizer.Instance.EnqueueAction(() =>
            {
                // [수정] ProcessMissAction에 skillResult를 넘겨서 1타라도 맞았으면 회피 모션을 막습니다.
                if (!hit.isHit) ProcessMissAction(isPlayerAttacking, isPlayerDefending, isPureUtility, skillResult);
                else ProcessHitAction(hit, isPlayerAttacking, isPlayerDefending, isPureUtility, skillResult, skill);
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
        if (skillResult.anyCrit && !isPureUtility)
            CombatUIManager.Instance.StartCoroutine(CombatUIManager.Instance.ShowCritAlert());
    }

    // 단일 타격 실패(회피) 연출
    // ==========================================================
    private void ProcessMissAction(bool isPlayerAttacking, bool isPlayerDefending, bool isPureUtility, SkillResult skillResult)
    {
        if (isPureUtility) return;

        BattleEventSystem.CallEvaded(isPlayerDefending);

        // [핵심 수정 2] 1타라도 스친 다단히트 공격이라면 회피 모션을 띄우지 않고 묵묵히 피격(Hit) 상태를 유지합니다!
        if (!skillResult.anyHit)
        {
            Sprite evadeSprite = isPlayerDefending ? playerData?.evade : currentEnemyData?.evade;
            CombatUIManager.Instance.SetDefenderImage(!isPlayerAttacking, evadeSprite);
        }

        // (StyleRank 및 새벽별 로직은 PerformSkillRoutine으로 이관되어 삭제됨)
    }

    // 단일 타격 성공(명중) 연출
    // ==========================================================
    private void ProcessHitAction(HitResult hit, bool isPlayerAttacking, bool isPlayerDefending, bool isPureUtility, SkillResult skillResult, SkillData skill)
    {
        if (isPureUtility) return;

        if (hit.isCrit && isPlayerAttacking && !currentState.hasRewardedCritThisSkill)
        {
            StyleRankManager.Instance.OnCriticalHit();
            currentState.hasRewardedCritThisSkill = true;
        }

        if (isPlayerAttacking)
        {
            ApplyDamageToEntity(false, hit.damage);
            if (!currentState.isBombActive) currentState.accumulatedDamage += hit.damage;

            // [신규] 데몬 시너지 / 흡혈 아이템 '글로벌 흡혈' 로직 적용
            float currentLifeSteal = currentPlayerStats.lifeSteal;

            if (skill != null && skill.skillLogic != null)
            {
                currentLifeSteal += skill.skillLogic.GetSkillBonusLifesteal(skill);
            }

            // [데몬 희귀 아이템 - 귀면의 파편] 잃은 체력 비례 흡혈률 상승!
            if (currentActiveEntity != null && currentActiveEntity.type == EntityType.Player && PlayerManager.Instance != null)
            {
                var demonRares = PlayerManager.Instance.inventory.FindAll(x => x.data.itemClass == ItemClass.Demon && x.data.grade == ItemGrade.Rare);
                float missingRatio = (float)(currentPlayerStats.maxHp - currentPlayerStats.currentHp) / currentPlayerStats.maxHp;

                foreach (var dRare in demonRares)
                {
                    float maxBonus = dRare.starLevel == 1 ? 0.02f : (dRare.starLevel == 2 ? 0.10f : 0.30f);
                    currentLifeSteal += (missingRatio * maxBonus);
                }
            }

            if (hit.damage > 0 && currentLifeSteal > 0f && currentActiveEntity != null && currentActiveEntity.type == EntityType.Player)
            {
                float baseHeal = hit.damage * currentLifeSteal;

                // [신규] 마성 강화(4점) 및 오니의 검은 피(에픽) - 회복량 증폭 적용!
                int healAmount = Mathf.RoundToInt(baseHeal * (1f + currentPlayerStats.healingReceivedAmp));

                if (healAmount > 0)
                {
                    int excessHeal = (currentPlayerStats.currentHp + healAmount) - currentPlayerStats.maxHp;
                    currentPlayerStats.currentHp = Mathf.Clamp(currentPlayerStats.currentHp + healAmount, 0, currentPlayerStats.maxHp);

                    CombatUIManager.Instance.playerStatusUI.UpdateHP(currentPlayerStats.currentHp, currentPlayerStats.maxHp);
                    CombatUIManager.Instance.SpawnDamageText($"<color=#00FF00>+{healAmount}</color>", false, true);

                    // [신규] 데몬 6점 및 전설 - 초과 회복 버프 발동
                    if (excessHeal > 0) ApplyOverhealBuff(excessHeal);
                }
            }
        }
        else // 적(Enemy)이 공격했을 때의 처리
        {
            // 1. 일반 타격 데미지 적용 (단 한 번만!)
            ApplyDamageToEntity(true, hit.damage);

            // 2. 적군 흡혈 로직
            float enemyLifeSteal = currentEnemyData.lifeSteal;
            if (skill != null && skill.skillLogic != null)
                enemyLifeSteal += skill.skillLogic.GetSkillBonusLifesteal(skill);

            if (hit.damage > 0 && enemyLifeSteal > 0f)
            {
                float baseHeal = hit.damage * enemyLifeSteal;
                int healAmount = Mathf.RoundToInt(baseHeal * (1f + currentEnemyData.healingReceivedAmp));

                if (healAmount > 0)
                {
                    currentEnemyHp = Mathf.Clamp(currentEnemyHp + healAmount, 0, currentEnemyData.maxHp);
                    currentEnemyData.currentHp = currentEnemyHp;

                    if (CombatUIManager.Instance != null)
                    {
                        CombatUIManager.Instance.enemyStatusUI.UpdateHP(currentEnemyHp, currentEnemyData.maxHp);
                        CombatUIManager.Instance.SpawnDamageText($"<color=#00FF00>+{healAmount}</color>", false, false);
                    }
                    DevLog.Log($"[적 흡혈] {healAmount} 회복!");
                }
            }

            // 3. [핵심] 특수 효과 처리 (스택 폭발 등)
            // 이제 하드코딩 없이 어떤 보스 스킬이든 TryProcessHitEffect가 구현되어 있으면 호출됩니다.
            int explosionDamage = skill.skillLogic.TryProcessHitEffect(currentEnemyData);

            if (explosionDamage > 0)
            {
                // 특수 피해 적용 (이미 일반 데미지는 위에서 들어갔으므로 이것만 추가로 들어감)
                CombatManager.Instance.ApplyDamageToEntity(true, explosionDamage);

                // 연출: 피격 이미지 + 보라색 데미지 텍스트
                CombatUIManager.Instance.SetDefenderImage(true, playerData.hit);
                CombatUIManager.Instance.SpawnDamageText($"★{explosionDamage}", false, true);

                DevLog.Log($"[스킬 특수 효과] 특수 피해 {explosionDamage} 발생!");
            }

            // 4. 기 모으기 파괴 로직
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
        bool isDead = false; // 죽었는지 판정할 결과를 잠시 담아둘 변수

        if (isPlayerTarget)
        {
            int hpAfterDamage = currentPlayerStats.currentHp - damage;

            // 버서커 6시너지 & 전설 아이템 (사신 거부 / 부활)
            if (hpAfterDamage <= 0 && PlayerManager.Instance != null && !currentState.hasResurrected)
            {
                var syn = PlayerManager.Instance.GetCurrentSynergies();
                var inventory = PlayerManager.Instance.inventory;

                bool has6Point = syn.GetValueOrDefault(ItemClass.Berserker) >= 6;
                bool hasLegendary = inventory.Exists(x => x.data.itemClass == ItemClass.Berserker && x.data.grade == ItemGrade.Legendary);

                if (has6Point || hasLegendary)
                {
                    currentState.hasResurrected = true;

                    if (has6Point && hasLegendary)
                    {
                        currentPlayerStats.currentHp = currentPlayerStats.maxHp;
                        CombatUIManager.Instance.SpawnDamageText("<color=#00FF00>Resurrect!</color>", false, true);
                        DevLog.Log("[불굴의 투지+전설] 치명상을 입었으나, 최대 체력으로 부활합니다!");
                    }
                    else
                    {
                        currentPlayerStats.currentHp = 1;
                        CombatUIManager.Instance.SpawnDamageText("<color=#FF0000>Endure!</color>", false, true);
                        DevLog.Log("[사신 거부] 치명상을 입었으나, 체력 1로 버텨냅니다!");
                    }

                    BattleEventSystem.CallHpChanged(true, currentPlayerStats.currentHp, currentPlayerStats.maxHp);

                    // 부활 직후에도 특수 스탯 UI 갱신!
                    RefreshSpecialStatsProgressUI();
                    return false; // 안 죽었으므로 여기서 함수 종료
                }
            }

            // 부활 기믹이 안 터졌다면 정상적으로 데미지 적용
            currentPlayerStats.currentHp = Mathf.Max(0, hpAfterDamage);
            BattleEventSystem.CallHpChanged(true, currentPlayerStats.currentHp, currentPlayerStats.maxHp);

            isDead = currentPlayerStats.currentHp <= 0; // 결과 저장
        }
        else
        {
            // 적군 데미지 처리 (기존 동일)
            currentEnemyHp = Mathf.Max(0, currentEnemyHp - damage);
            currentEnemyData.currentHp = currentEnemyHp;
            currentEnemyData.aiBrain?.UpdatePassives(currentEnemyData);

            BattleEventSystem.CallHpChanged(false, currentEnemyHp, currentEnemyData.maxHp);

            isDead = currentEnemyHp <= 0; // 결과 저장
        }

        // [핵심] 리턴으로 빠져나가기 직전에 안전하게 UI를 갱신합니다!
        RefreshSpecialStatsProgressUI();

        return isDead; // 최종 결과 반환
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
                        float baseHeal = currentPlayerStats.maxHp * hpRegenRate;
                        // [수정] 재생 효과에도 회복 증폭 효율이 똑같이 적용됩니다!
                        int healAmount = Mathf.RoundToInt(baseHeal * (1f + currentPlayerStats.healingReceivedAmp));
                        int excessHeal = (currentPlayerStats.currentHp + healAmount) - currentPlayerStats.maxHp;

                        currentPlayerStats.currentHp = Mathf.Clamp(currentPlayerStats.currentHp + healAmount, 0, currentPlayerStats.maxHp);
                        CombatUIManager.Instance.playerStatusUI.UpdateHP(currentPlayerStats.currentHp, currentPlayerStats.maxHp);
                        CombatUIManager.Instance.SpawnDamageText($"<color=#00FF00>+{healAmount}</color>", false, true);
                        DevLog.Log($"[재생] 턴 종료! 셰리의 체력이 {healAmount} 회복되었습니다.");

                        // [신규] 재생으로 넘친 체력도 피의 폭주를 발동시킵니다!
                        if (excessHeal > 0) ApplyOverhealBuff(excessHeal);
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

            if (currentActiveEntity.isPlayer) BuffManager.Instance.AdvanceTurnActiveEffects(true);
            else if (currentActiveEntity.type == EntityType.Enemy) BuffManager.Instance.AdvanceTurnActiveEffects(false);

            //  캐스터 시너지: 매 턴 종료 시 무작위 독립 버프 부여
            if (currentActiveEntity.isPlayer && PlayerManager.Instance != null)
            {
                var syn = PlayerManager.Instance.GetCurrentSynergies();
                var inventory = PlayerManager.Instance.inventory;

                // [캐스터 4점] 매 턴 스탯 5% 버프 1개
                if (syn.GetValueOrDefault(ItemClass.Caster) >= 4) ApplyRandomCasterStatBuff(0.05f);

                // [캐스터 희귀] 수치를 모두 더한 뒤 1개의 버프만 생성
                var casterRares = inventory.FindAll(x => x.data.itemClass == ItemClass.Caster && x.data.grade == ItemGrade.Rare);
                float casterRareVal = 0f;
                foreach (var casterRare in casterRares)
                    casterRareVal += casterRare.starLevel == 1 ? 0.02f : (casterRare.starLevel == 2 ? 0.08f : 0.30f);

                if (casterRareVal > 0f) ApplyRandomCasterStatBuff(casterRareVal);

                // [캐스터 에픽] 수치를 모두 더한 뒤 1개의 버프만 생성
                var casterEpics = inventory.FindAll(x => x.data.itemClass == ItemClass.Caster && x.data.grade == ItemGrade.Epic);
                float casterEpicVal = 0f;
                foreach (var casterEpic in casterEpics)
                    casterEpicVal += casterEpic.starLevel == 1 ? 0.02f : (casterEpic.starLevel == 2 ? 0.08f : 0.30f);

                if (casterEpicVal > 0f) ApplyRandomCasterEpicBuff(casterEpicVal);
            }
        }

        CalculateNextTurn();
    }

    private void ApplyRandomCasterStatBuff(float value)
    {
        int rand = Random.Range(0, 4);
        TargetStat target = TargetStat.Strength;
        string statName = "힘";

        if (rand == 1) { target = TargetStat.Defense; statName = "방어력"; }
        else if (rand == 2) { target = TargetStat.Speed; statName = "속도"; }
        else if (rand == 3) { target = TargetStat.Luck; statName = "운"; }

        // CreateInstance를 사용해 메모리에 완전히 고유한 버프 인스턴스를 찍어냅니다. (서로 덮어쓰지 않음!)
        StatusEffectData newBuff = ScriptableObject.CreateInstance<StatusEffectData>();
        newBuff.category = EffectCategory.Buff;
        newBuff.targetStat = target;
        newBuff.modifierType = ModifierType.Percentage;
        newBuff.effectName = $"마력 순환({statName})";

        BuffManager.Instance.AddEffect(true, newBuff, value, 1); // 1턴 유지
        DevLog.Log($"[캐스터 스탯 버프] 셰리에게 {statName} {value * 100}% 증가 버프가 독립 부여되었습니다.");
    }

    private void ApplyRandomCasterEpicBuff(float value)
    {
        int rand = Random.Range(0, 5);
        SpecialEffectType specialType = SpecialEffectType.DamageGivenAmp;
        string buffName = "피해 증폭";
        float applyValue = value;

        if (rand == 0) { specialType = SpecialEffectType.CritRateUp; buffName = "크리티컬 확률"; applyValue = value * 100f; } // 확률은 합산 연산이라 100을 곱함
        else if (rand == 1) { specialType = SpecialEffectType.CritDamageUp; buffName = "크리티컬 피해량"; }
        else if (rand == 2) { specialType = SpecialEffectType.EvasionUp; buffName = "회피율"; applyValue = value * 100f; }
        else if (rand == 3) { specialType = SpecialEffectType.AccuracyUp; buffName = "명중률"; applyValue = value * 100f; }
        else if (rand == 4) { specialType = SpecialEffectType.DamageGivenAmp; buffName = "주는 피해 증폭"; }

        StatusEffectData newBuff = ScriptableObject.CreateInstance<StatusEffectData>();
        newBuff.category = EffectCategory.Buff;
        newBuff.specialType = specialType;
        newBuff.effectName = $"마력 공명({buffName})";

        BuffManager.Instance.AddEffect(true, newBuff, applyValue, 1);
        DevLog.Log($"[캐스터 에픽 버프] 셰리에게 {buffName} +{applyValue} 버프가 독립 부여되었습니다.");
    }

    private void ApplyRandomTricksterStatDebuff(float value)
    {
        int rand = Random.Range(0, 4);
        TargetStat target = TargetStat.Strength;
        string statName = "힘";

        if (rand == 1) { target = TargetStat.Defense; statName = "방어력"; }
        else if (rand == 2) { target = TargetStat.Speed; statName = "속도"; }
        else if (rand == 3) { target = TargetStat.Luck; statName = "운"; }

        StatusEffectData newDebuff = ScriptableObject.CreateInstance<StatusEffectData>();
        newDebuff.category = EffectCategory.Debuff;
        newDebuff.targetStat = target;
        newDebuff.modifierType = ModifierType.Percentage;
        newDebuff.effectName = $"악의적 간섭({statName})";

        BuffManager.Instance.AddEffect(false, newDebuff, -value, 1); // 감소이므로 -value를 전달
        DevLog.Log($"[트릭스터] 적에게 {statName} {value * 100}% 감소 디버프 부여!");
    }

    private void ApplyRandomTricksterEpicDebuff(float statVal, float bleedVal, float burnVal)
    {
        int rand = Random.Range(0, 5);
        SpecialEffectType specialType = SpecialEffectType.EvasionUp;
        string debuffName = "회피율 감소";
        float applyValue = -statVal * 100f; // 명중/회피는 상수로 -20 등의 수치 사용

        if (rand == 0) { specialType = SpecialEffectType.EvasionUp; debuffName = "회피율 감소"; applyValue = -statVal * 100f; }
        else if (rand == 1) { specialType = SpecialEffectType.DamageAmp; debuffName = "받는 피해 증가"; applyValue = statVal; } // Amp는 양수일 때 데미지 증가
        else if (rand == 2) { specialType = SpecialEffectType.AccuracyUp; debuffName = "명중률 감소"; applyValue = -statVal * 100f; }
        else if (rand == 3) { specialType = SpecialEffectType.Bleed; debuffName = "심연의 출혈"; applyValue = bleedVal; } // 출혈은 양수 배율
        else if (rand == 4) { specialType = SpecialEffectType.Burn; debuffName = "지옥의 화상"; applyValue = burnVal; } // 화상도 양수 배율

        StatusEffectData newDebuff = ScriptableObject.CreateInstance<StatusEffectData>();
        newDebuff.category = EffectCategory.Debuff;
        newDebuff.specialType = specialType;
        newDebuff.effectName = $"기괴한 가면({debuffName})";

        BuffManager.Instance.AddEffect(false, newDebuff, applyValue, 1);
        DevLog.Log($"[트릭스터 에픽] 적에게 {debuffName} (수치:{applyValue}) 부여!");
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
                    CombatUIManager.Instance.SpawnDamageText("★" + selfDamage.ToString(), false, true);
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
                    CombatUIManager.Instance.SpawnDamageText("★" + extraDmg.ToString(), false, false);
                    BattleEventSystem.CallHpChanged(false, currentEnemyHp, currentEnemyData.maxHp);

                    currentState.accumulatedDamage = 0; // 초기화
                    yield return new WaitForSeconds(1.0f);
                    CombatUIManager.Instance.ResetDefenderImage(false);
                }
            }
        }
    }

    // [신규] 데몬 6점 및 전설 - 초과 회복(Over-heal) 비례 버프 발생기
    // =======================================================
    public void ApplyOverhealBuff(int excessHeal)
    {
        if (PlayerManager.Instance == null) return;
        var syn = PlayerManager.Instance.GetCurrentSynergies();
        var inventory = PlayerManager.Instance.inventory;

        bool has6Point = syn.GetValueOrDefault(ItemClass.Demon) >= 6;
        bool hasLegendary = inventory.Exists(x => x.data.itemClass == ItemClass.Demon && x.data.grade == ItemGrade.Legendary);

        if (!has6Point && !hasLegendary) return;

        // 배율 산출: 기획안에 따라 최대 체력 비례 %당 1% (6점) + 0.5% (전설)
        float multiplier = 0f;
        if (has6Point) multiplier += 1.0f;
        if (hasLegendary) multiplier += 0.5f;

        // 공식: (초과 회복량 / 최대 체력) * 배율
        // 예: 1000 체력 중 200 초과 회복 시 -> 0.2 * 1.5 = 0.3f (30% 증폭)
        float ampValue = ((float)excessHeal / currentPlayerStats.maxHp) * multiplier;

        if (ampValue > 0f)
        {
            StatusEffectData newBuff = ScriptableObject.CreateInstance<StatusEffectData>();
            newBuff.category = EffectCategory.Buff;
            newBuff.specialType = SpecialEffectType.DamageGivenAmp;
            newBuff.effectName = "피의 폭주";

            BuffManager.Instance.AddEffect(true, newBuff, ampValue, 1);
            DevLog.Log($"[피의 폭주] 초과 회복 {excessHeal} 달성 -> 피해 증폭 {ampValue * 100:F1}% 버프 1턴 획득!");
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

    public void HealEntity(bool isPlayerTarget, int amount)
    {
        if (isPlayerTarget)
        {
            currentPlayerStats.currentHp = Mathf.Clamp(currentPlayerStats.currentHp + amount, 0, currentPlayerStats.maxHp);
            BattleEventSystem.CallHpChanged(true, currentPlayerStats.currentHp, currentPlayerStats.maxHp);
            if (CombatUIManager.Instance != null)
                CombatUIManager.Instance.playerStatusUI.UpdateHP(currentPlayerStats.currentHp, currentPlayerStats.maxHp);
        }
        else
        {
            currentEnemyHp = Mathf.Clamp(currentEnemyHp + amount, 0, currentEnemyData.maxHp);
            currentEnemyData.currentHp = currentEnemyHp;

            // 회복된 체력에 맞춰서 패시브(피해 증폭률)도 실시간 리셋 연산
            currentEnemyData.aiBrain?.UpdatePassives(currentEnemyData);

            BattleEventSystem.CallHpChanged(false, currentEnemyHp, currentEnemyData.maxHp);
            if (CombatUIManager.Instance != null)
                CombatUIManager.Instance.enemyStatusUI.UpdateHP(currentEnemyHp, currentEnemyData.maxHp);
        }

        RefreshSpecialStatsProgressUI();
    }

    public void RestoreDefenderImage(bool isPlayerTarget)
    {
        // 1. 대상이 그로기 상태인지 확인
        bool isBroken = BreakManager.Instance.IsBroken(isPlayerTarget);

        if (isBroken)
        {
            // 2. 그로기 상태라면 그로기 이미지로 복구
            Sprite breakSprite = isPlayerTarget ? playerData?.breakImage : currentEnemyData?.breakImage;
            if (breakSprite != null)
                CombatUIManager.Instance.SetDefenderImage(isPlayerTarget, breakSprite);
            DevLog.Log($"[이미지 복구] {(isPlayerTarget ? "주인공" : "적")}이 그로기 상태이므로 그로기 이미지를 유지합니다.");
        }
        else
        {
            // 3. 그로기 상태가 아니면 일반 이미지로 복구
            CombatUIManager.Instance.ResetDefenderImage(isPlayerTarget);
            DevLog.Log($"[이미지 복구] 일반 상태로 이미지를 복구합니다.");
        }
    }

    public void ToggleAnalysis()
    {
        // 플레이어 턴(스킬 선택 중)일 때만 오픈
        if (!IsPlayerSelectingPhase) return;

        if (analysisUI.gameObject.activeSelf) analysisUI.Close();
        else analysisUI.Open(currentEnemyData);
    }
}