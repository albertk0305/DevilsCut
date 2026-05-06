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

    // ==========================================
    // 버프 시스템 변수
    // ==========================================
    public enum BuffStat { Strength, Defense, Speed, Luck }
    public enum BuffType { Flat, Percentage } // 고정값인지 퍼센트인지 구분

    public enum SpecialEffect { None, Guard, Reflect, Poison }

    public class SpecialEffectInfo
    {
        public SpecialEffect effect;
        public int turnsLeft;
    }

    private List<SpecialEffectInfo> playerSpecialEffects = new List<SpecialEffectInfo>();

    public class BuffInfo
    {
        public BuffStat stat;
        public BuffType type; 
        public float value;   // int에서 float로 변경 (퍼센트 계산 용도)
        public int turnsLeft;
    }

    private List<BuffInfo> playerBuffs = new List<BuffInfo>();

    // 외부(스킬 로직)에서 버프를 추가할 때 호출하는 함수
    public void AddPlayerBuff(BuffStat stat, BuffType type, float value, int turns)
    {
        playerBuffs.Add(new BuffInfo { stat = stat, type = type, value = value, turnsLeft = turns });
        DevLog.Log($"[버프 부여] {stat} {(type == BuffType.Percentage ? value * 100 + "%" : value + "포인트")} 증가 ({turns}턴)");
    }

    // 현재 스탯에 버프 수치를 모두 합산해서 반환하는 함수
    private int GetPlayerTotalStat(BuffStat statType, int baseStat)
    {
        float percentageBonusSum = 0; // 20% + 50% = 70% 처럼 합산할 변수
        float flatBonusSum = 0;       // 고정치 합산할 변수

        foreach (var buff in playerBuffs)
        {
            if (buff.stat == statType)
            {
                if (buff.type == BuffType.Percentage) percentageBonusSum += buff.value;
                else flatBonusSum += buff.value;
            }
        }

        // 최종 공식: (기본값 * (1 + 퍼센트합)) + 고정값합
        float finalValue = (baseStat * (1f + percentageBonusSum)) + flatBonusSum;
        return Mathf.RoundToInt(finalValue);
    }

    private int ApplySpecialEffectsToDamage(int incomingDamage)
    {
        foreach (var effectInfo in playerSpecialEffects)
        {
            if (effectInfo.effect == SpecialEffect.Guard)
            {
                incomingDamage /= 2; // 가드 중이면 데미지 절반
                DevLog.Log("가드 발동! 데미지가 절반으로 감소합니다.");
            }
        }
        return incomingDamage;
    }

    // 아군 턴이 시작될 때마다 버프 지속 턴수를 1씩 깎는 함수
    private void UpdatePlayerBuffsOnTurnStart()
    {
        for (int i = playerBuffs.Count - 1; i >= 0; i--)
        {
            playerBuffs[i].turnsLeft--;
            if (playerBuffs[i].turnsLeft <= 0)
            {
                playerBuffs.RemoveAt(i); // 지속 시간이 끝난 버프 삭제
            }
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
        playerBuffs.Clear(); // 전투 시작 시 버프 목록 초기화

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
                CalculateNextTurn();
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
                CalculateNextTurn();
                yield break;
            }

            // 플레이어 턴이 시작될 때 버프 지속 시간을 깎습니다!
            UpdatePlayerBuffsOnTurnStart();

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
                CalculateNextTurn();
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
        else { DevLog.LogError("적 스킬 패턴빔!"); CalculateNextTurn(); }
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
        int attackerSpeed = isPlayerAttacking ? GetPlayerTotalStat(BuffStat.Speed, currentPlayerStats.speed) : currentEnemyData.speed;
        int defenderSpeed = isPlayerAttacking ? currentEnemyData.speed : GetPlayerTotalStat(BuffStat.Speed, currentPlayerStats.speed);
        int attackerStrength = isPlayerAttacking ? GetPlayerTotalStat(BuffStat.Strength, currentPlayerStats.strength) : currentEnemyData.strength;
        int attackerLuck = isPlayerAttacking ? GetPlayerTotalStat(BuffStat.Luck, currentPlayerStats.luck) : currentEnemyData.luck;
        int defenderDefense = isPlayerAttacking ? currentEnemyData.defense : GetPlayerTotalStat(BuffStat.Defense, currentPlayerStats.defense);
        int defenderBR = isPlayerAttacking ? currentEnemyData.breakResistance : currentPlayerStats.breakResistance; // BR은 보통 고정이므로 둠

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

        for (int i = 0; i < totalHits; i++)
        {
            HitResult result = new HitResult();
            result.isHit = CombatMath.CheckHitSuccess(skill.baseAccuracy, attackerSpeed, defenderSpeed);

            if (result.isHit)
            {
                anyHit = true;
                float calculatedDamage = attackerStrength * skill.damageMultiplier;
                if (skill.skillLogic != null) calculatedDamage *= skill.skillLogic.GetDamageMultiplier(currentPlayerStats, currentEnemyData, isPlayerAttacking);

                if (isPlayerAttacking) calculatedDamage *= StyleRankManager.Instance.GetRankDamageMultiplier();

                if (!isPlayerAttacking && isPlayerBroken) calculatedDamage *= 2.0f;
                else if (isPlayerAttacking && isEnemyBroken) calculatedDamage *= 2.0f;

                result.isCrit = CombatMath.CheckCriticalSuccess(skill.bonusCritRate, attackerLuck);
                if (result.isCrit) { calculatedDamage *= 1.5f; anyCrit = true; }

                calculatedDamage *= (1f - CombatMath.GetDamageReduction(defenderDefense));
                result.damage = Mathf.RoundToInt(calculatedDamage);
                if (result.damage <= 0) result.damage = 1;

                if (isPlayerAttacking && !isEnemyBroken)
                {
                    result.breakDamage = skill.breakPower * (skill.skillLogic != null ? skill.skillLogic.GetBreakMultiplier(currentPlayerStats, currentEnemyData, isPlayerAttacking) : 1f);
                    result.breakDamage *= (1f - CombatMath.GetBreakDamageReduction(defenderBR));
                }
                else if (!isPlayerAttacking && !isPlayerBroken)
                {
                    result.breakDamage = skill.breakPower * (skill.skillLogic != null ? skill.skillLogic.GetBreakMultiplier(currentPlayerStats, currentEnemyData, isPlayerAttacking) : 1f);
                    result.breakDamage *= (1f - CombatMath.GetBreakDamageReduction(defenderBR));
                }
                else result.breakDamage = 0f;
            }
            hitResults.Add(result);
        }

        CombatUIManager.Instance.SetCasterImage(isPlayerAttacking, skill.skillActionImage);

        string commentaryText = "";
        if (!anyHit) commentaryText = $"{attackerName}의 {skillName}이(가) 빗나갔습니다!";
        else if (anyCrit) commentaryText = $"{attackerName}의 {skillName} 치명적으로 적중!";
        else commentaryText = $"{attackerName}의 {skillName} 적중!";

        Coroutine textCoroutine = StartCoroutine(CombatUIManager.Instance.TypeCommentary(commentaryText));

        if (anyHit) CombatUIManager.Instance.SetDefenderImage(isPlayerDefending, defenderHitSprite);
        else { CombatUIManager.Instance.SetDefenderImage(isPlayerDefending, defenderEvadeSprite); if (!isPlayerAttacking) StyleRankManager.Instance.OnEvade(); }

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
                    StyleRankManager.Instance.OnPlayerHit();
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
        if (isPlayerAttacking && currentEnemyHp == 0) EndCombat(true);
        else if (!isPlayerAttacking && currentPlayerStats.currentHp == 0) EndCombat(false);
        else CalculateNextTurn();
    }

    private IEnumerator KarinTurnRoutine()
    {
        DevLog.Log("카린의 턴입니다!");
        yield return new WaitForSeconds(1.0f);

        KarinItemData equippedItem = PlayerManager.Instance.equippedKarinItem;
        if (equippedItem == null)
        {
            yield return StartCoroutine(CombatUIManager.Instance.TypeCommentary("카린: \"어라? 쓸 수 있는 물건이 없네!\""));
            CalculateNextTurn();
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
        else CalculateNextTurn();
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
            else CalculateNextTurn();
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
}