using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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
    private string[] categoryKeys = { "cat_sword", "cat_gun", "cat_martial", "cat_magic", "cat_oni" };
    private SkillCategory selectedCategory;
    private List<SkillData> currentDisplaySkills;

    [Header("기(Ki) 스킬 전용 상태")]
    public bool isPlayerCharging = false;
    public bool isUnleashingCharge = false;
    public SkillData chargingSkill = null;
    public bool hasUsedKiExtraTurn = false;
    public bool wasEnemyBrokenAtSkillStart = false;

    [Header("크루세이더 (Last Train Home) 상태")]
    public bool isBombActive = false;
    public int savedBombDamage = 0;
    public int lastSuccessfulHits = 0; // 진화 A 디버프 계산용

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

        yield return StartCoroutine(CombatUIManager.Instance.TypeCommentary($"{eName} 조우!", true, 1.0f));
        yield return oneSecondWait;

        SupporterData activeSup = PlayerManager.Instance.activeSupporter;
        if (activeSup != null && activeSup.startSkillLogic != null)
        {
            yield return StartCoroutine(CompanionManager.Instance.ExecuteSupporterTurn(activeSup, true));
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

    private IEnumerator ProcessTurnRoutine(TurnEntity currentTurnOwner)
    {
        currentActiveEntity = currentTurnOwner;
        playerHpAtTurnStart = currentPlayerStats.currentHp;
        enemyHpAtTurnStart = currentEnemyHp;

        string pName = playerData != null ? GetTranslatedText(playerData.playerNamekey) : "주인공";
        string eName = currentEnemyData != null ? GetTranslatedText(currentEnemyData.enemyNameKey) : "적";

        if (currentTurnOwner.type == EntityType.Enemy)
        {
            CombatUIManager.Instance.SetActionPanelActive(false);
            CombatUIManager.Instance.SetWaitingPanelActive(true);
            currentMenuState = MenuState.Hidden;

            if (isBombActive)
            {
                isBombActive = false;

                var effects = BuffManager.Instance.GetEffects(false);
                effects.RemoveAll(e => e.effectData != null && e.effectData.specialType == SpecialEffectType.TimeBomb);
                CombatUIManager.Instance.RefreshBuffUI();

                // 적 이미지를 피격 이미지로 변경 (주인공은 그대로 유지)
                CombatUIManager.Instance.SetDefenderImage(false, currentEnemyData.hit);

                // 코멘터리 및 폭발 데미지 적용
                yield return StartCoroutine(CombatUIManager.Instance.TypeCommentary("라스트 트레인 홈 발동!!", true, 0.5f));

                currentEnemyHp = Mathf.Max(0, currentEnemyHp - savedBombDamage);
                BattleEventSystem.CallHpChanged(false, currentEnemyHp, currentEnemyData.maxHp);
                CombatUIManager.Instance.SpawnDamageText(savedBombDamage.ToString(), true, false);
                DevLog.Log($"[라스트 트레인 홈] 적에게 {savedBombDamage}의 확정 피해를 입힙니다!");

                yield return new WaitForSeconds(1.0f);
                CombatUIManager.Instance.ResetDefenderImage(false);

                // 폭탄 맞고 죽었으면 턴 종료
                if (currentEnemyHp <= 0)
                {
                    EndCombat(false);
                    yield break;
                }
            }

            if (BreakManager.Instance.IsBroken(false))
            {
                yield return StartCoroutine(CombatUIManager.Instance.TypeCommentary($"{eName}이(가) 그로기 상태에서 정신을 차렸습니다."));
                BreakManager.Instance.WakeUpFromBreak(false);
                CombatUIManager.Instance.ResetDefenderImage(false);
                ResolveTurnEnd();
                yield break;
            }
            yield return StartCoroutine(CombatUIManager.Instance.TypeCommentary($"{eName}의 차례입니다!"));
            StartCoroutine(EnemyTurnRoutine());
        }
        else if (currentTurnOwner.isPlayer)
        {
            if (BreakManager.Instance.IsBroken(true))
            {
                yield return StartCoroutine(CombatUIManager.Instance.TypeCommentary($"{pName}이(가) 그로기 상태에서 정신을 차렸습니다."));
                BreakManager.Instance.WakeUpFromBreak(true);
                CombatUIManager.Instance.ResetDefenderImage(true);
                CombatUIManager.Instance.ResetCasterImage(true);
                ResolveTurnEnd();
                yield break;
            }
            if (isPlayerCharging && chargingSkill != null)
            {
                // 1. 유저가 조작할 수 없도록 버튼을 숨기고 Waiting 패널을 띄웁니다.
                CombatUIManager.Instance.SetActionPanelActive(false);
                CombatUIManager.Instance.SetWaitingPanelActive(true);

                // 2. 차지 상태를 해제하고 해방 모드로 전환합니다.
                isPlayerCharging = false;
                isUnleashingCharge = true;

                // 3. 발사 코멘터리를 띄운 뒤 즉시 스킬 연산으로 넘깁니다.
                yield return StartCoroutine(CombatUIManager.Instance.TypeCommentary($"{pName}이(가) 모아둔 기를 방출합니다!", true, 1.0f));
                DevLog.Log("[원기옥] 모은 기를 발사합니다!");

                PerformSkillRoutine(chargingSkill, true); // 이후 알아서 데미지 연산 후 턴 종료(ResolveTurnEnd)로 이어짐
            }
            else
            {
                CombatUIManager.Instance.SetWaitingPanelActive(false);
                ShowCategoryMenu();
                StartCoroutine(CombatUIManager.Instance.TypeCommentary($"{pName}, 무슨 공격을 할까요?", false));
            }
        }
        else if (currentTurnOwner.type == EntityType.Karin)
        {
            CombatUIManager.Instance.SetActionPanelActive(false);
            CombatUIManager.Instance.SetWaitingPanelActive(true);
            currentMenuState = MenuState.Hidden;
            StartCoroutine(CompanionManager.Instance.ExecuteKarinTurn());
        }
        else if (currentTurnOwner.type == EntityType.Supporter)
        {
            CombatUIManager.Instance.SetActionPanelActive(false);
            CombatUIManager.Instance.SetWaitingPanelActive(true);
            currentMenuState = MenuState.Hidden;

            SupporterData activeSup = PlayerManager.Instance.activeSupporter;
            if (activeSup != null && activeSup.battleSkillLogic != null)
                yield return StartCoroutine(CompanionManager.Instance.ExecuteSupporterTurn(activeSup, false));
            else
                ResolveTurnEnd();
        }
    }

    private IEnumerator EnemyTurnRoutine()
    {
        yield return new WaitForSeconds(0.3f);
        SkillData skillToUse = null;

        if (currentEnemyData?.aiBrain != null)
        {
            skillToUse = currentEnemyData.aiBrain.DecideNextSkill(enemyTurnCount, currentPlayerStats, currentEnemyData);
            enemyTurnCount++;
        }

        if (skillToUse != null) PerformSkillRoutine(skillToUse, false, skillToUse.isUltimate);
        else ResolveTurnEnd();
    }

    public void ShowCategoryMenu()
    {
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
        if (currentMenuState == MenuState.CategorySelect) ShowSkillMenu(slotIndex);
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
        currentMenuState = MenuState.Hidden;
        CombatUIManager.Instance.SetActionPanelActive(false);
        wasEnemyBrokenAtSkillStart = BreakManager.Instance.IsBroken(false);

        // [추가] 2. [진화 C] 차지 시작 처리
        // 만약 플레이어가 기 스킬을 썼는데, 아직 해방(Unleash) 중이 아니라면 '차지 모드'로 들어갑니다.
        if (isPlayerAttacking && skill.skillLogic is SkillLogic_Gi && skill.currentEvolution == SkillEvolution.PathC && !isUnleashingCharge)
        {
            BattleVisualizer.Instance.EnqueueAction(() =>
            {
                isPlayerCharging = true;
                chargingSkill = skill;
                CombatUIManager.Instance.SetCasterImage(true, skill.skillActionImage); // 이미지 변경
                CombatUIManager.Instance.SpawnDamageText("Charge!", false, true);
                DevLog.Log("[원기옥] 기를 모으기 시작합니다!");
            });
            BattleVisualizer.Instance.EnqueueDelay(2.0f);

            BattleVisualizer.Instance.StartSequence(() => {
                if (isPlayerAttacking) BuffManager.Instance.UpdateEffectsOnTurnEnd(true);
                CalculateNextTurn(); // 행동 없이 바로 턴 종료
            });
            return; // 여기서 함수 종료 (공격 연출로 안 넘어감)
        }

        // 1. 순수 데미지 연산 (BattleCalculator)
        int attackerStrength = StatManager.Instance.GetEffectiveStat(isPlayerAttacking, TargetStat.Strength);
        int attackerDefense = StatManager.Instance.GetEffectiveStat(isPlayerAttacking, TargetStat.Defense);
        int attackerLuck = StatManager.Instance.GetEffectiveStat(isPlayerAttacking, TargetStat.Luck);
        int attackerSpeed = StatManager.Instance.GetEffectiveStat(isPlayerAttacking, TargetStat.Speed);

        int defenderDefense = StatManager.Instance.GetEffectiveStat(!isPlayerAttacking, TargetStat.Defense);
        int defenderSpeed = StatManager.Instance.GetEffectiveStat(!isPlayerAttacking, TargetStat.Speed);
        int defenderBR = StatManager.Instance.GetEffectiveStat(!isPlayerAttacking, TargetStat.BreakResistance);

        SkillResult skillResult = BattleCalculator.CalculateSkill(
            skill, isPlayerAttacking, currentPlayerStats, currentEnemyData,
            attackerStrength, attackerDefense, attackerLuck, attackerSpeed, defenderDefense, defenderSpeed, defenderBR
        );

        bool isPlayerDefending = !isPlayerAttacking;
        Sprite defenderHitSprite = isPlayerDefending ? playerData?.hit : currentEnemyData?.hit;
        Sprite defenderEvadeSprite = isPlayerDefending ? playerData?.evade : currentEnemyData?.evade;
        string attackerName = isPlayerAttacking ? (playerData != null ? GetTranslatedText(playerData.playerNamekey) : "주인공") : (currentEnemyData != null ? GetTranslatedText(currentEnemyData.enemyNameKey) : "적");
        string skillName = GetTranslatedText(skill.skillNameKey);

        // 2. 연출 대본 작성 (BattleVisualizer)
        if (isUltimate)
        {
            Sprite cutInSprite = isPlayerAttacking ? playerData?.cutIn : currentEnemyData?.CutIn;
            if (cutInSprite != null)
            {
                // [연출 추가] 컷인과 함께 이전 텍스트를 밀어버리고 필살기 대사를 즉시 출력!
                string ultText = $"{attackerName}의 필살기!";
                BattleVisualizer.Instance.EnqueueAction(() =>
                {
                    CombatUIManager.Instance.InterruptAndTypeCommentary(ultText);
                });

                BattleVisualizer.Instance.EnqueueCutIn(cutInSprite);
            }
        }

        // ==========================================
        // [템포 개선] 내 이미지 + 적 이미지 + 텍스트 출력을 하나로 묶어 '동시'에 실행합니다!
        // ==========================================
        string commentary = !skillResult.anyHit ? $"{attackerName}의 {skillName}이(가) 빗나갔습니다!" :
                            (skillResult.anyCrit ? $"{attackerName}의 {skillName} 치명적으로 적중!" : $"{attackerName}의 {skillName} 적중!");
        bool isPureUtility = skill.GetCurrentDamageMultiplier() <= 0f && !skill.forceHitReaction;

        BattleVisualizer.Instance.EnqueueAction(() =>
        {
            if (skill.skillLogic is SkillLogic_FantasticDreamer dreamLogic)
            {
                CombatUIManager.Instance.ShowFantasticDreamerDice(dreamLogic.LastRolledStage, isPlayerAttacking);
            }

            // 1. 내 이미지 변경
            CombatUIManager.Instance.SetCasterImage(isPlayerAttacking, skill.skillActionImage);
            skill.skillLogic?.PaySkillCost(skill, currentPlayerStats, currentEnemyData, isPlayerAttacking);
            CompanionManager.Instance.UpdateEmotion(skillResult.anyHit ?
                (isPlayerAttacking ? CompanionManager.Emotion.Happy : CompanionManager.Emotion.Worried) :
                (isPlayerAttacking ? CompanionManager.Emotion.Worried : CompanionManager.Emotion.Happy));

            // 2. 방어자 이미지 변경
            Sprite reactionSprite = null;
            if (skillResult.anyHit)
            {
                if (!isPureUtility) // 공격기일 때만 피격/가드 이미지를 설정!
                {
                    reactionSprite = skillResult.isGuardTriggered
                        ? (isPlayerAttacking ? currentEnemyData?.guardImage : playerData?.guardImage)
                        : (isPlayerAttacking ? currentEnemyData?.hit : playerData?.hit);
                }
            }
            else
            {
                if (!isPureUtility) // 공격기가 빗나갔을 때만 회피 이미지 설정!
                {
                    reactionSprite = isPlayerAttacking ? currentEnemyData?.evade : playerData?.evade;
                }
            }
            // reactionSprite가 null이면 UIManager는 원래(기본) 이미지를 그대로 유지합니다!
            CombatUIManager.Instance.SetDefenderImage(!isPlayerAttacking, reactionSprite);

            // 3. 텍스트가 대본을 멈추게 하지 않고, 백그라운드에서 타라락 쳐지게 합니다!
            CombatUIManager.Instance.InterruptAndTypeCommentary(commentary);

            // 4. [신규 추가] 여기서 대본 정지 없이 크리티컬 번쩍임만 백그라운드로 휙 던져줍니다!
            if (skillResult.anyCrit)
                CombatUIManager.Instance.StartCoroutine(CombatUIManager.Instance.ShowCritAlert());
        });

        bool isMorningStarApRecovered = false;
        bool hasRewardedCrit = false;
        foreach (var hit in skillResult.hits)
        {
            // 1. 명중/회피 기본 연출 대본
            BattleVisualizer.Instance.EnqueueAction(() =>
            {
                if (!hit.isHit)
                {
                    if (!isPureUtility)
                    {
                        // 1. 방송국에 회피를 알려 "Miss" 텍스트를 팝업시킵니다.
                        BattleEventSystem.CallEvaded(isPlayerDefending);

                        // 2. 즉시 방어자의 이미지를 '회피(Evade)' 이미지로 바꿉니다.
                        Sprite evadeSprite = isPlayerDefending ? playerData?.evade : currentEnemyData?.evade;
                        CombatUIManager.Instance.SetDefenderImage(!isPlayerAttacking, evadeSprite);

                        if (isPlayerDefending)
                        {
                            StyleRankManager.Instance.OnEvade();

                            // [진화 B] 난식 턴 당기기 (시각적 딜레이가 불필요하므로 즉시 처리)
                            var martialSkill = PlayerManager.Instance.unlockedSkills.Find(s => s.category == SkillCategory.Martial);
                            if (martialSkill != null && martialSkill.skillLogic is SkillLogic_MorningStar msLogic)
                            {
                                bool hasEvasionBuff = BuffManager.Instance.GetEffects(true).Exists(e => e.effectData == msLogic.evasionBuffData);
                                if (hasEvasionBuff && martialSkill.currentEvolution == SkillEvolution.PathB && !isMorningStarApRecovered)
                                {
                                    var playerEntity = TurnManager.Instance.turnQueue.Find(e => e.isPlayer);
                                    if (playerEntity != null)
                                    {
                                        playerEntity.actionGauge += msLogic.pathB_ApRecovery;
                                        isMorningStarApRecovered = true;
                                        DevLog.Log($"[새벽별:난식] 회피 성공! 행동 게이지 {msLogic.pathB_ApRecovery} 회복.");
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (hit.isCrit && isPlayerAttacking && !hasRewardedCrit) { StyleRankManager.Instance.OnCriticalHit(); hasRewardedCrit = true; }

                    if (isPlayerAttacking)
                    {
                        currentEnemyHp = Mathf.Max(0, currentEnemyHp - hit.damage);
                        BattleEventSystem.CallHpChanged(false, currentEnemyHp, currentEnemyData.maxHp);
                    }
                    else
                    {
                        currentPlayerStats.currentHp = Mathf.Max(0, currentPlayerStats.currentHp - hit.damage);
                        BattleEventSystem.CallHpChanged(true, currentPlayerStats.currentHp, currentPlayerStats.maxHp);
                        if (!skillResult.isGuardTriggered) StyleRankManager.Instance.OnPlayerHit();

                        if (isPlayerCharging && hit.damage > 0)
                        {
                            isPlayerCharging = false;
                            chargingSkill = null;
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
            });

            // 다단히트 간 기본 대기 시간
            BattleVisualizer.Instance.EnqueueDelay(0.15f);
        }

        int successCount = 0;
        foreach (var hit in skillResult.hits) { if (hit.isHit) successCount++; }
        lastSuccessfulHits = successCount;

        BattleVisualizer.Instance.EnqueueAction(() =>
        {
            skill.skillLogic?.ApplyEffectOnHit(skill, currentPlayerStats, currentEnemyData, isPlayerAttacking, skillResult.anyHit);
        });

        // 2. [신규] 루프 종료 후 카운터 반격 판정 및 연출
        bool isCounterTriggered = false;

        // 전부 회피(anyHit == false)했을 때 카운터 조건 체크
        if (!skillResult.anyHit && !isPureUtility && isPlayerDefending)
        {
            var martialSkill = PlayerManager.Instance.unlockedSkills.Find(s => s.category == SkillCategory.Martial);
            if (martialSkill != null && martialSkill.skillLogic is SkillLogic_MorningStar msLogic)
            {
                bool hasEvasionBuff = BuffManager.Instance.GetEffects(true).Exists(e => e.effectData == msLogic.evasionBuffData);
                if (hasEvasionBuff && martialSkill.currentEvolution == SkillEvolution.PathA)
                {
                    isCounterTriggered = true;
                    int levelIdx = Mathf.Clamp(martialSkill.skillLevel - 1, 0, msLogic.pathA_CounterRates.Length - 1);
                    int counterDmg = Mathf.RoundToInt(currentPlayerStats.strength * msLogic.pathA_CounterRates[levelIdx]);
                    Sprite counterImage = msLogic.GetCounterActionImage(martialSkill);

                    // 1단계: 회피 성공 이미지를 충분히 감상 (평소 턴 넘어가기 전 대기 시간 2.0f 적용)
                    BattleVisualizer.Instance.EnqueueDelay(2.0f);

                    // 2단계: 카운터 일격 작렬! (이미지 교체 및 적 피격 연출)
                    BattleVisualizer.Instance.EnqueueAction(() =>
                    {
                        currentEnemyHp = Mathf.Max(0, currentEnemyHp - counterDmg);

                        CombatUIManager.Instance.SetDefenderImage(true, counterImage); // 내 이미지 카운터로
                        CombatUIManager.Instance.SetDefenderImage(false, currentEnemyData?.hit); // 적 이미지 피격으로

                        CombatUIManager.Instance.enemyStatusUI.UpdateHP(currentEnemyHp, currentEnemyData.maxHp);
                        CombatUIManager.Instance.SpawnDamageText(counterDmg.ToString(), false, false);
                        DevLog.Log($"[새벽별:멸식] 카운터 발동! {counterDmg} 피해");
                    });

                    // 3단계: 카운터 타격감을 느낄 수 있게 대기 (스킬 적중 후 대기 시간과 동일하게 2.0f 적용)
                    BattleVisualizer.Instance.EnqueueDelay(2.0f);
                }
            }
        }

        // 카운터가 발동하지 않은 일반적인 상황(명중했거나 카운터가 없는 회피)에서의 대기
        if (!isCounterTriggered)
        {
            // 데미지 텍스트나 Miss를 감상할 수 있도록 표준 2.0초 대기
            BattleVisualizer.Instance.EnqueueDelay(2.0f);
        }

        // 가드 및 인과율(반사) 후처리
        if (skillResult.isGuardTriggered)
        {
            BattleVisualizer.Instance.EnqueueAction(() =>
            {
                StyleRankManager.Instance?.OnSupportActionUsed();
                BuffManager.Instance.ConsumeGuardEffect(true);
            });
        }

        if (isPlayerDefending && skillResult.isGuardTriggered)
        {
            float reflectRatio = PlayerManager.Instance.GetReflectRatio();

            if (reflectRatio > 0f)
            {
                int reflectDamage = Mathf.Max(1, Mathf.RoundToInt(skillResult.totalMitigatedDamage * reflectRatio));
                BattleVisualizer.Instance.EnqueueAction(() =>
                {
                    Sprite reflectSprite = playerData.reflectImage != null ? playerData.reflectImage : playerData.guardImage;
                    CombatUIManager.Instance.SetDefenderImage(true, reflectSprite);
                    CombatUIManager.Instance.SetDefenderImage(false, currentEnemyData.hit);

                    currentEnemyHp = Mathf.Max(0, currentEnemyHp - reflectDamage);
                    BattleEventSystem.CallHpChanged(false, currentEnemyHp, currentEnemyData.maxHp);
                    BattleEventSystem.CallDamageTaken(false, reflectDamage, false);

                    // 반사 텍스트도 동시 송출!
                    CombatUIManager.Instance.InterruptAndTypeCommentary($"[인과율 발동!] 튕겨낸 힘으로 적에게 {reflectDamage}의 고정 피해를 반사합니다!");
                });

                // 반사 데미지도 구경할 수 있게 대기
                BattleVisualizer.Instance.EnqueueDelay(2.0f);
            }
        }

        // 화면 리셋
        BattleVisualizer.Instance.EnqueueAction(() =>
        {
            CombatUIManager.Instance.ClearCombatEffects(); // 여기서 텍스트가 지워짐

            if (isPlayerAttacking)
            {
                StyleRankManager.Instance.OnSkillUsed(selectedCategory);
                StyleRankManager.Instance.ResetTurnState();
                if (isUltimate) StyleRankManager.Instance.ResetRankForUltimate();
            }

            if (!(isPlayerAttacking && isPlayerCharging))
            {
                CombatUIManager.Instance.ResetCasterImage(isPlayerAttacking);
            }

            bool isDefenderBroken = (!isPlayerAttacking && BreakManager.Instance.IsBroken(true)) || (isPlayerAttacking && BreakManager.Instance.IsBroken(false));

            if (!isDefenderBroken)
            {
                // 방어자(플레이어)가 기를 모으는 중이었다면, 기본 이미지가 아닌 [기 모으기 이미지]로 복구!
                if (isPlayerDefending && isPlayerCharging && chargingSkill != null)
                {
                    CombatUIManager.Instance.SetDefenderImage(true, chargingSkill.skillActionImage);
                }
                else
                {
                    // 일반적인 상황에선 기본 이미지로 리셋
                    CombatUIManager.Instance.ResetDefenderImage(isPlayerDefending);
                }
            }
            else
            {
                // [수정됨] 방어자가 아직 그로기 상태라면, ScriptableObject에 할당해둔 'breakImage'로 되돌려놓습니다!
                Sprite groggySprite = isPlayerDefending ? playerData?.breakImage : currentEnemyData?.breakImage;

                if (groggySprite != null)
                {
                    CombatUIManager.Instance.SetDefenderImage(isPlayerDefending, groggySprite);
                }

                DevLog.Log($"[{(isPlayerDefending ? "주인공" : "적")}]가 아직 그로기 상태이므로 전용 Break 이미지로 복구합니다.");
            }
        });

        // 3. 지휘관 권한 위임 및 턴 종료 대기
        BattleVisualizer.Instance.StartSequence(() =>
        {
            if (isPlayerAttacking && isUnleashingCharge)
            {
                isUnleashingCharge = false;
            }

            if (currentEnemyHp <= 0 || currentPlayerStats.currentHp <= 0) EndCombat(currentEnemyHp <= 0);
            else ResolveTurnEnd();
        });
    }

    public bool ApplyDamageToEnemy(int damage)
    {
        currentEnemyHp = Mathf.Max(0, currentEnemyHp - damage);
        BattleEventSystem.CallHpChanged(false, currentEnemyHp, currentEnemyData.maxHp);
        return currentEnemyHp <= 0;
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

            foreach (var eff in effects)
                if (eff.effectData.specialType == SpecialEffectType.HpRegen) hpRegenRate += eff.value;

            if (hpRegenRate > 0f)
            {
                if (isPlayerTurn)
                {
                    int healAmount = Mathf.RoundToInt(currentPlayerStats.maxHp * hpRegenRate);
                    currentPlayerStats.currentHp = Mathf.Clamp(currentPlayerStats.currentHp + healAmount, 0, currentPlayerStats.maxHp);
                    CombatUIManager.Instance.playerStatusUI.UpdateHP(currentPlayerStats.currentHp, currentPlayerStats.maxHp);
                    CombatUIManager.Instance.SpawnDamageText($"+{healAmount}", false, true);
                    DevLog.Log($"[아발론] 턴 종료! 셰리의 체력이 {healAmount} 회복되었습니다.");
                }
                else
                {
                    int healAmount = Mathf.RoundToInt(currentEnemyData.maxHp * hpRegenRate);
                    currentEnemyHp = Mathf.Clamp(currentEnemyHp + healAmount, 0, currentEnemyData.maxHp);
                    CombatUIManager.Instance.enemyStatusUI.UpdateHP(currentEnemyHp, currentEnemyData.maxHp);
                    CombatUIManager.Instance.SpawnDamageText($"+{healAmount}", false, false);
                }
            }

            if (currentActiveEntity.isPlayer) BuffManager.Instance.UpdateEffectsOnTurnEnd(true);
            else if (currentActiveEntity.type == EntityType.Enemy) BuffManager.Instance.UpdateEffectsOnTurnEnd(false);
        }

        CalculateNextTurn();
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