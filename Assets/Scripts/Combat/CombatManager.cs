using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance;

    [Header("데이터 연결")]
    public PlayerData playerData;
    public KarinData karinData;

    private PlayerStats currentPlayerStats;
    private EnemyData currentEnemyData;
    private int currentEnemyHp;
    private float currentPlayerBreak = 0;
    private float currentEnemyBreak = 0;
    private int enemyTurnCount = 0;

    private bool isEnemyBroken = false;
    private bool isPlayerBroken = false;

    private int playerHpAtTurnStart;
    private int enemyHpAtTurnStart;
    private TurnEntity currentActiveEntity;

    private enum MenuState { Hidden, CategorySelect, SkillSelect }
    private MenuState currentMenuState = MenuState.Hidden;
    private string[] categoryKeys = { "cat_sword", "cat_gun", "cat_martial", "cat_magic", "cat_oni" };
    private SkillCategory selectedCategory;
    private List<SkillData> currentDisplaySkills;

    private struct HitResult
    {
        public bool isHit;
        public bool isCrit;
        public int damage;
        public float breakDamage;
    }

    public enum CompanionEmotion { Normal, Happy, Worried }

    private void UpdateCompanionEmotion(CompanionEmotion emotion)
    {
        Sprite kSprite = null;
        if (karinData != null)
        {
            switch (emotion)
            {
                case CompanionEmotion.Normal: kSprite = karinData.normal; break;
                case CompanionEmotion.Happy: kSprite = karinData.happy; break;
                case CompanionEmotion.Worried: kSprite = karinData.worried; break;
            }
        }

        Sprite sSprite = null;
        SupporterData supData = PlayerManager.Instance != null ? PlayerManager.Instance.activeSupporter : null;
        if (supData != null)
        {
            switch (emotion)
            {
                case CompanionEmotion.Normal: sSprite = supData.mainImage; break;
                case CompanionEmotion.Happy: sSprite = supData.happy; break;
                case CompanionEmotion.Worried: sSprite = supData.worried; break;
            }
        }

        CombatUIManager.Instance.UpdateProfileImages(kSprite, sSprite);
    }

    // ==========================================
    // 1. 버프/상태이상 통합 관리 시스템
    // ==========================================
    public class ActiveEffect
    {
        public StatusEffectData effectData; // 이 효과의 모든 룰(Rule)과 아이콘
        public float value;                 // 위력 (20%면 0.2f, 10고정치면 10f)
        public int turnsLeft;               // 남은 턴 수
        public bool isNewlyApplied;
    }

    private List<ActiveEffect> playerEffects = new List<ActiveEffect>();
    private List<ActiveEffect> enemyEffects = new List<ActiveEffect>();

    public List<ActiveEffect> GetPlayerEffects() { return playerEffects; }
    public List<ActiveEffect> GetEnemyEffects() { return enemyEffects; }

    public bool IsPlayerSelectingPhase => currentMenuState == MenuState.CategorySelect || currentMenuState == MenuState.SkillSelect;

    // UI 출력을 위해 같은 SO를 공유하는 효과들의 수치를 합쳐주는 함수
    public Dictionary<StatusEffectData, float> GetGroupedEffects(bool isPlayer)
    {
        var list = isPlayer ? playerEffects : enemyEffects;
        var grouped = new Dictionary<StatusEffectData, float>();

        foreach (var effect in list)
        {
            if (grouped.ContainsKey(effect.effectData)) grouped[effect.effectData] += effect.value;
            else grouped.Add(effect.effectData, effect.value);
        }
        return grouped;
    }

    // [통합된 핵심 함수] 스킬/아이템에서 효과를 부여할 때 무조건 이 함수를 호출합니다!
    public void AddEffect(bool isPlayer, StatusEffectData data, float value, int turns)
    {
        var list = isPlayer ? playerEffects : enemyEffects;

        bool isSelfBuff = false;
        if (currentActiveEntity != null)
        {
            // 1. 아군(셰리)에게 들어온 버프인데, 지금 턴의 주인도 셰리(isPlayer)라면 -> 셀프 버프!
            if (isPlayer && currentActiveEntity.isPlayer)
            {
                isSelfBuff = true;
            }
            // 2. 적에게 들어온 버프(or 회복)인데, 지금 턴의 주인도 적이라면 -> 셀프 버프!
            else if (!isPlayer && currentActiveEntity.entityName == "Enemy")
            {
                isSelfBuff = true;
            }
            // (카린이나 조력자가 셰리에게 걸어준 경우는 위 조건에 맞지 않으므로 false가 됩니다)
        }
        list.Add(new ActiveEffect { effectData = data, value = value, turnsLeft = turns, isNewlyApplied = isSelfBuff });

        DevLog.Log($"[효과 부여] {(isPlayer ? "아군" : "적")}에게 {data.effectName} 적용! (수치: {value}, {turns}턴)");
        CombatUIManager.Instance.RefreshBuffUI();
    }

    //  실제 전투 계산 시, 모든 버프 리스트를 순회하며 최종 스탯을 산출합니다.
    private int GetTotalStat(bool isPlayer, TargetStat statType, int baseStat)
    {
        var list = isPlayer ? playerEffects : enemyEffects;
        float percentageSum = 0f;
        float flatSum = 0f;

        foreach (var effect in list)
        {
            // 이 효과가 우리가 찾고 있는 스탯(예: 힘)을 올려주는 효과라면?
            if (effect.effectData.targetStat == statType)
            {
                if (effect.effectData.modifierType == ModifierType.Percentage)
                    percentageSum += effect.value; // 퍼센트끼리 더하기 (예: 20% + 50% = 70%)
                else
                    flatSum += effect.value;       // 고정치끼리 더하기
            }
        }

        // 복리 연산 방지: (기본스탯 * (1 + 퍼센트총합)) + 고정치총합
        float finalValue = (baseStat * (1f + percentageSum)) + flatSum;
        return Mathf.RoundToInt(finalValue);
    }

    // 턴이 '종료'될 때 지속시간을 깎고 만료된 것을 지웁니다.
    private void UpdateEffectsOnTurnEnd(bool isPlayer)
    {
        var list = isPlayer ? playerEffects : enemyEffects;
        bool hasChanged = false;

        for (int i = list.Count - 1; i >= 0; i--)
        {
            if (list[i].isNewlyApplied)
            {
                list[i].isNewlyApplied = false;
                continue;
            }
            list[i].turnsLeft--;
            hasChanged = true;
            if (list[i].turnsLeft <= 0)
            {
                list.RemoveAt(i);
                hasChanged = true;
            }
        }

        if (hasChanged) CombatUIManager.Instance.RefreshBuffUI();
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

    // ==========================================
    // 전투 개전 템포 조절
    // ==========================================
    private IEnumerator CombatStartPhaseRoutine()
    {
        string eName = currentEnemyData != null ? GetTranslatedText(currentEnemyData.enemyNameKey) : "적";

        // 1. 조우 텍스트 출력 및 대기 (너무 급하지 않게 템포 조절)
        yield return StartCoroutine(CombatUIManager.Instance.TypeCommentary($"{eName} 조우!", true, 0.8f));
        yield return new WaitForSeconds(1.0f); // 텍스트를 다 읽을 수 있는 텀

        // 2. 적 개전 스킬 우선 처리 (추후 구현)

        // 3. 조력자 개전 스킬 처리
        SupporterData activeSup = PlayerManager.Instance.activeSupporter;
        if (activeSup != null && activeSup.startSkillLogic != null)
        {
            yield return StartCoroutine(PerformSupporterSkillRoutine(activeSup, true));
        }

        UpdateCompanionEmotion(CompanionEmotion.Normal);
        CalculateNextTurn();
    }

    private void SetupCombatScene()
    {
        if (PlayerManager.Instance != null)
        {
            // [수정됨] 원본을 오염시키지 않도록 .Clone()으로 복제본을 떠서 전투에 사용합니다!
            currentPlayerStats = PlayerManager.Instance.stats.Clone();
            currentEnemyData = PlayerManager.Instance.currentEnemyToFight;
        }

        if (currentPlayerStats != null && playerData != null)
            CombatUIManager.Instance.InitPlayerUI(currentPlayerStats.maxHp, currentPlayerStats.currentHp, playerData.normal);

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
        isEnemyBroken = false;
        isPlayerBroken = false;
        playerEffects.Clear();
        enemyEffects.Clear();

        CombatUIManager.Instance.SetActionPanelActive(false);
        CombatUIManager.Instance.SetWaitingPanelActive(true);
        currentMenuState = MenuState.Hidden;

        if (StyleRankManager.Instance != null) StyleRankManager.Instance.InitCombat();
    }

    private void InitializeTurnQueue()
    {
        TurnManager.Instance.ClearQueue();

        if (playerData != null)
            TurnManager.Instance.AddEntity("Player", currentPlayerStats.ActionPoints, true, 1.0f, playerData.cutIn);

        if (karinData != null && PlayerManager.Instance.equippedKarinItem != null)
            TurnManager.Instance.AddEntity("Karin", currentPlayerStats.ActionPoints, false, 0.333f, karinData.CutIn);

        if (PlayerManager.Instance.activeSupporter != null)
            TurnManager.Instance.AddEntity("Supporter", currentPlayerStats.ActionPoints, false, 0.2f, PlayerManager.Instance.activeSupporter.CutIn);

        if (currentEnemyData != null)
            TurnManager.Instance.AddEntity("Enemy", currentEnemyData.ActionPoints, false, 1.0f, currentEnemyData.CutIn);

        UpdateTurnOrderUI();
    }

    public void CalculateNextTurn()
    {
        TurnEntity nextTurnEntity = TurnManager.Instance.CalculateAndGetNextTurn();
        UpdateTurnOrderUI();
        DevLog.Log($"[턴 알림] {nextTurnEntity.entityName}의 턴!");
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

        if (currentTurnOwner.entityName == "Enemy")
        {
            CombatUIManager.Instance.SetActionPanelActive(false);
            CombatUIManager.Instance.SetWaitingPanelActive(true);
            currentMenuState = MenuState.Hidden;

            if (isEnemyBroken)
            {
                yield return StartCoroutine(CombatUIManager.Instance.TypeCommentary($"{eName}이(가) 그로기 상태에서 정신을 차렸습니다."));
                isEnemyBroken = false;
                currentEnemyBreak = 0f;
                CombatUIManager.Instance.UpdateEnemyBreak(0f);
                CombatUIManager.Instance.ResetDefenderImage(false);
                ResolveTurnEnd();
                yield break;
            }
            yield return StartCoroutine(CombatUIManager.Instance.TypeCommentary($"{eName}의 차례입니다! 대비해주세요!"));
            StartCoroutine(EnemyTurnRoutine());
        }
        else if (currentTurnOwner.isPlayer)
        {
            if (isPlayerBroken)
            {
                yield return StartCoroutine(CombatUIManager.Instance.TypeCommentary($"{pName}이(가) 그로기 상태에서 정신을 차렸습니다."));
                isPlayerBroken = false;
                currentPlayerBreak = 0f;
                CombatUIManager.Instance.UpdatePlayerBreak(0f);
                CombatUIManager.Instance.ResetCasterImage(true);
                ResolveTurnEnd();
                yield break;
            }
            CombatUIManager.Instance.SetWaitingPanelActive(false);
            ShowCategoryMenu();
            StartCoroutine(CombatUIManager.Instance.TypeCommentary($"{pName}, 무슨 공격을 할까요?", false));
        }
        else if (currentTurnOwner.entityName == "Karin")
        {
            CombatUIManager.Instance.SetActionPanelActive(false);
            CombatUIManager.Instance.SetWaitingPanelActive(true);
            currentMenuState = MenuState.Hidden;
            StartCoroutine(KarinTurnRoutine());
        }
        else if (currentTurnOwner.entityName == "Supporter")
        {
            CombatUIManager.Instance.SetActionPanelActive(false);
            CombatUIManager.Instance.SetWaitingPanelActive(true);
            currentMenuState = MenuState.Hidden;

            SupporterData activeSup = PlayerManager.Instance.activeSupporter;
            if (activeSup != null && activeSup.battleSkillLogic != null)
            {
                yield return StartCoroutine(PerformSupporterSkillRoutine(activeSup, false));
            }
            else
            {
                ResolveTurnEnd();
            }
        }
    }

    private IEnumerator EnemyTurnRoutine()
    {
        yield return new WaitForSeconds(1.0f);
        SkillData skillToUse = null;

        if (currentEnemyData != null && currentEnemyData.aiBrain != null)
        {
            skillToUse = currentEnemyData.aiBrain.DecideNextSkill(enemyTurnCount, currentPlayerStats, currentEnemyData);
            enemyTurnCount++;
        }

        if (skillToUse != null) StartCoroutine(PerformSkillRoutine(skillToUse, false, skillToUse.isUltimate));
        else { DevLog.LogError("적 스킬 패턴빔!"); ResolveTurnEnd(); }
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
        StartCoroutine(PerformSkillRoutine(skill, isPlayerAttacking, isUltimate));
    }

    private IEnumerator PerformSkillRoutine(SkillData skill, bool isPlayerAttacking, bool isUltimate = false)
    {
        if (isUltimate)
        {
            Sprite cutInSprite = isPlayerAttacking ? (playerData != null ? playerData.cutIn : null) : (currentEnemyData != null ? currentEnemyData.CutIn : null);
            if (cutInSprite != null) yield return StartCoroutine(CombatUIManager.Instance.ShowCutIn(cutInSprite));
        }

        // 버프가 적용된 실제 스탯을 가져와서 계산합니다!
        int attackerSpeed = isPlayerAttacking
            ? GetTotalStat(true, TargetStat.Speed, currentPlayerStats.speed)
            : GetTotalStat(false, TargetStat.Speed, currentEnemyData.speed);

        int defenderSpeed = isPlayerAttacking
            ? GetTotalStat(false, TargetStat.Speed, currentEnemyData.speed)
            : GetTotalStat(true, TargetStat.Speed, currentPlayerStats.speed);

        int attackerStrength = isPlayerAttacking
            ? GetTotalStat(true, TargetStat.Strength, currentPlayerStats.strength)
            : GetTotalStat(false, TargetStat.Strength, currentEnemyData.strength);

        int attackerLuck = isPlayerAttacking
            ? GetTotalStat(true, TargetStat.Luck, currentPlayerStats.luck)
            : GetTotalStat(false, TargetStat.Luck, currentEnemyData.luck);

        int defenderDefense = isPlayerAttacking
            ? GetTotalStat(false, TargetStat.Defense, currentEnemyData.defense)
            : GetTotalStat(true, TargetStat.Defense, currentPlayerStats.defense);

        int defenderBR = isPlayerAttacking ? currentEnemyData.breakResistance : currentPlayerStats.breakResistance;

        bool isPlayerDefending = !isPlayerAttacking;
        Sprite defenderHitSprite = isPlayerDefending ? (playerData != null ? playerData.hit : null) : (currentEnemyData != null ? currentEnemyData.hit : null);
        Sprite defenderEvadeSprite = isPlayerDefending ? (playerData != null ? playerData.evade : null) : (currentEnemyData != null ? currentEnemyData.evade : null);

        string attackerName = isPlayerAttacking ? (playerData != null ? GetTranslatedText(playerData.playerNamekey) : "주인공") : (currentEnemyData != null ? GetTranslatedText(currentEnemyData.enemyNameKey) : "적");
        string skillName = GetTranslatedText(skill.skillNameKey);

        if (isPlayerAttacking) StyleRankManager.Instance.OnSkillUsed(selectedCategory);

        int totalHits = skill.hitCount > 0 ? skill.hitCount : 1;
        bool anyCrit = false;
        bool anyHit = false;

        List<HitResult> hitResults = new List<HitResult>();
        bool isGuardTriggered = false;
        for (int i = 0; i < totalHits; i++)
        {
            HitResult result = new HitResult();
            result.isHit = CombatMath.CheckHitSuccess(skill.baseAccuracy, attackerSpeed, defenderSpeed);

            if (result.isHit)
            {
                anyHit = true;
                float calculatedDamage = attackerStrength * skill.GetCurrentDamageMultiplier();
                if (skill.skillLogic != null) calculatedDamage *= skill.skillLogic.GetDamageMultiplier(currentPlayerStats, currentEnemyData, isPlayerAttacking);

                if (isPlayerAttacking) calculatedDamage *= StyleRankManager.Instance.GetRankDamageMultiplier();

                if (!isPlayerAttacking && isPlayerBroken) calculatedDamage *= 2.0f;
                else if (isPlayerAttacking && isEnemyBroken) calculatedDamage *= 2.0f;

                result.isCrit = CombatMath.CheckCriticalSuccess(skill.bonusCritRate, attackerLuck);
                if (result.isCrit) { calculatedDamage *= 1.5f; anyCrit = true; }

                calculatedDamage *= (1f - CombatMath.GetDamageReduction(defenderDefense));
                result.damage = Mathf.RoundToInt(calculatedDamage);
                if (result.damage <= 0) result.damage = 1;

                if (isPlayerDefending)
                {
                    // 현재 플레이어에게 가드(SpecialType.Guard) 버프가 있는지 확인
                    var guardEffect = playerEffects.Find(e => e.effectData.specialType == SpecialEffectType.Guard);

                    if (guardEffect != null && result.damage > 0)
                    {
                        result.damage = Mathf.RoundToInt(result.damage * 0.5f); // 피해 50% 감소
                        result.breakDamage = 0f; // [추가] 가드 중일 때는 브레이크 수치가 쌓이지 않음!
                        isGuardTriggered = true; // 가드가 한 번이라도 발동했음을 기록
                    }
                }

                if (isPlayerAttacking && !isEnemyBroken)
                {
                    result.breakDamage = skill.GetCurrentBreakPower() * (skill.skillLogic != null ? skill.skillLogic.GetBreakMultiplier(currentPlayerStats, currentEnemyData, isPlayerAttacking) : 1f);
                    result.breakDamage *= (1f - CombatMath.GetBreakDamageReduction(defenderBR));
                }
                else if (!isPlayerAttacking && !isPlayerBroken)
                {
                    result.breakDamage = skill.GetCurrentBreakPower() * (skill.skillLogic != null ? skill.skillLogic.GetBreakMultiplier(currentPlayerStats, currentEnemyData, isPlayerAttacking) : 1f);
                    result.breakDamage *= (1f - CombatMath.GetBreakDamageReduction(defenderBR));
                }
                else result.breakDamage = 0f;
            }
            hitResults.Add(result);
        }

        CombatUIManager.Instance.SetCasterImage(isPlayerAttacking, skill.skillActionImage);

        if (isPlayerAttacking)
        {
            // 아군 공격: 맞췄으면 Happy, 빗나갔으면 Worried
            UpdateCompanionEmotion(anyHit ? CompanionEmotion.Happy : CompanionEmotion.Worried);
        }
        else
        {
            // 적 공격: 맞았으면 Worried, 피했으면 Happy
            UpdateCompanionEmotion(anyHit ? CompanionEmotion.Worried : CompanionEmotion.Happy);
        }

        string commentaryText = "";
        if (!anyHit) commentaryText = $"{attackerName}의 {skillName}이(가) 빗나갔습니다!";
        else if (anyCrit) commentaryText = $"{attackerName}의 {skillName} 치명적으로 적중!";
        else commentaryText = $"{attackerName}의 {skillName} 적중!";

        Coroutine textCoroutine = StartCoroutine(CombatUIManager.Instance.TypeCommentary(commentaryText));

        if (anyHit)
        {
            if (isGuardTriggered)
            {
                // 1. 방어 성공: 가드 전용 이미지 출력
                Sprite guardSprite = playerData.guardImage;

                // (만약 에디터에서 가드 이미지를 깜빡하고 안 넣었다면, 튕기지 않게 기본 피격 이미지로 대체)
                if (guardSprite == null) guardSprite = defenderHitSprite;

                CombatUIManager.Instance.SetDefenderImage(isPlayerDefending, guardSprite);
            }
            else
            {
                // 2. 일반 피격 (정타 맞음)
                CombatUIManager.Instance.SetDefenderImage(isPlayerDefending, defenderHitSprite);
            }
        }
        else
        {
            // 3. 완전 회피 (Miss)
            CombatUIManager.Instance.SetDefenderImage(isPlayerDefending, defenderEvadeSprite);
            if (!isPlayerAttacking) StyleRankManager.Instance.OnEvade();
        }

        if (anyCrit) StartCoroutine(CombatUIManager.Instance.ShowCritAlert());

        bool hasRewardedCrit = false;

        foreach (var hit in hitResults)
        {
            if (!hit.isHit) CombatUIManager.Instance.SpawnDamageText("Miss", false, isPlayerDefending);
            else
            {
                if (hit.isCrit && isPlayerAttacking && !hasRewardedCrit) { StyleRankManager.Instance.OnCriticalHit(); hasRewardedCrit = true; }

                if (isPlayerAttacking)
                {
                    currentEnemyHp = Mathf.Max(0, currentEnemyHp - hit.damage);
                    CombatUIManager.Instance.UpdateEnemyHP(currentEnemyHp, currentEnemyData.maxHp);
                }
                else
                {
                    currentPlayerStats.currentHp = Mathf.Max(0, currentPlayerStats.currentHp - hit.damage);
                    CombatUIManager.Instance.UpdatePlayerHP(currentPlayerStats.currentHp, currentPlayerStats.maxHp);
                    if (!isGuardTriggered)
                    {
                        StyleRankManager.Instance.OnPlayerHit();
                    }
                }

                if (isPlayerAttacking && !isEnemyBroken)
                {
                    currentEnemyBreak += hit.breakDamage * CombatMath.GetBreakSnowballMultiplier(currentEnemyBreak);
                    if (currentEnemyBreak >= 100f)
                    {
                        currentEnemyBreak = 100f;
                        isEnemyBroken = true;
                        if (currentEnemyData.breakImage != null) CombatUIManager.Instance.SetDefenderImage(false, currentEnemyData.breakImage);
                        TurnManager.Instance.ResetGauge("Enemy");
                        UpdateTurnOrderUI();
                        StyleRankManager.Instance.OnEnemyBreak();
                    }
                    CombatUIManager.Instance.UpdateEnemyBreak(currentEnemyBreak);
                }
                else if (!isPlayerAttacking && !isPlayerBroken)
                {
                    currentPlayerBreak += hit.breakDamage * CombatMath.GetBreakSnowballMultiplier(currentPlayerBreak);
                    if (currentPlayerBreak >= 100f)
                    {
                        currentPlayerBreak = 100f;
                        isPlayerBroken = true;
                        if (playerData != null && playerData.breakImage != null) CombatUIManager.Instance.SetDefenderImage(true, playerData.breakImage);
                        TurnManager.Instance.ResetGauge("Player");
                        UpdateTurnOrderUI();
                    }
                    CombatUIManager.Instance.UpdatePlayerBreak(currentPlayerBreak);
                }

                CombatUIManager.Instance.SpawnDamageText(hit.damage.ToString(), hit.isCrit, isPlayerDefending);
            }
            yield return new WaitForSeconds(0.2f);
        }

        yield return textCoroutine;

        if (isGuardTriggered)
        {
            // 1. 스타일 랭크 딱 1번만 상승!
            if (StyleRankManager.Instance != null) StyleRankManager.Instance.OnSupportActionUsed();

            // 2. 가드 버프 중 '남은 턴수가 가장 적은 것' 딱 1개만 찾아서 소모!
            var guardEffects = playerEffects.FindAll(e => e.effectData.specialType == SpecialEffectType.Guard);
            if (guardEffects.Count > 0)
            {
                // 남은 턴수(turnsLeft)를 기준으로 오름차순 정렬 (가장 적게 남은 게 0번 인덱스에 옴)
                guardEffects.Sort((a, b) => a.turnsLeft.CompareTo(b.turnsLeft));
                playerEffects.Remove(guardEffects[0]); // 가장 턴이 적은 1개만 쏙 뺍니다!
            }

            CombatUIManager.Instance.RefreshBuffUI(); // UI 즉시 갱신
        }

        if (skill.skillLogic != null) skill.skillLogic.ApplyEffect(currentPlayerStats, currentEnemyData, isPlayerAttacking);

        if (isPlayerAttacking)
        {
            StyleRankManager.Instance.ResetTurnState();
            if (isUltimate) StyleRankManager.Instance.ResetRankForUltimate();
        }

        CombatUIManager.Instance.ClearCombatEffects();
        CombatUIManager.Instance.ResetCasterImage(isPlayerAttacking);

        if (!(!isPlayerAttacking && isPlayerBroken) && !(isPlayerAttacking && isEnemyBroken))
            CombatUIManager.Instance.ResetDefenderImage(isPlayerDefending);

        // 전투가 끝났을 때 승리 함수 호출!
        if (currentEnemyHp == 0 || currentPlayerStats.currentHp == 0)
        {
            EndCombat(currentEnemyHp == 0);
        }
        else
        {
            ResolveTurnEnd(); // 이 함수 안에서 버프를 깎고 CalculateNextTurn을 부릅니다.
        }
    }

    private IEnumerator KarinTurnRoutine()
    {
        DevLog.Log("카린의 턴입니다!");
        yield return new WaitForSeconds(1.0f);

        KarinItemData equippedItem = PlayerManager.Instance.equippedKarinItem;
        if (equippedItem == null)
        {
            yield return StartCoroutine(CombatUIManager.Instance.TypeCommentary("카린: \"어라? 쓸 수 있는 물건이 없네!\""));
            ResolveTurnEnd();
            yield break;
        }

        yield return StartCoroutine(PerformKarinItemRoutine(equippedItem));
    }

    private IEnumerator PerformKarinItemRoutine(KarinItemData item)
    {
        if (karinData != null && karinData.CutIn != null) yield return StartCoroutine(CombatUIManager.Instance.ShowCutIn(karinData.CutIn));
        if (karinData != null && karinData.ready != null) CombatUIManager.Instance.SetCasterImage(true, karinData.ready);

        string itemName = GetTranslatedText(item.itemName);
        Coroutine textCoroutine = StartCoroutine(CombatUIManager.Instance.TypeCommentary($"카린이 {itemName}을(를) 사용했습니다!"));

        int damage = 0;
        if (item.itemLogic != null)
        {
            damage = item.itemLogic.CalculateDamage(currentPlayerStats, currentEnemyData);
            item.itemLogic.ApplyEffect(currentPlayerStats, currentEnemyData);
        }

        UpdateCompanionEmotion(CompanionEmotion.Happy);
        if (StyleRankManager.Instance != null) StyleRankManager.Instance.OnSupportActionUsed();

        bool isCrit = false;
        bool isPlayerDefending = false;
        Sprite defenderHitSprite = currentEnemyData != null ? currentEnemyData.hit : null;

        if (damage > 0)
        {
            CombatUIManager.Instance.SetDefenderImage(isPlayerDefending, defenderHitSprite);
            currentEnemyHp = Mathf.Max(0, currentEnemyHp - damage);
            CombatUIManager.Instance.UpdateEnemyHP(currentEnemyHp, currentEnemyData.maxHp);
            CombatUIManager.Instance.SpawnDamageText(damage.ToString(), isCrit, isPlayerDefending);
        }

        yield return new WaitForSeconds(0.5f);
        yield return textCoroutine;

        CombatUIManager.Instance.ClearCombatEffects();
        CombatUIManager.Instance.ResetCasterImage(true);
        if (damage > 0) CombatUIManager.Instance.ResetDefenderImage(isPlayerDefending);

        if (currentEnemyHp == 0) EndCombat(true);
        else ResolveTurnEnd();
    }

    private IEnumerator PerformSupporterSkillRoutine(SupporterData supporter, bool isStartSkill)
    {
        Sprite cutIn = (isStartSkill && supporter.startSkillCutIn != null) ? supporter.startSkillCutIn : supporter.CutIn;
        if (cutIn != null) yield return StartCoroutine(CombatUIManager.Instance.ShowCutIn(cutIn));

        Sprite actionImage = isStartSkill ? supporter.startSkillImage : supporter.battleSkillImage;
        if (actionImage != null) CombatUIManager.Instance.SetCasterImage(true, actionImage);

        string skillType = isStartSkill ? "개전 스킬" : "전투 스킬";
        string supName = GetTranslatedText(supporter.supporterName);
        Coroutine textCoroutine = StartCoroutine(CombatUIManager.Instance.TypeCommentary($"{supName}의 {skillType} 발동!"));

        SupporterLogicBase logic = isStartSkill ? supporter.startSkillLogic : supporter.battleSkillLogic;
        int damage = 0;

        if (logic != null)
        {
            damage = logic.CalculateDamage(currentPlayerStats, currentEnemyData);
            logic.ApplyEffect(currentPlayerStats, currentEnemyData); // 여기서 셰리에게 버프 부여!
        }

        UpdateCompanionEmotion(CompanionEmotion.Happy);
        if (!isStartSkill && StyleRankManager.Instance != null) StyleRankManager.Instance.OnSupportActionUsed();

        bool isPlayerDefending = false;
        Sprite defenderHitSprite = currentEnemyData != null ? currentEnemyData.hit : null;

        if (damage > 0)
        {
            CombatUIManager.Instance.SetDefenderImage(isPlayerDefending, defenderHitSprite);
            currentEnemyHp = Mathf.Max(0, currentEnemyHp - damage);
            CombatUIManager.Instance.UpdateEnemyHP(currentEnemyHp, currentEnemyData.maxHp);
            CombatUIManager.Instance.SpawnDamageText(damage.ToString(), false, isPlayerDefending);
        }

        yield return new WaitForSeconds(0.5f);
        yield return textCoroutine;

        CombatUIManager.Instance.ClearCombatEffects();
        CombatUIManager.Instance.ResetCasterImage(true);
        if (damage > 0) CombatUIManager.Instance.ResetDefenderImage(isPlayerDefending);

        if (!isStartSkill)
        {
            if (currentEnemyHp == 0) EndCombat(true);
            else ResolveTurnEnd();
        }
    }

    // ==========================================
    // 전투 종료 시 원본 스탯 업데이트
    // ==========================================
    private void EndCombat(bool isWin)
    {
        DevLog.Log(isWin ? "--- 전투 승리! ---" : "--- 전투 패배... ---");

        // 전투가 끝나면 임시 스테이터스의 '잔여 HP'만 원본에 덮어씌워서 저장합니다!
        // (버프된 힘, 방어력 등은 전부 복제본에만 남고 파괴되므로 원상복구됩니다.)
        if (PlayerManager.Instance != null && currentPlayerStats != null)
        {
            PlayerManager.Instance.stats.currentHp = currentPlayerStats.currentHp;
        }

        // TODO: 경험치 및 골드 보상 획득 로직, 화면 씬 전환 등 처리
    }

    private string GetTranslatedText(string key)
    {
        if (string.IsNullOrEmpty(key)) return "";
        if (LocalizationManager.Instance != null) return LocalizationManager.Instance.GetText(key);
        return key;
    }

    private void ResolveTurnEnd()
    {
        UpdateCompanionEmotion(CompanionEmotion.Normal);
        // 1. 체력이 깎였는지 검사 (시작 체력보다 현재 체력이 낮으면 이번 턴에 맞은 것!)
        bool playerTookDamage = currentPlayerStats.currentHp < playerHpAtTurnStart;
        bool enemyTookDamage = currentEnemyHp < enemyHpAtTurnStart;

        // 2. 플레이어 그로기 회복 (피해를 안 받았고, 이미 그로기 상태가 아니며, 게이지가 조금이라도 쌓여있을 때)
        if (!playerTookDamage && !isPlayerBroken && currentPlayerBreak > 0f)
        {
            float recovery = CombatMath.GetBreakRecoveryAmount(currentPlayerBreak);
            currentPlayerBreak = Mathf.Max(0f, currentPlayerBreak - recovery);
            CombatUIManager.Instance.UpdatePlayerBreak(currentPlayerBreak);
            DevLog.Log($"[그로기 회복] 셰리: -{recovery:F1} (현재: {currentPlayerBreak:F1})");
        }

        // 3. 적 그로기 회복
        if (!enemyTookDamage && !isEnemyBroken && currentEnemyBreak > 0f)
        {
            float recovery = CombatMath.GetBreakRecoveryAmount(currentEnemyBreak);
            currentEnemyBreak = Mathf.Max(0f, currentEnemyBreak - recovery);
            CombatUIManager.Instance.UpdateEnemyBreak(currentEnemyBreak);
            DevLog.Log($"[그로기 회복] 적: -{recovery:F1} (현재: {currentEnemyBreak:F1})");
        }

        if (currentActiveEntity != null)
        {
            if (currentActiveEntity.isPlayer)
            {
                UpdateEffectsOnTurnEnd(true);  // 플레이어 턴이 끝났을 때만 플레이어 버프 감소
            }
            else if (currentActiveEntity.entityName == "Enemy")
            {
                UpdateEffectsOnTurnEnd(false); // 적 턴이 끝났을 때만 적 버프 감소
            }
            // 카린이나 조력자의 턴이 끝났을 때는 아무것도 깎지 않고 유지됩니다!
        }

        // 4. 회복 정산이 끝났으므로 다음 턴으로 넘깁니다!
        CalculateNextTurn();
    }
}