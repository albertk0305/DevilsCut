using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

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
    public bool hasTriggeredEnemyCounterThisSkill = false;
    public int totalExcessHealThisSkill = 0;

    public bool hasResurrected = false;
    public bool currentTurnDeathGuardActive = false;
    public int currentTurnDeathGuardMinHp = 0;
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

    [Header("아이템 시너지 StatusEffectData 매핑")]
    [SerializeField] private StatusEffectData demonOverhealDamageAmpEffect;

    [Header("턴 효과 StatusEffectData 매핑")]
    [SerializeField] private TurnEffectResolverConfig turnEffectConfig;

    [SerializeField] private CombatDefeatUIController defeatUIController;
    [SerializeField] private CombatVictoryUIController victoryUIController;
    [SerializeField] private InfiniteBattleResultUIController infiniteBattleResultUI;

    private PlayerStats currentPlayerStats;
    public PlayerStats GetCurrentPlayerStats() => currentPlayerStats;

    private EnemyData currentEnemyData;
    public EnemyData GetCurrentEnemyData() => currentEnemyData;
    private int battleStartPlayerHp;

    private bool combatEnded;
    public bool IsCombatEnded => combatEnded;

    private int currentEnemyHp;
    private int enemyTurnCount = 0;
    private int playerHpAtTurnStart;
    private int enemyHpAtTurnStart;
    private TurnEntity currentActiveEntity;
    private CombatActionMenuController actionMenuController;
    private CombatPresentationDirector presentationDirector;
    private DamageResolutionService damageResolutionService;
    private TurnEffectResolver turnEffectResolver;
    private readonly Queue<SkillData> pendingEnemySkillSequence = new Queue<SkillData>();
    [SerializeField] private CombatTimingSettings timing = new CombatTimingSettings();
    public CombatState currentState = new CombatState();

    public CombatTimingSettings Timing => timing;

    public bool CanInteractWithCombatUI =>
    !combatEnded &&
    currentActiveEntity != null &&
    currentActiveEntity.type == EntityType.Player &&
    actionMenuController != null &&
    actionMenuController.IsPlayerSelectingPhase;

    private struct SkillCalculationContext
    {
        public int atkStr;
        public int atkDef;
        public int atkLck;
        public int atkSpd;

        public int defDef;
        public int defSpd;
        public int defBR;
        public int defCurrentHp;
        public int defMaxHp;
    }

    private struct SkillPresentationContext
    {
        public bool isPlayerDefending;
        public bool isPureUtility;
        public string attackerName;
        public string skillName;
        public string commentary;
        public string commentaryKey;
        public string commentaryFallback;
        public object[] commentaryArgs;
    }

    private struct SkillCastPresentationContext
    {
        public SkillData skill;
        public bool isPlayerAttacking;
        public SkillResult skillResult;
        public string commentary;
        public string commentaryKey;
        public string commentaryFallback;
        public object[] commentaryArgs;
        public bool isPureUtility;
        public Sprite reactionSprite;
        public bool showCritAlert;
    }

    private struct SkillExecutionContext
    {
        public SkillData skill;
        public bool isPlayerAttacking;
        public bool isUltimate;
        public SkillCalculationContext calculation;
        public SkillResult result;
        public SkillPresentationContext presentation;
    }

    public bool IsPlayerSelectingPhase
    {
        get
        {
            EnsureActionMenuController();
            return actionMenuController != null && actionMenuController.IsPlayerSelectingPhase;
        }
    }

    private void EnsureActionMenuController()
    {
        if (actionMenuController != null) return;

        if (CombatUIManager.Instance == null)
        {
            DevLog.Log("[CombatManager] CombatUIManager.Instance가 없어 CombatActionMenuController를 초기화할 수 없습니다.");
            return;
        }

        actionMenuController = new CombatActionMenuController(
            CombatUIManager.Instance,
            analysisUI,
            () => currentEnemyData,
            ExecuteSkillFromActionMenu
        );
    }

    public void ToggleAnalysis()
    {
        EnsureActionMenuController();
        actionMenuController?.ToggleAnalysis();
    }

    private void EnsurePresentationDirector()
    {
        if (presentationDirector != null) return;

        if (CombatUIManager.Instance == null || BattleVisualizer.Instance == null)
        {
            DevLog.Log("[CombatManager] CombatPresentationDirector를 초기화할 수 없습니다.");
            return;
        }

        presentationDirector = new CombatPresentationDirector(
            CombatUIManager.Instance,
            BattleVisualizer.Instance
        );
    }

    private DamageResolutionService DamageResolver
    {
        get
        {
            if (damageResolutionService == null)
                damageResolutionService = new DamageResolutionService(RefreshSpecialStatsProgressUI);

            return damageResolutionService;
        }
    }

    public DamageResolutionResult LastDamageResolutionResult => DamageResolver.LastResult;

    private TurnEffectResolver TurnEffects
    {
        get
        {
            if (turnEffectResolver == null)
                turnEffectResolver = new TurnEffectResolver(turnEffectConfig);

            return turnEffectResolver;
        }
    }

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

        yield return CombatUIManager.Instance.TypeLocalizedCommentary("combat_comment_encounter_format", "{0} 조우!", new object[] { eName }, true, timing.encounterCommentDelay);

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
            battleStartPlayerHp = currentPlayerStats.currentHp;
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
        currentEnemyData?.aiBrain?.UpdatePassives(currentEnemyData);
        StyleRankManager.Instance?.InitCombat();

        EnsureActionMenuController();
        actionMenuController?.HideActionMenuAndShowWaiting();

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

        EnsurePresentationDirector();
        presentationDirector?.UpdateTurnOrder(icons);
    }
    // ==========================================================
    // 1. 메인 턴 분배기 (Switch 문으로 가독성 극대화)
    // ==========================================================
    private IEnumerator ProcessTurnRoutine(TurnEntity currentTurnOwner)
    {
        ClearCurrentTurnDeathGuard();
        ClearPendingEnemySkillSequence();
        currentActiveEntity = currentTurnOwner;
        playerHpAtTurnStart = currentPlayerStats.currentHp;
        enemyHpAtTurnStart = currentEnemyHp;

        RefreshSpecialStatsProgressUI();

        EnsureActionMenuController();
        actionMenuController?.HideActionMenuAndShowWaiting();

        yield return HandlePreTurnEffects(currentTurnOwner);

        if (currentEnemyHp <= 0 || currentPlayerStats.currentHp <= 0) yield break;

        bool turnSkipped = false;
        yield return TryConsumeStunTurn(currentTurnOwner, skipped => turnSkipped = skipped);
        if (turnSkipped) yield break;

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

    private IEnumerator TryConsumeStunTurn(TurnEntity owner, System.Action<bool> onComplete)
    {
        onComplete?.Invoke(false);

        if (owner == null || BuffManager.Instance == null)
            yield break;

        if (owner.type != EntityType.Enemy && owner.type != EntityType.Player)
            yield break;

        bool isPlayerTarget = owner.type == EntityType.Player;
        var effects = BuffManager.Instance.GetEffects(isPlayerTarget);
        LogTurnStartEffects(owner.type, effects);

        var stunEffect = effects.Find(e => e.effectData != null && e.effectData.specialType == SpecialEffectType.Stun);
        bool stunFound = stunEffect != null;
        DevLog.Log($"[Stun Debug] {owner.type} turn start. stunFound={stunFound}");

        if (!stunFound)
            yield break;

        effects.Remove(stunEffect);
        if (CombatUIManager.Instance != null) CombatUIManager.Instance.RefreshBuffUI();

        string targetName = isPlayerTarget
            ? (playerData != null ? GetTranslatedText(playerData.playerNamekey) : "주인공")
            : (currentEnemyData != null ? GetTranslatedText(currentEnemyData.enemyNameKey) : "적");

        string commentaryKey = isPlayerTarget ? "combat_comment_stun_skip_format" : "combat_comment_infinite_void_skip_format";
        string commentaryFallback = isPlayerTarget
            ? "{0:은는} 스턴 효과로 행동할 수 없습니다!"
            : "{0:은는} 무량공처의 효과로 행동할 수 없습니다!";

        if (CombatUIManager.Instance != null)
            yield return CombatUIManager.Instance.TypeLocalizedCommentary(commentaryKey, commentaryFallback, new object[] { targetName }, true, timing.turnSkipCommentDelay);

        ResolveTurnEnd();
        onComplete?.Invoke(true);
    }

    private void LogTurnStartEffects(EntityType ownerType, List<BuffManager.ActiveEffect> effects)
    {
        string sideName = ownerType == EntityType.Player ? "Player" : "Enemy";
        int count = effects != null ? effects.Count : 0;
        DevLog.Log($"[Stun Debug] {sideName} turn start effects. count={count}");

        if (effects == null) return;

        for (int i = 0; i < effects.Count; i++)
        {
            var effect = effects[i];
            StatusEffectData data = effect.effectData;
            string dataName = data != null ? data.name : "null";
            string effectName = data != null ? data.effectName : "null";
            string specialType = data != null ? data.specialType.ToString() : "null";

            DevLog.Log($"[Stun Debug] {sideName} effect[{i}] dataName={dataName}, effectName={effectName}, specialType={specialType}, turnsLeft={effect.turnsLeft}, isNewlyApplied={effect.isNewlyApplied}");
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
            TurnEffects.ApplyTricksterPreTurnEffects(PlayerManager.Instance);

            var playerEffects = BuffManager.Instance.GetEffects(true);
            var bleedEffects = playerEffects.FindAll(e => e.effectData != null && e.effectData.specialType == SpecialEffectType.Bleed);

            if (bleedEffects.Count > 0)
            {
                float totalBleedMultiplier = 0f;
                foreach (var bleedEffect in bleedEffects)
                {
                    totalBleedMultiplier += bleedEffect.value;
                }

                int enemyStr = currentEnemyData != null ? currentEnemyData.strength : 0;
                if (StatManager.Instance != null)
                {
                    enemyStr = StatManager.Instance.GetEffectiveStat(false, TargetStat.Strength);
                }

                int bleedDamage = Mathf.Max(1, Mathf.RoundToInt(enemyStr * totalBleedMultiplier));
                ApplyDamageToEntity(true, bleedDamage);
                PlayNormalHitSfxForResolvedDamage(bleedDamage);

                CombatUIManager.Instance.SetDefenderImage(true, playerData.hit);
                if (!ShouldSuppressDamageText(true))
                    CombatUIManager.Instance.SpawnDamageText("★" + bleedDamage.ToString(), false, true);

                yield return CombatUIManager.Instance.TypeLocalizedCommentary("combat_comment_player_bleed_dot_format", "셰리가 {0}의 출혈 지속 피해를 입습니다.", new object[] { bleedDamage }, true, timing.dotCommentDelay);

                yield return new WaitForSeconds(timing.dotHitHold);
                CombatUIManager.Instance.ResetDefenderImage(true);

                if (CheckAndHandleBattleEnd())
                    yield break;
            }
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
                PlayNormalHitSfxForResolvedDamage(bleedDmg);

                CombatUIManager.Instance.SetDefenderImage(false, currentEnemyData.hit);
                CombatUIManager.Instance.SpawnDamageText("★" + bleedDmg.ToString(), false, false);

                yield return CombatUIManager.Instance.TypeLocalizedCommentary("combat_comment_enemy_bleed_dot_format", "심연의 출혈! {0:이가} {1}의 지속 피해를 입습니다.", new object[] { eName, bleedDmg }, true, timing.dotCommentDelay);

                yield return new WaitForSeconds(timing.dotHitHold);
                CombatUIManager.Instance.ResetDefenderImage(false);

                if (CheckAndHandleBattleEnd())
                    yield break;
            }

            if (burnEffect != null)
            {
                // 최대 체력에 비례한 고정 피해
                int burnDmg = Mathf.Max(1, Mathf.RoundToInt(currentEnemyData.maxHp * burnEffect.value));
                ApplyDamageToEntity(false, burnDmg);
                PlayNormalHitSfxForResolvedDamage(burnDmg);

                CombatUIManager.Instance.SetDefenderImage(false, currentEnemyData.hit);
                CombatUIManager.Instance.SpawnDamageText("★" + burnDmg.ToString(), false, false);

                yield return CombatUIManager.Instance.TypeLocalizedCommentary("combat_comment_enemy_burn_dot_format", "지옥의 플람베! {0:이가} {1}의 화상 피해를 입습니다.", new object[] { eName, burnDmg }, true, timing.dotCommentDelay);

                yield return new WaitForSeconds(timing.dotHitHold);
                CombatUIManager.Instance.ResetDefenderImage(false);

                if (CheckAndHandleBattleEnd())
                    yield break;
            }

            if (currentState.isBombActive)
            {
                currentState.isBombActive = false;
                enemyEffects.RemoveAll(e => e.effectData != null && e.effectData.specialType == SpecialEffectType.TimeBomb);
                CombatUIManager.Instance.RefreshBuffUI();

                CombatUIManager.Instance.SetDefenderImage(false, currentEnemyData.hit);

                yield return CombatUIManager.Instance.TypeLocalizedCommentary("combat_comment_last_train_home", "라스트 트레인 홈 발동!!", null, true, timing.dotCommentDelay);

                ApplyDamageToEntity(false, currentState.savedBombDamage);
                PlayNormalHitSfxForResolvedDamage(currentState.savedBombDamage);
                CombatUIManager.Instance.SpawnDamageText("★" + currentState.savedBombDamage.ToString(), false, false);
                DevLog.Log($"[라스트 트레인 홈] 적에게 {currentState.savedBombDamage}의 확정 피해를 입힙니다!");

                yield return new WaitForSeconds(timing.dotHitHold);
                CombatUIManager.Instance.ResetDefenderImage(false);

                if (CheckAndHandleBattleEnd())
                    yield break;
            }
        }
    }

    // ==========================================================
    // 3. 적(Enemy) 턴 로직
    // ==========================================================
    private IEnumerator HandleEnemyTurn()
    {
        string eName = currentEnemyData != null ? GetTranslatedText(currentEnemyData.enemyNameKey) : "적";

        if (BreakManager.Instance.IsBroken(false))
        {
            yield return CombatUIManager.Instance.TypeLocalizedCommentary("combat_comment_break_recover_format", "{0:이가} 그로기 상태에서 정신을 차렸습니다.", new object[] { eName });
            BreakManager.Instance.WakeUpFromBreak(false);
            CombatUIManager.Instance.ResetDefenderImage(false);
            ResolveTurnEnd();
            yield break;
        }

        yield return CombatUIManager.Instance.TypeLocalizedCommentary("combat_comment_enemy_turn_format", "{0}의 차례입니다!", new object[] { eName }, true, timing.enemyTurnCommentDelay);
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
            yield return CombatUIManager.Instance.TypeLocalizedCommentary("combat_comment_break_recover_format", "{0:이가} 그로기 상태에서 정신을 차렸습니다.", new object[] { pName });
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
            yield return CombatUIManager.Instance.TypeLocalizedCommentary("combat_comment_charge_release_format", "{0:이가} 모아둔 기를 방출합니다!", new object[] { pName }, true, 1.0f);
            DevLog.Log("[원기옥] 모은 기를 발사합니다!");
            PerformSkillRoutine(currentState.chargingSkill, true);
        }
        else
        {
            CombatUIManager.Instance.SetWaitingPanelActive(false);
            ShowCategoryMenu();
            yield return CombatUIManager.Instance.TypeLocalizedCommentary("combat_comment_select_skill_prompt", "사용할 스킬을 선택해주세요.", null, false);
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
        yield return new WaitForSeconds(timing.enemyIntentDelay);

        EnemyActionIntent intent = null;

        // 1. AI 뇌(Brain)에게 이번 턴의 '행동 계획서'를 결재받습니다.
        if (currentEnemyData?.aiBrain != null)
        {
            intent = currentEnemyData.aiBrain.DecideNextAction(enemyTurnCount, currentPlayerStats, currentEnemyData);
            enemyTurnCount++;
        }

        SkillData firstSkill = PrepareEnemySkillSequence(intent);

        // 2. 계획서에 스킬이 정상적으로 들어있다면 실행합니다. (미카엘의 모든 행동)
        if (firstSkill != null)
        {
            PerformSkillRoutine(firstSkill, false, firstSkill.isUltimate);
        }
        else
        {
            // AI가 없거나 깡통인 경우, 혹은 쉴 때 (대기)
            ResolveTurnEnd();
        }
    }

    private SkillData PrepareEnemySkillSequence(EnemyActionIntent intent)
    {
        ClearPendingEnemySkillSequence();

        if (intent == null)
            return null;

        List<SkillData> validSequence = new List<SkillData>();

        if (intent.skillSequence != null && intent.skillSequence.Count > 0)
        {
            for (int i = 0; i < intent.skillSequence.Count; i++)
            {
                SkillData skill = intent.skillSequence[i];
                if (skill == null)
                {
                    DevLog.LogWarning($"[EnemySkillSequence] Null skill skipped at index {i}.");
                    continue;
                }

                validSequence.Add(skill);
            }
        }

        if (validSequence.Count == 0 && intent.skillToUse != null)
            validSequence.Add(intent.skillToUse);

        if (validSequence.Count == 0)
            return null;

        for (int i = 1; i < validSequence.Count; i++)
            pendingEnemySkillSequence.Enqueue(validSequence[i]);

        return validSequence[0];
    }

    private void ClearPendingEnemySkillSequence()
    {
        pendingEnemySkillSequence.Clear();
    }

    public void ShowCategoryMenu()
    {
        EnsureActionMenuController();
        actionMenuController?.ShowCategoryMenu();
    }

    public void ShowSkillMenu(int categoryIndex)
    {
        EnsureActionMenuController();
        actionMenuController?.ShowSkillMenu(categoryIndex);
    }

    public void OnActionSlotClicked(int slotIndex)
    {
        EnsureActionMenuController();
        actionMenuController?.OnActionSlotClicked(slotIndex);
    }

    private void ExecuteSkillFromActionMenu(SkillData skill, bool isPlayerAttacking, bool isUltimate = false)
    {
        PerformSkillRoutine(skill, isPlayerAttacking, isUltimate);
    }

    // 스킬 처리 프로세스 (연산 -> 큐 적재 -> 실행)
    private void PerformSkillRoutine(SkillData skill, bool isPlayerAttacking, bool isUltimate = false)
    {
        if (analysisUI != null) analysisUI.Close();

        // 1. 상태 스냅샷 및 초기화
        ResetSkillExecutionState();

        //  [복구됨] 기(Ki) 차지(원기옥) 시작 판정
        if (TryBeginGiCharge(skill, isPlayerAttacking))
            return;

        //  [복구됨] 실시간 스탯 산출 및 BattleCalculator 연산 (skillResult 생성)
        SkillExecutionContext executionContext = BuildSkillExecutionContext(
            skill,
            isPlayerAttacking,
            isUltimate);

        EnqueueSkillExecutionSequence(executionContext);

        // 3. 지휘관 권한 위임 및 턴 종료 대기
        BattleVisualizer.Instance.StartSequence(() => CompleteSkillSequence(isPlayerAttacking));
    }

    private void ResetSkillExecutionState()
    {
        currentState.wasEnemyBrokenAtSkillStart = BreakManager.Instance.IsBroken(false);
        currentState.hasRewardedCritThisSkill = false;
        currentState.isMorningStarApRecoveredThisSkill = false;
        currentState.hasTriggeredEnemyCounterThisSkill = false;
        currentState.totalExcessHealThisSkill = 0;
    }

    private bool TryBeginGiCharge(SkillData skill, bool isPlayerAttacking)
    {
        if (skill == null) return false;
        if (!(skill.skillLogic is IChargeSkillLogic chargeLogic)) return false;
        if (!chargeLogic.ShouldBeginCharge(
            skill,
            isPlayerAttacking,
            currentState.isPlayerCharging,
            currentState.isUnleashingCharge)) return false;

        currentState.isPlayerCharging = true;
        currentState.chargingSkill = skill;

        if (skill.skillActionImage != null)
            CombatUIManager.Instance.SetCasterImage(true, skill.skillActionImage);

        string pName = playerData != null
            ? GetTranslatedText(playerData.playerNamekey)
            : "주인공";

        StartCoroutine(CombatUIManager.Instance.TypeLocalizedCommentary("combat_comment_charge_start_format", "{0:이가} 기를 모으기 시작합니다!", new object[] { pName }));
        ResolveTurnEnd();

        return true;
    }

    private void EnqueueSkillExecutionSequence(SkillExecutionContext context)
    {
        EnqueueUltimateCutInIfNeeded(context);

        // Cast presentation
        SkillData skill = context.skill;
        bool isPlayerAttacking = context.isPlayerAttacking;
        SkillResult castResult = context.result;
        string castCommentary = context.presentation.commentary;
        string castCommentaryKey = context.presentation.commentaryKey;
        string castCommentaryFallback = context.presentation.commentaryFallback;
        object[] castCommentaryArgs = context.presentation.commentaryArgs;
        bool castIsPureUtility = context.presentation.isPureUtility;
        BattleVisualizer.Instance.EnqueueAction(() => ApplySkillCastUI(skill, isPlayerAttacking, castResult, castCommentary, castCommentaryKey, castCommentaryFallback, castCommentaryArgs, castIsPureUtility));

        // Immediate defense outcome
        ApplyImmediateDefenseOutcome(context);

        // Hit actions
        EnqueueSkillHitActions(context);
        UpdateLastSuccessfulHits(context);

        // On-hit skill effect
        EnqueueApplyEffectOnHit(context);

        // Enemy skill damage counter
        EnqueueEnemyCounterIfNeeded(context);

        // Counter
        EnqueueMorningStarCounterIfNeeded(context);

        // Guard and reflect
        EnqueueGuardAndReflectIfNeeded(context);

        // Reset
        EnqueueSkillReset(context);
    }

    private SkillExecutionContext BuildSkillExecutionContext(
        SkillData skill,
        bool isPlayerAttacking,
        bool isUltimate)
    {
        SkillExecutionContext context = new SkillExecutionContext();
        context.skill = skill;
        context.isPlayerAttacking = isPlayerAttacking;
        context.isUltimate = isUltimate;
        context.calculation = BuildSkillCalculationContext(context.isPlayerAttacking);
        context.result = CalculateSkillResult(
            context.skill,
            context.isPlayerAttacking,
            context.calculation);
        context.presentation = BuildSkillPresentationContext(
            context.skill,
            context.isPlayerAttacking,
            context.result);

        return context;
    }

    private SkillResult CalculateSkillResult(
        SkillData skill,
        bool isPlayerAttacking,
        SkillCalculationContext context)
    {
        return BattleCalculator.CalculateSkill(
            skill,
            isPlayerAttacking,
            currentPlayerStats,
            currentEnemyData,
            context.atkStr,
            context.atkDef,
            context.atkLck,
            context.atkSpd,
            context.defDef,
            context.defSpd,
            context.defBR,
            context.defCurrentHp,
            context.defMaxHp
        );
    }

    private SkillCalculationContext BuildSkillCalculationContext(bool isPlayerAttacking)
    {
        return new SkillCalculationContext
        {
            atkStr = StatManager.Instance.GetEffectiveStat(isPlayerAttacking, TargetStat.Strength),
            atkDef = StatManager.Instance.GetEffectiveStat(isPlayerAttacking, TargetStat.Defense),
            atkLck = StatManager.Instance.GetEffectiveStat(isPlayerAttacking, TargetStat.Luck),
            atkSpd = StatManager.Instance.GetEffectiveStat(isPlayerAttacking, TargetStat.Speed),

            defDef = StatManager.Instance.GetEffectiveStat(!isPlayerAttacking, TargetStat.Defense),
            defSpd = StatManager.Instance.GetEffectiveStat(!isPlayerAttacking, TargetStat.Speed),
            defBR = StatManager.Instance.GetEffectiveStat(!isPlayerAttacking, TargetStat.BreakResistance),
            defCurrentHp = isPlayerAttacking ? currentEnemyHp : currentPlayerStats.currentHp,
            defMaxHp = isPlayerAttacking ? currentEnemyData.maxHp : currentPlayerStats.maxHp
        };
    }

    private SkillPresentationContext BuildSkillPresentationContext(
    SkillData skill,
    bool isPlayerAttacking,
    SkillResult skillResult)
    {
        float baseMultForUI = skill.GetCurrentDamageMultiplier();
        float logicMultForUI = skill.skillLogic != null
            ? skill.skillLogic.GetDamageMultiplier(skill, currentPlayerStats, currentEnemyData, isPlayerAttacking)
            : 1f;

        bool isAttackForUI =
            baseMultForUI > 0f ||
            (baseMultForUI <= 0f && logicMultForUI > 0f && logicMultForUI != 1.0f);

        bool isPureUtility = !isAttackForUI && !skill.forceHitReaction;
        bool isPlayerDefending = !isPlayerAttacking;

        string attackerName = isPlayerAttacking
            ? (playerData != null ? GetTranslatedText(playerData.playerNamekey) : "주인공")
            : (currentEnemyData != null ? GetTranslatedText(currentEnemyData.enemyNameKey) : "적");

        string skillName = GetTranslatedText(skill.skillNameKey);

        EnsurePresentationDirector();

        string commentaryKey;
        string commentaryFallback;
        ResolveSkillCommentaryDescriptor(skillResult, isPureUtility, out commentaryKey, out commentaryFallback);
        object[] commentaryArgs = new object[] { attackerName, skillName };

        string commentary = presentationDirector != null
            ? presentationDirector.BuildSkillCommentary(attackerName, skillName, skillResult, isPureUtility)
            : BuildSkillCommentaryFallback(attackerName, skillName, skillResult, isPureUtility);

        return new SkillPresentationContext
        {
            isPlayerDefending = isPlayerDefending,
            isPureUtility = isPureUtility,
            attackerName = attackerName,
            skillName = skillName,
            commentary = commentary,
            commentaryKey = commentaryKey,
            commentaryFallback = commentaryFallback,
            commentaryArgs = commentaryArgs
        };
    }

    private void EnqueueUltimateCutInIfNeeded(SkillExecutionContext context)
    {
        if (!context.isUltimate) return;

        Sprite cutInSprite = context.isPlayerAttacking
            ? playerData?.cutIn
            : currentEnemyData?.CutIn;

        EnsurePresentationDirector();
        presentationDirector?.EnqueueUltimateCutIn(cutInSprite, context.presentation.attackerName);
    }

    private void EnqueueSkillHitActions(SkillExecutionContext context)
    {
        SkillData skill = context.skill;
        SkillResult skillResult = context.result;
        bool isPlayerAttacking = context.isPlayerAttacking;
        bool isPlayerDefending = context.presentation.isPlayerDefending;
        bool isPureUtility = context.presentation.isPureUtility;

        foreach (var hit in skillResult.hits)
        {
            BattleVisualizer.Instance.EnqueueAction(() =>
            {
                if (!hit.isHit)
                    ProcessMissAction(isPlayerAttacking, isPlayerDefending, isPureUtility, skillResult);
                else
                    ProcessHitAction(hit, isPlayerAttacking, isPlayerDefending, isPureUtility, skillResult, skill);
            });

            BattleVisualizer.Instance.EnqueueDelay(timing.hitInterval);
        }
    }

    private void UpdateLastSuccessfulHits(SkillExecutionContext context)
    {
        SkillResult skillResult = context.result;
        int successCount = 0;

        foreach (var hit in skillResult.hits)
        {
            if (hit.isHit)
                successCount++;
        }

        currentState.lastSuccessfulHits = successCount;
    }

    private void EnqueueApplyEffectOnHit(SkillExecutionContext context)
    {
        SkillData skill = context.skill;
        bool isPlayerAttacking = context.isPlayerAttacking;
        bool anyHit = context.result.anyHit;

        BattleVisualizer.Instance.EnqueueAction(() =>
            skill.skillLogic?.ApplyEffectOnHit(
                skill,
                currentPlayerStats,
                currentEnemyData,
                isPlayerAttacking,
                anyHit));
    }

    private void EnqueueSkillReset(SkillExecutionContext context)
    {
        bool isPlayerAttacking = context.isPlayerAttacking;
        bool isPlayerDefending = context.presentation.isPlayerDefending;
        bool isUltimate = context.isUltimate;
        SkillData skill = context.skill;

        BattleVisualizer.Instance.EnqueueAction(() =>
            ResetCombatUI(isPlayerAttacking, isPlayerDefending, isUltimate, skill));
    }

    private void ApplyImmediateDefenseOutcome(SkillExecutionContext context)
    {
        SkillResult skillResult = context.result;
        bool isPlayerDefending = context.presentation.isPlayerDefending;
        bool isPureUtility = context.presentation.isPureUtility;
        if (!isPlayerDefending || isPureUtility)
            return;

        bool isInvincible = BuffManager.Instance
            .GetEffects(true)
            .Exists(e => e.effectData.specialType == SpecialEffectType.Invincible);

        // 완전 회피
        if (!skillResult.anyHit)
        {
            StyleRankManager.Instance.OnEvade();

            var martialSkill = PlayerManager.Instance.unlockedSkills.Find(
                s => s.category == SkillCategory.Martial);

            if (martialSkill != null && martialSkill.skillLogic is IPerfectEvadeApRecoverySkillLogic apRecoveryLogic)
            {
                if (apRecoveryLogic.TryGetPerfectEvadeApRecovery(
                    martialSkill,
                    BuffManager.Instance.GetEffects(true),
                    currentState.isMorningStarApRecoveredThisSkill,
                    out float apRecovery))
                {
                    var playerEntity = TurnManager.Instance.turnQueue.Find(e => e.isPlayer);

                    if (playerEntity != null)
                    {
                        playerEntity.actionGauge += apRecovery;
                        currentState.isMorningStarApRecoveredThisSkill = true;

                        DevLog.Log($"[새벽별:난식] 완벽 회피 성공! 행동 게이지 {apRecovery} 회복.");
                    }
                }
            }

            return;
        }

        // 피격
        if (!skillResult.isGuardTriggered && !isInvincible)
        {
            StyleRankManager.Instance.OnPlayerHit();
        }
        else if (isInvincible)
        {
            DevLog.Log("[무하한] 무적 상태이므로 스타일 랭크가 감소하지 않습니다.");
        }
    }

    private void EnqueueMorningStarCounterIfNeeded(SkillExecutionContext context)
    {
        SkillResult skillResult = context.result;
        bool isPlayerDefending = context.presentation.isPlayerDefending;
        bool isPureUtility = context.presentation.isPureUtility;
        bool isCounterTriggered = false;

        if (!skillResult.anyHit && !isPureUtility && isPlayerDefending)
        {
            var martialSkill = PlayerManager.Instance.unlockedSkills.Find(
                s => s.category == SkillCategory.Martial);

            if (martialSkill != null && martialSkill.skillLogic is IPerfectEvadeCounterSkillLogic counterLogic)
            {
                if (counterLogic.TryGetPerfectEvadeCounter(
                    martialSkill,
                    currentPlayerStats,
                    BuffManager.Instance.GetEffects(true),
                    out int counterDmg,
                    out Sprite counterImage))
                {
                    isCounterTriggered = true;

                    BattleVisualizer.Instance.EnqueueDelay(timing.counterPreDelay);
                    BattleVisualizer.Instance.EnqueueAction(() =>
                        ApplyCounterAndReflectUI(counterDmg, counterImage, false));
                    BattleVisualizer.Instance.EnqueueDelay(timing.counterHold);
                }
            }
        }

        if (!isCounterTriggered)
            BattleVisualizer.Instance.EnqueueDelay(timing.postSkillHold);
    }

    private void EnqueueEnemyCounterIfNeeded(SkillExecutionContext context)
    {
        if (!context.isPlayerAttacking) return;
        if (!context.result.anyHit) return;
        if (context.presentation.isPureUtility) return;
        if (currentEnemyData == null) return;
        if (!(currentEnemyData.aiBrain is IEnemySkillDamageCounter counterAI)) return;
        if (!CanEnemySkillDamageCounterTrigger(counterAI)) return;

        BattleVisualizer.Instance.EnqueueDelay(timing.enemyCounterPreDelay);   // 히트 여운
        BattleVisualizer.Instance.EnqueueAction(TryTriggerEnemyCounterAfterEnemyTakesSkillDamage);
    }

    private void EnqueueGuardAndReflectIfNeeded(SkillExecutionContext context)
    {
        SkillResult skillResult = context.result;
        bool isPlayerAttacking = context.isPlayerAttacking;
        bool isPlayerDefending = context.presentation.isPlayerDefending;
        bool shouldLogReflectDebug = isPlayerDefending || skillResult.isGuardTriggered;

        if (shouldLogReflectDebug)
            DevLog.Log($"[IngaYul Debug] guard check. isPlayerAttacking={isPlayerAttacking}, isPlayerDefending={isPlayerDefending}, anyHit={skillResult.anyHit}, isGuardTriggered={skillResult.isGuardTriggered}, totalMitigatedDamage={skillResult.totalMitigatedDamage}");

        if (!skillResult.isGuardTriggered)
        {
            if (shouldLogReflectDebug)
                DevLog.Log("[IngaYul Debug] skipped: isGuardTriggered == false");
            return;
        }

        BattleVisualizer.Instance.EnqueueAction(() =>
        {
            if (ShouldCancelPlayerCounterReaction())
                return;

            StyleRankManager.Instance?.OnSupportActionUsed();
            BuffManager.Instance.ConsumeGuardEffect(true);
        });

        if (!isPlayerDefending)
        {
            DevLog.Log("[IngaYul Debug] skipped: isPlayerDefending == false");
            return;
        }

        if (ShouldCancelPlayerCounterReaction())
        {
            DevLog.Log("[IngaYul Debug] skipped: player counter reaction cancelled before enqueue.");
            return;
        }

        float reflectRatio = 0f;

        if (PlayerManager.Instance != null)
            reflectRatio = PlayerManager.Instance.GetReflectRatio();

        DevLog.Log($"[IngaYul Debug] reflectRatio={reflectRatio}");

        if (reflectRatio <= 0f)
        {
            DevLog.Log("[IngaYul Debug] skipped: reflectRatio <= 0");
            return;
        }

        int reflectDamage = Mathf.Max(
            1,
            Mathf.RoundToInt(skillResult.totalMitigatedDamage * reflectRatio));

        DevLog.Log($"[IngaYul Debug] reflect triggered. damage={reflectDamage}");

        Sprite reflectSprite = playerData.reflectImage != null
            ? playerData.reflectImage
            : playerData.guardImage;

        BattleVisualizer.Instance.EnqueueDelay(timing.counterPreDelay);
        BattleVisualizer.Instance.EnqueueAction(() =>
            ApplyCounterAndReflectUI(reflectDamage, reflectSprite, true));

        BattleVisualizer.Instance.EnqueueDelay(timing.counterHold);
    }

    private void CompleteSkillSequence(bool isPlayerAttacking)
    {
        if (isPlayerAttacking && currentState.isUnleashingCharge)
            currentState.isUnleashingCharge = false;

        if (CheckAndHandleBattleEnd())
            return;

        if (!isPlayerAttacking && TryExecuteNextPendingEnemySkill())
            return;

        ResolveTurnEnd();
    }

    private bool TryExecuteNextPendingEnemySkill()
    {
        if (pendingEnemySkillSequence.Count == 0)
            return false;

        if (combatEnded)
        {
            ClearPendingEnemySkillSequence();
            return false;
        }

        if (currentEnemyData == null || currentEnemyHp <= 0 || currentPlayerStats == null || currentPlayerStats.currentHp <= 0)
        {
            ClearPendingEnemySkillSequence();
            return false;
        }

        if (currentActiveEntity == null || currentActiveEntity.type != EntityType.Enemy)
        {
            ClearPendingEnemySkillSequence();
            return false;
        }

        while (pendingEnemySkillSequence.Count > 0)
        {
            SkillData nextSkill = pendingEnemySkillSequence.Dequeue();
            if (nextSkill == null)
            {
                DevLog.LogWarning("[EnemySkillSequence] Null pending skill skipped.");
                continue;
            }

            PerformSkillRoutine(nextSkill, false, nextSkill.isUltimate);
            return true;
        }

        return false;
    }

    private void ClearCurrentTurnDeathGuard()
    {
        currentState.currentTurnDeathGuardActive = false;
        currentState.currentTurnDeathGuardMinHp = 0;
    }

    private bool CheckAndHandleBattleEnd()
    {
        return ResolveBattleEndAfterHpChanged();
    }

    private bool ResolveBattleEndAfterHpChanged()
    {
        if (combatEnded)
            return true;

        if (currentPlayerStats == null)
            return false;

        if (currentEnemyHp <= 0 || currentPlayerStats.currentHp <= 0)
        {
            EndCombat(currentEnemyHp <= 0);
            return true;
        }

        return false;
    }

    // 스킬 시전 초기 연출 (이미지, 대사, 코스트 지불 등)
    private void ApplySkillCastUI(
        SkillData skill,
        bool isPlayerAttacking,
        SkillResult skillResult,
        string commentary,
        string commentaryKey,
        string commentaryFallback,
        object[] commentaryArgs,
        bool isPureUtility)
    {
        EnsurePresentationDirector();

        presentationDirector?.ShowSpecialCastPresentationIfNeeded(skill, isPlayerAttacking);
        presentationDirector?.SetCasterImage(isPlayerAttacking, skill.skillActionImage);
        PaySkillCostForCast(skill, isPlayerAttacking);
        CompanionManager.Emotion emotion = ResolveCompanionEmotionAfterSkillCast(skillResult, isPlayerAttacking);
        CompanionManager.Instance.UpdateEmotion(emotion);

        // 2. 방어자 이미지 변경
        Sprite reactionSprite = ResolveDefenderReactionSprite(skillResult, isPlayerAttacking, isPureUtility);
        SkillCastPresentationContext presentationContext = new SkillCastPresentationContext
        {
            skill = skill,
            isPlayerAttacking = isPlayerAttacking,
            skillResult = skillResult,
            commentary = commentary,
            commentaryKey = commentaryKey,
            commentaryFallback = commentaryFallback,
            commentaryArgs = commentaryArgs,
            isPureUtility = isPureUtility,
            reactionSprite = reactionSprite,
            showCritAlert = skillResult.anyCrit && !isPureUtility
        };

        ShowSkillCastResultPresentation(presentationContext);
    }

    private void PaySkillCostForCast(
        SkillData skill,
        bool isPlayerAttacking)
    {
        skill.skillLogic?.PaySkillCost(
            skill,
            currentPlayerStats,
            currentEnemyData,
            isPlayerAttacking);
    }

    private void ShowSkillCastResultPresentation(SkillCastPresentationContext context)
    {
        EnsurePresentationDirector();

        presentationDirector?.ShowCastResultPresentation(
            !context.isPlayerAttacking,
            context.reactionSprite,
            context.commentary,
            context.showCritAlert,
            context.commentaryKey,
            context.commentaryFallback,
            context.commentaryArgs);
    }

    private CompanionManager.Emotion ResolveCompanionEmotionAfterSkillCast(
        SkillResult skillResult,
        bool isPlayerAttacking)
    {
        return skillResult.anyHit ?
            (isPlayerAttacking ? CompanionManager.Emotion.Happy : CompanionManager.Emotion.Worried) :
            (isPlayerAttacking ? CompanionManager.Emotion.Worried : CompanionManager.Emotion.Happy);
    }

    // Defender reaction sprite
    private Sprite ResolveDefenderReactionSprite(
        SkillResult skillResult,
        bool isPlayerAttacking,
        bool isPureUtility)
    {
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

        return reactionSprite;
    }

    // Miss presentation
    private void ProcessMissAction(bool isPlayerAttacking, bool isPlayerDefending, bool isPureUtility, SkillResult skillResult)
    {
        if (isPureUtility) return;

        BattleEventSystem.CallEvaded(isPlayerDefending);

        ShowEvadePresentationIfNeeded(isPlayerAttacking, isPlayerDefending, skillResult);

        // (StyleRank 및 새벽별 로직은 PerformSkillRoutine으로 이관되어 삭제됨)
    }

    private void ShowEvadePresentationIfNeeded(
        bool isPlayerAttacking,
        bool isPlayerDefending,
        SkillResult skillResult)
    {
        if (skillResult.anyHit)
            return;

        Sprite evadeSprite = isPlayerDefending ? playerData?.evade : currentEnemyData?.evade;
        CombatUIManager.Instance.SetDefenderImage(!isPlayerAttacking, evadeSprite);
        CombatSfxController.Instance?.PlayDodge();
    }

    // 단일 타격 성공(명중) 연출
    // ==========================================================
    private void ProcessHitAction(HitResult hit, bool isPlayerAttacking, bool isPlayerDefending, bool isPureUtility, SkillResult skillResult, SkillData skill)
    {
        if (isPureUtility) return;

        RewardCriticalHitIfNeeded(hit, isPlayerAttacking);

        if (isPlayerAttacking)
            ProcessPlayerSuccessfulHit(hit, skill);
        else if (!ProcessEnemySuccessfulHit(hit, skill))
            return;

        ApplyBreakDamageAfterHit(hit, isPlayerAttacking);

        if (!isPureUtility && !ShouldSuppressDamageText(isPlayerDefending))
            BattleEventSystem.CallDamageTaken(isPlayerDefending, hit.damage, hit.isCrit);
    }

    private void ProcessPlayerSuccessfulHit(HitResult hit, SkillData skill)
    {
        ApplyDamageToEntity(false, hit.damage);
        PlaySkillHitSfxForResolvedDamage(hit.damage, hit.isCrit);
        AccumulatePlayerHitDamageIfBombInactive(hit);
        ApplyPlayerLifestealAfterHit(hit, skill);
    }

    private bool ProcessEnemySuccessfulHit(HitResult hit, SkillData skill)
    {
        // 1. 일반 타격 데미지 적용 (단 한 번만!)
        bool isDead = ApplyDamageToEntity(true, hit.damage);
        PlaySkillHitSfxForResolvedDamage(hit.damage, hit.isCrit);

        if (isDead || currentPlayerStats.currentHp <= 0)
        {
            if (!ShouldSuppressDamageText(true))
                BattleEventSystem.CallDamageTaken(true, hit.damage, hit.isCrit);

            CheckAndHandleBattleEnd();
            BattleVisualizer.Instance?.ClearPendingVisuals();
            return false;
        }

        // 2. 적군 흡혈 로직
        ApplyEnemyLifestealAfterHit(hit, skill);

        // 3. [핵심] 특수 효과 처리 (스택 폭발 등)
        // 이제 하드코딩 없이 어떤 보스 스킬이든 TryProcessHitEffect가 구현되어 있으면 호출됩니다.
        ProcessEnemySpecialHitEffect(skill);

        // 4. 기 모으기 파괴 로직
        CancelPlayerChargeIfInterrupted(hit);
        return true;
    }

    private void RewardCriticalHitIfNeeded(HitResult hit, bool isPlayerAttacking)
    {
        if (hit.isCrit && isPlayerAttacking && !currentState.hasRewardedCritThisSkill)
        {
            StyleRankManager.Instance.OnCriticalHit();
            currentState.hasRewardedCritThisSkill = true;
        }
    }

    private void AccumulatePlayerHitDamageIfBombInactive(HitResult hit)
    {
        if (!currentState.isBombActive) currentState.accumulatedDamage += hit.damage;
    }

    private void CancelPlayerChargeIfInterrupted(HitResult hit)
    {
        if (currentState.isPlayerCharging && hit.damage > 0)
        {
            currentState.isPlayerCharging = false;
            currentState.chargingSkill = null;
            CombatUIManager.Instance.SpawnDamageText("Broken!", false, true);
            DevLog.Log("[원기옥] 피격당하여 기 모으기가 취소되었습니다!");
        }
    }

    private void ApplyPlayerLifestealAfterHit(HitResult hit, SkillData skill)
    {
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

    private void ApplyEnemyLifestealAfterHit(HitResult hit, SkillData skill)
    {
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
    }

    private void ProcessEnemySpecialHitEffect(SkillData skill)
    {
        if (skill == null)
        {
            DevLog.LogWarning("[EnemySpecialHitEffect] Skill is null. Special hit effect skipped.");
            return;
        }

        if (skill.skillLogic == null)
            return;

        if (currentEnemyData == null)
        {
            string skillName = string.IsNullOrEmpty(skill.skillNameKey) ? skill.name : skill.skillNameKey;
            DevLog.LogWarning($"[EnemySpecialHitEffect] Current enemy data is null. Skill={skillName}");
            return;
        }

        int explosionDamage = skill.skillLogic.TryProcessHitEffect(currentEnemyData);

        if (explosionDamage > 0)
        {
            // 특수 피해 적용 (이미 일반 데미지는 위에서 들어갔으므로 이것만 추가로 들어감)
            CombatManager.Instance.ApplyDamageToEntity(true, explosionDamage);
            PlayNormalHitSfxForResolvedDamage(explosionDamage);

            // 연출: 피격 이미지 + 보라색 데미지 텍스트
            CombatUIManager.Instance.SetDefenderImage(true, playerData.hit);
            if (!ShouldSuppressDamageText(true))
                CombatUIManager.Instance.SpawnDamageText($"★{explosionDamage}", false, true);

            DevLog.Log($"[스킬 특수 효과] 특수 피해 {explosionDamage} 발생!");
        }
    }

    private void ApplyBreakDamageAfterHit(HitResult hit, bool isPlayerAttacking)
    {
        if (isPlayerAttacking && !BreakManager.Instance.IsBroken(false))
            if (BreakManager.Instance.AddBreakDamage(false, hit.breakDamage)) UpdateTurnOrderUI();

        if (!isPlayerAttacking && !BreakManager.Instance.IsBroken(true))
            if (BreakManager.Instance.AddBreakDamage(true, hit.breakDamage)) UpdateTurnOrderUI();
    }

    // 새벽별 카운터 및 인과율 반사 연출
    // ==========================================================
    private void ApplyCounterAndReflectUI(int damage, Sprite defenderImage, bool isReflect)
    {
        if (ShouldCancelPlayerCounterReaction())
        {
            CheckAndHandleBattleEnd();
            return;
        }

        CombatUIManager.Instance.SetDefenderImage(true, defenderImage);
        CombatUIManager.Instance.SetDefenderImage(false, currentEnemyData?.hit);
        ApplyDamageToEntity(false, damage);
        PlayNormalHitSfxForResolvedDamage(damage);

        if (isReflect)
        {
            CombatUIManager.Instance.SpawnDamageText("★" + damage.ToString(), false, false);
            CombatUIManager.Instance.InterruptAndTypeLocalizedCommentary("combat_comment_reflect_format", "[인과율 발동!] 튕겨낸 힘으로 적에게 {0}의 고정 피해를 반사합니다!", damage);
        }
        else
        {
            CombatUIManager.Instance.SpawnDamageText("★" + damage.ToString(), false, false);
            DevLog.Log($"[새벽별:멸식] 카운터 발동! {damage} 피해");
        }
    }

    private bool ShouldCancelPlayerCounterReaction()
    {
        if (combatEnded)
            return true;

        if (currentPlayerStats == null || currentPlayerStats.currentHp <= 0)
            return true;

        if (currentEnemyData == null || currentEnemyHp <= 0)
            return true;

        return false;
    }

    // 화면 복구 (이펙트, 랭크, 이미지 초기화)
    // ==========================================================
    private void ResetCombatUI(bool isPlayerAttacking, bool isPlayerDefending, bool isUltimate, SkillData skill)
    {
        EnsurePresentationDirector();
        presentationDirector?.ClearCombatEffects();

        UpdateStyleRankAfterSkillReset(isPlayerAttacking, isUltimate, skill);

        ResetCasterImageAfterSkillIfNeeded(isPlayerAttacking);

        RestoreDefenderImageAfterSkill(isPlayerAttacking, isPlayerDefending);

    }

    private void RestoreDefenderImageAfterSkill(bool isPlayerAttacking, bool isPlayerDefending)
    {
        if (ShouldHoldEnemyHitImageAfterInfiniteBattleDefeat(isPlayerAttacking, isPlayerDefending))
            return;

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

    private bool ShouldHoldEnemyHitImageAfterInfiniteBattleDefeat(bool isPlayerAttacking, bool isPlayerDefending)
    {
        return InfiniteBattleRunContext.IsRunPrepared
            && isPlayerAttacking
            && !isPlayerDefending
            && currentEnemyHp <= 0;
    }

    private void UpdateStyleRankAfterSkillReset(bool isPlayerAttacking, bool isUltimate, SkillData skill)
    {
        if (!isPlayerAttacking)
            return;

        EnsureActionMenuController();

        SkillCategory usedCategory = skill != null
            ? skill.category
            : (actionMenuController != null ? actionMenuController.SelectedCategory : SkillCategory.Sword);

        StyleRankManager.Instance.OnSkillUsed(usedCategory);
        StyleRankManager.Instance.ResetTurnState();

        if (isUltimate)
            StyleRankManager.Instance.ResetRankForUltimate();
    }

    private void ResetCasterImageAfterSkillIfNeeded(bool isPlayerAttacking)
    {
        if (isPlayerAttacking && currentState.isPlayerCharging)
            return;

        CombatUIManager.Instance.ResetCasterImage(isPlayerAttacking);
    }

    public bool ApplyDamageToEnemy(int damage)
    {
        return ApplyDamageToEntity(false, damage);
    }

    public int CalculateEnemyMitigatedDamageFromRaw(int rawDamage, string sourceLabel, float armorPenetrationRatio = 0f)
    {
        if (rawDamage <= 0) return 0;

        int enemyDefense = currentEnemyData != null ? currentEnemyData.defense : 0;
        if (StatManager.Instance != null)
            enemyDefense = StatManager.Instance.GetEffectiveStat(false, TargetStat.Defense);

        armorPenetrationRatio = Mathf.Clamp01(armorPenetrationRatio);
        float defenseReduction = CombatMath.GetDamageReduction(enemyDefense);
        float effectiveDefenseReduction = defenseReduction * (1f - armorPenetrationRatio);
        float defenseMultiplier = 1f - effectiveDefenseReduction;
        float damageReduction = GetActiveDamageReduction(false);

        float mitigatedDamage = rawDamage * defenseMultiplier;
        if (damageReduction > 0f)
            mitigatedDamage *= (1f - Mathf.Clamp01(damageReduction));

        int finalDamage = Mathf.Max(1, Mathf.RoundToInt(mitigatedDamage));
        DevLog.Log($"[CompanionDamage] source={sourceLabel}, raw={rawDamage}, enemyDEF={enemyDefense}, defMultiplier={defenseMultiplier:F3}, damageReduction={damageReduction:F3}, final={finalDamage}");

        return finalDamage;
    }

    public bool ApplyMitigatedDamageToEnemy(int rawDamage, string sourceLabel, out int finalDamage, float armorPenetrationRatio = 0f)
    {
        finalDamage = CalculateEnemyMitigatedDamageFromRaw(rawDamage, sourceLabel, armorPenetrationRatio);
        if (finalDamage <= 0) return false;

        return ApplyDamageToEnemy(finalDamage);
    }

    private float GetActiveDamageReduction(bool isPlayerTarget)
    {
        if (BuffManager.Instance == null) return 0f;

        float damageReduction = 0f;
        var effects = BuffManager.Instance.GetEffects(isPlayerTarget);
        foreach (var effect in effects)
        {
            if (effect.effectData != null && effect.effectData.specialType == SpecialEffectType.DamageReduction)
                damageReduction += effect.value;
        }

        return damageReduction;
    }

    private void TryTriggerEnemyCounterAfterEnemyTakesSkillDamage()
    {
        if (currentEnemyData == null) return;
        if (!(currentEnemyData.aiBrain is IEnemySkillDamageCounter counterAI)) return;
        if (!CanEnemySkillDamageCounterTrigger(counterAI)) return;
        if (currentState.hasTriggeredEnemyCounterThisSkill) return;
        if (currentEnemyHp <= 0) return;

        currentState.hasTriggeredEnemyCounterThisSkill = true;
        counterAI.OnCounterTriggered(currentEnemyData);

        int counterDamage = counterAI.GetCounterDamage(currentEnemyData);

        Sprite counterSprite = counterAI.GetCounterImage(currentEnemyData);
        if (counterSprite != null) CombatUIManager.Instance.SetCasterImage(false, counterSprite);

        CombatUIManager.Instance.SetDefenderImage(true, playerData.hit);
        ApplyDamageToEntity(true, counterDamage);
        PlayNormalHitSfxForResolvedDamage(counterDamage);
        if (!ShouldSuppressDamageText(true))
            CombatUIManager.Instance.SpawnDamageText("★" + counterDamage.ToString(), false, true);

        if (BreakManager.Instance != null && !BreakManager.Instance.IsBroken(true))
        {
            if (BreakManager.Instance.AddBreakDamage(true, counterAI.GetCounterBreakDamage())) UpdateTurnOrderUI();
        }

        string counterMessageKey = GetEnemyCounterMessageKey(counterAI);
        string counterMessageFallback = counterAI.GetCounterMessage(counterDamage);
        if (!string.IsNullOrEmpty(counterMessageKey))
            CombatUIManager.Instance.InterruptAndTypeLocalizedCommentary(counterMessageKey, counterMessageFallback, counterDamage);
        else
            CombatUIManager.Instance.InterruptAndTypeCommentary(counterMessageFallback);
        DevLog.Log($"[Enemy Counter] Counter damage {counterDamage}.");
    }

    private bool CanEnemySkillDamageCounterTrigger(IEnemySkillDamageCounter counterAI)
    {
        if (counterAI == null) return false;
        if (!counterAI.CanCounterAfterSkillDamage()) return false;
        if (counterAI is EnemyAI_Uriel) return CanUrielCounterAfterPlayerAttack();
        if (counterAI is EnemyAI_Pati) return CanPatiCounterAfterPlayerAttack();

        return true;
    }

    private bool CanEnemyCounterAfterPlayerAttack()
    {
        if (combatEnded) return false;
        if (currentEnemyData == null) return false;
        if (currentEnemyHp <= 0) return false;
        if (BreakManager.Instance == null) return false;
        if (BreakManager.Instance.IsBroken(false)) return false;

        return true;
    }

    private bool CanUrielCounterAfterPlayerAttack()
    {
        return CanEnemyCounterAfterPlayerAttack();
    }

    private bool CanPatiCounterAfterPlayerAttack()
    {
        return CanEnemyCounterAfterPlayerAttack();
    }

    public bool ApplyDamageToEntity(bool isPlayerTarget, int damage)
    {
        return DamageResolver.ApplyDamageToEntity(
            isPlayerTarget,
            damage,
            currentPlayerStats,
            currentEnemyData,
            currentState,
            ref currentEnemyHp
        );
    }

    public void HealEntity(bool isPlayerTarget, int amount)
    {
        DamageResolver.HealEntity(
            isPlayerTarget,
            amount,
            currentPlayerStats,
            currentEnemyData,
            ref currentEnemyHp
        );
    }

    private bool ShouldSuppressDamageText(bool isPlayerTarget)
    {
        return isPlayerTarget && DamageResolver.LastResult.showEndureText;
    }

    private void PlaySkillHitSfxForResolvedDamage(int attemptedDamage, bool isCritical)
    {
        if (!ShouldPlayImpactSfx(attemptedDamage))
            return;

        CombatSfxController.Instance?.PlaySkillHit(isCritical);
    }

    private void PlayNormalHitSfxForResolvedDamage(int attemptedDamage)
    {
        if (!ShouldPlayImpactSfx(attemptedDamage))
            return;

        CombatSfxController.Instance?.PlayNormalHit();
    }

    private bool ShouldPlayImpactSfx(int attemptedDamage)
    {
        return attemptedDamage > 0 || DamageResolver.LastResult.wasEndured;
    }

    public void SetPlayerHpToOneForScriptedEffect()
    {
        if (currentPlayerStats == null) return;
        if (currentPlayerStats.currentHp <= 0) return;

        currentPlayerStats.currentHp = Mathf.Min(currentPlayerStats.currentHp, 1);

        if (PlayerManager.Instance != null && PlayerManager.Instance.stats != null)
        {
            PlayerManager.Instance.stats.currentHp = currentPlayerStats.currentHp;
        }

        BattleEventSystem.CallHpChanged(true, currentPlayerStats.currentHp, currentPlayerStats.maxHp);

        if (CombatUIManager.Instance != null && CombatUIManager.Instance.playerStatusUI != null)
        {
            CombatUIManager.Instance.playerStatusUI.UpdateHP(currentPlayerStats.currentHp, currentPlayerStats.maxHp);
        }

        RefreshSpecialStatsProgressUI();
    }

    // 데몬 6점 및 전설 - 초과 회복(Over-heal) 비례 버프 발생기
    public void ApplyOverhealBuff(int excessHeal)
    {
        if (PlayerManager.Instance == null) return;
        var syn = PlayerManager.Instance.GetCurrentSynergies();
        var inventory = PlayerManager.Instance.inventory;
        ItemSynergyBalanceData synergyBalance = ItemSynergyBalanceData.Resolve();

        bool has6Point = syn.GetValueOrDefault(ItemClass.Demon) >= 6;
        bool hasLegendary = inventory.Exists(x => x.data.itemClass == ItemClass.Demon && x.data.grade == ItemGrade.Legendary);

        if (!has6Point && !hasLegendary) return;

        // 배율 산출: 기획안에 따라 최대 체력 비례 %당 1% (6점) + 0.5% (전설)
        float multiplier = 0f;
        if (has6Point) multiplier += synergyBalance.demon6OverhealAmpMultiplier;
        if (hasLegendary) multiplier += synergyBalance.demonLegendaryOverhealAmpMultiplier;

        // 공식: (초과 회복량 / 최대 체력) * 배율
        // 예: 1000 체력 중 200 초과 회복 시 -> 0.2 * 1.5 = 0.3f (30% 증폭)
        float ampValue = ((float)excessHeal / currentPlayerStats.maxHp) * multiplier;

        if (ampValue > 0f)
        {
            if (demonOverhealDamageAmpEffect == null)
            {
                DevLog.LogWarning("[피의 폭주] Demon 초과회복 피해증폭 StatusEffectData가 연결되지 않았습니다.");
                return;
            }

            BuffManager.Instance.AddEffect(true, demonOverhealDamageAmpEffect, ampValue, 1);
            DevLog.Log($"[피의 폭주] 초과 회복 {excessHeal} 달성 -> 피해 증폭 {ampValue * 100:F1}% 버프 1턴 획득!");
        }
    }

    public void EndCombat(bool isWin)
    {
        if (combatEnded)
            return;

        combatEnded = true;

        PlayerManager playerManager = PlayerManager.Instance;

        if (InfiniteBattleRunContext.IsRunPrepared)
        {
            HandleInfiniteBattleEnd(isWin);
            return;
        }

        if (isWin)
        {
            VictoryRewardGrantResult rewardResult = null;

            if (playerManager != null && currentPlayerStats != null)
            {
                playerManager.stats.currentHp = currentPlayerStats.currentHp;
            }

            if (playerManager != null)
            {
                rewardResult = BattleRewardService.GrantReward(
                    playerManager,
                    playerManager.currentBattleReward
                );

                if (!playerManager.suppressPendingBattleProgress)
                {
                    playerManager.pendingAdvanceBattleTurn = true;
                    playerManager.pendingBattleType = playerManager.currentBattleType;
                    playerManager.pendingBattlePhase = playerManager.currentBattlePhase;
                }
                else
                {
                    playerManager.pendingAdvanceBattleTurn = false;
                    DevLog.Log($"[HiddenBoss] Suppressed normal battle progress. hiddenBossID={playerManager.currentHiddenBossID}");
                }
            }
            else
            {
                DevLog.LogWarning("CombatManager: PlayerManager.Instance is missing during victory reward processing.");
            }

            if (victoryUIController != null)
            {
                string enemyName = currentEnemyData != null ? GetTranslatedText(currentEnemyData.enemyNameKey) : "Enemy";
                Time.timeScale = 0f;
                victoryUIController.ShowVictory(enemyName, rewardResult);
                return;
            }

            DevLog.LogWarning("CombatManager: CombatVictoryUIController is not assigned. Returning to Exploration immediately.");
            Time.timeScale = 1f;
            SceneLoader.LoadScene("Exploration");
            return;
        }

        ShowDefeatUI();
    }

    private void HandleInfiniteBattleEnd(bool isWin)
    {
        PlayerManager playerManager = PlayerManager.Instance;

        if (playerManager != null && currentPlayerStats != null)
            playerManager.stats.currentHp = Mathf.Max(0, currentPlayerStats.currentHp);

        if (isWin)
        {
            if (currentPlayerStats == null || currentPlayerStats.currentHp <= 0)
            {
                ShowInfiniteBattleResult();
                return;
            }

            InfiniteBattleRunContext.SetCurrentHpAfterBattle(currentPlayerStats.currentHp);
            InfiniteBattleRunContext.MarkCurrentFloorCleared();
            InfiniteBattleRunContext.AdvanceToNextFloor();
            InfiniteBattlePlayerApplier.ApplyCurrentHp(InfiniteBattleRunContext.CurrentPlayerHp);

            if (!InfiniteBattleEncounterBuilder.PrepareCurrentFloorEncounter())
            {
                DevLog.LogWarning("[InfiniteBattle] Next floor prepare failed. Showing result.");
                ShowInfiniteBattleResult();
                return;
            }

            Time.timeScale = 1f;
            SceneLoader.LoadScene(InfiniteBattleRunContext.Config.BattleSceneName);
            return;
        }

        ShowInfiniteBattleResult();
    }

    private void ShowInfiniteBattleResult()
    {
        int currentRecord = InfiniteBattleRunContext.ClearedFloorCount;
        int previousBest = InfiniteBattleRunContext.BestFloorBeforeRun;
        bool hasDirectReference = infiniteBattleResultUI != null;

        DevLog.Log($"[InfiniteBattle] ShowInfiniteBattleResult entered. directReferenceNull={!hasDirectReference}");
        if (hasDirectReference)
        {
            GameObject directObject = infiniteBattleResultUI.gameObject;
            DevLog.Log($"[InfiniteBattle] Direct result UI reference: name={directObject.name}, activeSelf={directObject.activeSelf}, activeInHierarchy={directObject.activeInHierarchy}");
        }

        if (SaveManager.Instance != null)
            SaveManager.Instance.UpdateInfiniteBattleBestFloor(InfiniteBattleRunContext.ClearId, currentRecord);

        Time.timeScale = 0f;

        InfiniteBattleResultUIController resultUI = ResolveInfiniteBattleResultUI(out bool usedDirectReference, out bool usedFallback, out string resolveSource);
        DevLog.Log($"[InfiniteBattle] Result UI display requested. currentRecord={currentRecord}, previousBest={previousBest}, usedDirectReference={usedDirectReference}, usedFallback={usedFallback}, resolveSource={resolveSource}");
        if (resultUI != null)
        {
            if (resultUI.ShowResult(currentRecord, previousBest))
                return;

            DevLog.LogWarning("[InfiniteBattle] Result UI ShowResult returned false. Returning to MainMenu.");
        }

        DevLog.LogWarning("[InfiniteBattle] Result UI missing. Returning to MainMenu.");
        InfiniteBattleRunContext.Clear();
        Time.timeScale = 1f;
        SceneLoader.LoadScene("MainMenu");
    }

    private InfiniteBattleResultUIController ResolveInfiniteBattleResultUI(out bool usedDirectReference, out bool usedFallback, out string resolveSource)
    {
        usedDirectReference = false;
        usedFallback = false;
        resolveSource = "none";

        if (infiniteBattleResultUI != null)
        {
            usedDirectReference = true;
            resolveSource = "inspector";
            return infiniteBattleResultUI;
        }

        usedFallback = true;
        DevLog.LogWarning("[InfiniteBattle] CombatManager infiniteBattleResultUI is not assigned. Trying scene-local fallback search.");
        infiniteBattleResultUI = ResolveInfiniteBattleResultUIInOwnScene();
        if (infiniteBattleResultUI != null)
        {
            resolveSource = "combatManagerScene";
            return infiniteBattleResultUI;
        }

        DevLog.LogWarning("[InfiniteBattle] Scene-local result UI fallback failed. Trying global inactive fallback search.");
        infiniteBattleResultUI = InfiniteBattleResultUIController.GetOrCreate();
        resolveSource = infiniteBattleResultUI != null ? "globalFallback" : "none";
        return infiniteBattleResultUI;
    }

    private InfiniteBattleResultUIController ResolveInfiniteBattleResultUIInOwnScene()
    {
        UnityEngine.SceneManagement.Scene scene = gameObject.scene;
        if (!scene.IsValid() || !scene.isLoaded)
        {
            DevLog.LogWarning("[InfiniteBattle] CombatManager scene is invalid or not loaded.");
            return null;
        }

        GameObject[] rootObjects = scene.GetRootGameObjects();
        foreach (GameObject rootObject in rootObjects)
        {
            if (rootObject == null)
                continue;

            InfiniteBattleResultUIController[] controllers = rootObject.GetComponentsInChildren<InfiniteBattleResultUIController>(true);
            if (controllers != null && controllers.Length > 0)
                return controllers[0];
        }

        foreach (GameObject rootObject in rootObjects)
        {
            GameObject canvas = FindChildGameObjectByName(rootObject, "InfiniteBattleCanvas");
            if (canvas != null)
            {
                DevLog.LogWarning("[InfiniteBattle] InfiniteBattleCanvas found without InfiniteBattleResultUIController. Adding runtime controller.");
                return canvas.AddComponent<InfiniteBattleResultUIController>();
            }
        }

        return null;
    }

    private GameObject FindChildGameObjectByName(GameObject root, string objectName)
    {
        if (root == null || string.IsNullOrEmpty(objectName))
            return null;

        if (root.name == objectName)
            return root;

        Transform rootTransform = root.transform;
        for (int i = 0; i < rootTransform.childCount; i++)
        {
            GameObject found = FindChildGameObjectByName(rootTransform.GetChild(i).gameObject, objectName);
            if (found != null)
                return found;
        }

        return null;
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
                yield return CombatUIManager.Instance.TypeLocalizedCommentary("combat_comment_regen_trigger_format", "{0}의 지속 회복 효과 발동!", new object[] { targetName }, true, timing.specialExpireCommentDelay);

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
                yield return new WaitForSeconds(timing.specialExpireHold);
            }

            if (currentActiveEntity.isPlayer) BuffManager.Instance.AdvanceTurnActiveEffects(true);
            else if (currentActiveEntity.type == EntityType.Enemy)
            {
                BuffManager.Instance.AdvanceTurnActiveEffects(false);

                if (currentEnemyData != null && currentEnemyData.aiBrain != null)
                    currentEnemyData.aiBrain.UpdatePassives(currentEnemyData);

                CombatUIManager.Instance.RefreshBuffUI();
            }

            //  캐스터 시너지: 매 턴 종료 시 무작위 독립 버프 부여
            if (currentActiveEntity.isPlayer && PlayerManager.Instance != null)
            {
                TurnEffects.ApplyCasterTurnEndEffects(PlayerManager.Instance);
            }
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
                    yield return CombatUIManager.Instance.TypeLocalizedCommentary("combat_comment_overheat_trigger", "과열(Overheat) 디버프 발동!!", null, true, timing.specialExpireCommentDelay);

                    int selfDamage = Mathf.RoundToInt(currentPlayerStats.currentHp * 0.4f);
                    ApplyDamageToEntity(true, selfDamage);
                    PlayNormalHitSfxForResolvedDamage(selfDamage);

                    CombatUIManager.Instance.SetDefenderImage(true, playerData.hit); // 주인공 피격 이미지
                    if (!ShouldSuppressDamageText(true))
                        CombatUIManager.Instance.SpawnDamageText("★" + selfDamage.ToString(), false, true);
                    BattleEventSystem.CallHpChanged(true, currentPlayerStats.currentHp, currentPlayerStats.maxHp);

                    yield return new WaitForSeconds(timing.specialExpireHold);
                    CombatUIManager.Instance.ResetDefenderImage(true);

                    if (ResolveBattleEndAfterHpChanged())
                        yield break;
                }

                // 2. [진화 B] 피해 누적 폭발 (적 피격)
                if (e.effectData.specialType == SpecialEffectType.DamageAccumulator)
                {
                    yield return CombatUIManager.Instance.TypeLocalizedCommentary("combat_comment_let_you_down_trigger", "렛 유 다운(Let You Down) 추가 피해 발동!", null, true, timing.specialExpireCommentDelay);

                    // 기록된 피해의 50%를 추가로 입힘
                    int extraDmg = Mathf.RoundToInt(currentState.accumulatedDamage * 0.5f);
                    ApplyDamageToEntity(false, extraDmg);
                    PlayNormalHitSfxForResolvedDamage(extraDmg);

                    CombatUIManager.Instance.SetDefenderImage(false, currentEnemyData.hit); // 적 피격 이미지
                    CombatUIManager.Instance.SpawnDamageText("★" + extraDmg.ToString(), false, false);
                    BattleEventSystem.CallHpChanged(false, currentEnemyHp, currentEnemyData.maxHp);

                    currentState.accumulatedDamage = 0; // 초기화
                    yield return new WaitForSeconds(timing.specialExpireHold);
                    CombatUIManager.Instance.ResetDefenderImage(false);

                    if (ResolveBattleEndAfterHpChanged())
                        yield break;
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

    private string BuildSkillCommentaryFallback(
        string attackerName,
        string skillName,
        SkillResult result,
        bool isPureUtility)
    {
        if (isPureUtility)
            return FormatLocalizedText("combat_comment_skill_utility_format", "{0:이가} {1:을를} 시전합니다.", attackerName, skillName);

        if (!result.anyHit)
            return FormatLocalizedText("combat_comment_skill_miss_format", "{0}의 {1:이가} 빗나갔습니다!", attackerName, skillName);

        if (result.anyCrit)
            return FormatLocalizedText("combat_comment_skill_crit_format", "{0}의 {1} 치명적으로 적중!", attackerName, skillName);

        return FormatLocalizedText("combat_comment_skill_hit_format", "{0}의 {1} 적중!", attackerName, skillName);
    }

    private void ResolveSkillCommentaryDescriptor(
        SkillResult result,
        bool isPureUtility,
        out string key,
        out string fallback)
    {
        if (isPureUtility)
        {
            key = "combat_comment_skill_utility_format";
            fallback = "{0:이가} {1:을를} 시전합니다.";
            return;
        }

        if (!result.anyHit)
        {
            key = "combat_comment_skill_miss_format";
            fallback = "{0}의 {1:이가} 빗나갔습니다!";
            return;
        }

        if (result.anyCrit)
        {
            key = "combat_comment_skill_crit_format";
            fallback = "{0}의 {1} 치명적으로 적중!";
            return;
        }

        key = "combat_comment_skill_hit_format";
        fallback = "{0}의 {1} 적중!";
    }

    private string GetLocalizedText(string key, string fallback)
    {
        if (!string.IsNullOrEmpty(key) && LocalizationManager.Instance != null)
        {
            string localized = LocalizationManager.Instance.GetText(key);
            if (!string.IsNullOrEmpty(localized) && localized != key)
                return localized;
        }

        if (!string.IsNullOrEmpty(fallback))
            return fallback;

        return key ?? "";
    }

    private string FormatLocalizedText(string key, string fallback, params object[] args)
    {
        string format = GetLocalizedText(key, fallback);
        try
        {
            return KoreanParticleFormatter.Format(format, args);
        }
        catch (System.FormatException)
        {
            try
            {
                return KoreanParticleFormatter.Format(fallback, args);
            }
            catch (System.FormatException)
            {
                return fallback ?? "";
            }
        }
    }

    private string GetEnemyCounterMessageKey(IEnemySkillDamageCounter counterAI)
    {
        if (counterAI is EnemyAI_Uriel) return "combat_comment_uriel_counter_format";
        if (counterAI is EnemyAI_Pati) return "combat_comment_pati_counter_format";
        return null;
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

    public void RestorePlayerSideImage()
    {
        if (CombatUIManager.Instance == null)
            return;

        // 1. 셰리가 그로기 상태라면 무조건 그로기 이미지 유지
        if (BreakManager.Instance != null && BreakManager.Instance.IsBroken(true))
        {
            if (playerData != null && playerData.breakImage != null)
            {
                CombatUIManager.Instance.SetCasterImage(true, playerData.breakImage);
            }
            else
            {
                CombatUIManager.Instance.ResetCasterImage(true);
            }

            DevLog.Log("[이미지 복구] 셰리가 그로기 상태이므로 Break 이미지로 복구합니다.");
            return;
        }

        // 2. 셰리가 기 모으기 중이면 기 모으기 이미지 유지
        if (currentState != null &&
            currentState.isPlayerCharging &&
            currentState.chargingSkill != null &&
            currentState.chargingSkill.skillActionImage != null)
        {
            CombatUIManager.Instance.SetCasterImage(true, currentState.chargingSkill.skillActionImage);
            DevLog.Log("[이미지 복구] 셰리가 기 모으기 중이므로 차지 이미지를 유지합니다.");
            return;
        }

        // 3. 그 외에는 일반 이미지로 복구
        CombatUIManager.Instance.ResetCasterImage(true);
    }

    public void RestorePlayerHpToBattleStart()
    {
        if (PlayerManager.Instance == null)
            return;

        PlayerManager.Instance.stats.currentHp = battleStartPlayerHp;
    }

    private void ShowDefeatUI()
    {
        Time.timeScale = 0f;

        if (defeatUIController != null)
        {
            defeatUIController.ShowDefeat();
        }
        else
        {
            DevLog.LogError("CombatManager: CombatDefeatUIController가 연결되지 않았습니다.");
        }
    }
}
