using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class PlayerStats
{
    public int level = 1;
    public int maxExp = 100;
    public int currentExp = 0;
    public int maxHp = 100;
    public int currentHp = 100;

    public int ActionPoints = 5;

    public int KarinAP => Mathf.Max(1, Mathf.RoundToInt(ActionPoints * 0.20f));
    public int SupporterAP => Mathf.Max(1, Mathf.RoundToInt(ActionPoints * 0.11f));

    public int breakResistance = 50;
    public float maxBreakGauge = 100f;
    public int strength = 10;
    public int defense = 10;
    public int speed = 10;
    public int luck = 5;

    public int currentGold = 0;

    public int rejectedSupporterCount = 0;

    [Header("전투 파생 스탯 (아이템/시너지 전용)")]
    public float finalDamageAmp = 0f;
    public float finalDamageReduction = 0f;
    public float critRate = 0f;
    public float critDamage = 1.5f;
    public float lifeSteal = 0f;
    public float trueDamageConversion = 0f;
    public float bonusAccuracy = 0f;
    public float bonusEvasion = 0f;
    public float healingReceivedAmp = 0f;

    public PlayerStats Clone()
    {
        return (PlayerStats)this.MemberwiseClone();
    }
}

[System.Serializable]
public class OwnedItem
{
    public EquipmentItemData data;
    [Range(1, 3)] public int starLevel = 1;

    public OwnedItem(EquipmentItemData data, int starLevel)
    {
        this.data = data;
        this.starLevel = starLevel;
    }
}

public class ItemMergeResult
{
    public EquipmentItemData itemData;
    public int resultStarLevel;

    public ItemMergeResult(EquipmentItemData itemData, int resultStarLevel)
    {
        this.itemData = itemData;
        this.resultStarLevel = resultStarLevel;
    }
}

[System.Serializable]
public class SupporterChoiceRecord
{
    public string supporterID;
    public SupporterChoiceState state;
}

[System.Serializable]
public class PlayerFacilityRankRecord
{
    public string facilityID;
    public int rank;
}

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance;

    [Header("플레이어 스탯")]
    public PlayerStats stats = new PlayerStats();

    [Header("조력자 파티 관리")]
    public List<SupporterData> unlockedSupporters = new List<SupporterData>();
    public List<SupporterChoiceRecord> supporterChoiceRecords = new List<SupporterChoiceRecord>();

    public SupporterData activeSupporter = null;

    [Header("카린 장비 관리")]
    public List<KarinItemData> ownedKarinItems = new List<KarinItemData>();
    public KarinItemData equippedKarinItem = null;

    [Header("일반 장비 인벤토리")]
    public List<OwnedItem> inventory = new List<OwnedItem>();

    [Header("전투 진입 데이터 (임시 저장소)")]
    public EnemyData currentEnemyToFight;

    [Header("Current Battle Context")]
    public BattleReward currentBattleReward;
    public BattleType currentBattleType;
    public int currentBattlePhase;

    [Header("Pending Exploration Progress")]
    public bool pendingAdvanceBattleTurn;
    public BattleType pendingBattleType;
    public int pendingBattlePhase;

    [Header("Pending Dialogue")]
    public DialogueData pendingDialogueData;
    public SupporterData pendingSupporterChoice;
    public string pendingDialogueReturnSceneName;

    [Header("플레이어 해금 스킬")]
    public List<SkillData> unlockedSkills = new List<SkillData>();

    [Header("새 게임 기본 지급")]
    public List<SkillData> defaultSkills = new List<SkillData>();
    public List<KarinItemData> defaultKarinItems = new List<KarinItemData>();
    public KarinItemData defaultEquippedKarinItem;

    [Header("Saved Exploration State")]
    public bool hasSavedExplorationState;
    public GamePhase savedExplorationPhase;
    public int savedExplorationCycle;
    public int savedExplorationTurnInPhase;
    public int savedExplorationKeys;
    public BossEncounterData savedCurrentTargetBoss;
    public Sprite savedLastVisitedNodeImage;
    public FacilityData savedLastVisitedFacility;
    public List<PlayerFacilityRankRecord> savedFacilityRanks = new List<PlayerFacilityRankRecord>();

    public List<SkillData> GetSkillsByCategory(SkillCategory category)
    {
        return unlockedSkills.FindAll(s => s.category == category);
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ResetForNewGame()
    {
        stats = new PlayerStats
        {
            level = 1,
            maxExp = 100,
            currentExp = 0,
            maxHp = 1000,
            currentHp = 1000,
            ActionPoints = 10,
            breakResistance = 10,
            maxBreakGauge = 50f,
            strength = 10,
            defense = 10,
            speed = 10,
            luck = 10,
            currentGold = 0,
            rejectedSupporterCount = 0,
            finalDamageAmp = 0f,
            finalDamageReduction = 0f,
            critRate = 0f,
            critDamage = 1.5f,
            lifeSteal = 0f,
            trueDamageConversion = 0f,
            bonusAccuracy = 0f,
            bonusEvasion = 0f,
            healingReceivedAmp = 0f
        };

        inventory.Clear();

        unlockedSkills.Clear();
        foreach (SkillData defaultSkill in defaultSkills)
        {
            if (defaultSkill == null)
                continue;

            SkillData runtimeSkill = Instantiate(defaultSkill);
            runtimeSkill.skillLevel = 1;
            runtimeSkill.currentEvolution = SkillEvolution.None;
            unlockedSkills.Add(runtimeSkill);
        }

        unlockedSupporters.Clear();
        supporterChoiceRecords.Clear();
        activeSupporter = null;

        ownedKarinItems.Clear();
        foreach (KarinItemData defaultItem in defaultKarinItems)
        {
            if (defaultItem != null && !ownedKarinItems.Contains(defaultItem))
                ownedKarinItems.Add(defaultItem);
        }

        equippedKarinItem = null;
        if (defaultEquippedKarinItem != null)
        {
            if (!ownedKarinItems.Contains(defaultEquippedKarinItem))
                ownedKarinItems.Add(defaultEquippedKarinItem);

            equippedKarinItem = defaultEquippedKarinItem;
        }
        else if (ownedKarinItems.Count > 0)
        {
            equippedKarinItem = ownedKarinItems[0];
        }

        currentEnemyToFight = null;
        currentBattleReward = new BattleReward();
        currentBattleType = default;
        currentBattlePhase = 0;

        pendingAdvanceBattleTurn = false;
        pendingBattleType = default;
        pendingBattlePhase = 0;
        ClearPendingDialogue();

        hasSavedExplorationState = false;
        savedExplorationPhase = GamePhase.BossSelection;
        savedExplorationCycle = 1;
        savedExplorationTurnInPhase = 0;
        savedExplorationKeys = 0;
        savedCurrentTargetBoss = null;
        savedLastVisitedNodeImage = null;
        savedLastVisitedFacility = null;
        savedFacilityRanks.Clear();

        DevLog.Log("[PlayerManager] 새 게임 상태로 초기화했습니다.");
    }

    public SupporterChoiceState GetSupporterChoiceState(string supporterID)
    {
        if (string.IsNullOrEmpty(supporterID))
            return SupporterChoiceState.Undecided;

        SupporterChoiceRecord record = FindSupporterChoiceRecord(supporterID);
        if (record != null)
            return record.state;

        if (IsSupporterUnlocked(supporterID))
            return SupporterChoiceState.Recruited;

        return SupporterChoiceState.Undecided;
    }

    public SupporterChoiceState GetSupporterChoiceState(SupporterData supporter)
    {
        return GetSupporterChoiceState(supporter != null ? supporter.supporterID : null);
    }

    public bool IsSupporterRecruited(string supporterID)
    {
        return GetSupporterChoiceState(supporterID) == SupporterChoiceState.Recruited;
    }

    public bool IsSupporterRecruited(SupporterData supporter)
    {
        return IsSupporterRecruited(supporter != null ? supporter.supporterID : null);
    }

    public bool IsSupporterRejected(string supporterID)
    {
        return GetSupporterChoiceState(supporterID) == SupporterChoiceState.Rejected;
    }

    public bool IsSupporterRejected(SupporterData supporter)
    {
        return IsSupporterRejected(supporter != null ? supporter.supporterID : null);
    }

    public bool IsSupporterChoiceResolved(string supporterID)
    {
        SupporterChoiceState state = GetSupporterChoiceState(supporterID);
        return state == SupporterChoiceState.Recruited || state == SupporterChoiceState.Rejected;
    }

    public bool IsSupporterChoiceResolved(SupporterData supporter)
    {
        return IsSupporterChoiceResolved(supporter != null ? supporter.supporterID : null);
    }

    public bool RecruitSupporter(SupporterData supporter)
    {
        if (supporter == null || string.IsNullOrEmpty(supporter.supporterID))
            return false;

        SupporterChoiceState currentState = GetSupporterChoiceState(supporter.supporterID);
        if (currentState == SupporterChoiceState.Recruited || currentState == SupporterChoiceState.Rejected)
            return false;

        SetSupporterChoiceState(supporter.supporterID, SupporterChoiceState.Recruited);

        if (!IsSupporterUnlocked(supporter.supporterID))
            unlockedSupporters.Add(supporter);

        if (!string.IsNullOrEmpty(supporter.linkedFacilityID))
            EnsureFacilityRankAtLeast(supporter.linkedFacilityID, 1);

        return true;
    }

    public bool RejectSupporter(SupporterData supporter)
    {
        if (supporter == null || string.IsNullOrEmpty(supporter.supporterID))
            return false;

        SupporterChoiceState currentState = GetSupporterChoiceState(supporter.supporterID);
        if (currentState == SupporterChoiceState.Recruited || currentState == SupporterChoiceState.Rejected)
            return false;

        SetSupporterChoiceState(supporter.supporterID, SupporterChoiceState.Rejected);

        if (activeSupporter != null && activeSupporter.supporterID == supporter.supporterID)
            activeSupporter = null;

        stats.rejectedSupporterCount += 1;
        return true;
    }

    public void SetSupporterChoiceState(string supporterID, SupporterChoiceState state)
    {
        if (string.IsNullOrEmpty(supporterID))
            return;

        SupporterChoiceRecord record = FindSupporterChoiceRecord(supporterID);
        if (record == null)
        {
            supporterChoiceRecords.Add(new SupporterChoiceRecord
            {
                supporterID = supporterID,
                state = state
            });
            return;
        }

        record.state = state;
    }

    public void EnsureFacilityRankAtLeast(string facilityID, int minimumRank)
    {
        if (string.IsNullOrEmpty(facilityID))
            return;

        int targetRank = Mathf.Clamp(minimumRank, 0, 3);
        PlayerFacilityRankRecord record = FindFacilityRankRecord(facilityID);
        if (record == null)
        {
            savedFacilityRanks.Add(new PlayerFacilityRankRecord
            {
                facilityID = facilityID,
                rank = targetRank
            });
        }
        else if (record.rank < targetRank)
        {
            record.rank = targetRank;
        }

        if (ExplorationManager.Instance != null)
            ExplorationManager.Instance.EnsureFacilityRankAtLeast(facilityID, targetRank);
    }

    public void SetSavedFacilityRanks(Dictionary<string, int> facilityRanks)
    {
        savedFacilityRanks.Clear();

        if (facilityRanks == null)
            return;

        foreach (KeyValuePair<string, int> rank in facilityRanks)
        {
            if (string.IsNullOrEmpty(rank.Key))
                continue;

            savedFacilityRanks.Add(new PlayerFacilityRankRecord
            {
                facilityID = rank.Key,
                rank = Mathf.Clamp(rank.Value, 0, 3)
            });
        }
    }

    private PlayerFacilityRankRecord FindFacilityRankRecord(string facilityID)
    {
        if (savedFacilityRanks == null || string.IsNullOrEmpty(facilityID))
            return null;

        foreach (PlayerFacilityRankRecord record in savedFacilityRanks)
        {
            if (record != null && record.facilityID == facilityID)
                return record;
        }

        return null;
    }

    public void SetPendingSupporterDialogue(DialogueData dialogueData, SupporterData supporter, string returnSceneName)
    {
        pendingDialogueData = dialogueData;
        pendingSupporterChoice = supporter;
        pendingDialogueReturnSceneName = returnSceneName;
    }

    public void ClearPendingDialogue()
    {
        pendingDialogueData = null;
        pendingSupporterChoice = null;
        pendingDialogueReturnSceneName = "";
    }

    public bool HasPendingDialogue()
    {
        return pendingDialogueData != null;
    }

    public bool HasPendingSupporterChoice()
    {
        return pendingDialogueData != null && pendingSupporterChoice != null;
    }

    private SupporterChoiceRecord FindSupporterChoiceRecord(string supporterID)
    {
        if (supporterChoiceRecords == null || string.IsNullOrEmpty(supporterID))
            return null;

        foreach (SupporterChoiceRecord record in supporterChoiceRecords)
        {
            if (record != null && record.supporterID == supporterID)
                return record;
        }

        return null;
    }

    private bool IsSupporterUnlocked(string supporterID)
    {
        if (unlockedSupporters == null || string.IsNullOrEmpty(supporterID))
            return false;

        foreach (SupporterData supporter in unlockedSupporters)
        {
            if (supporter != null && supporter.supporterID == supporterID)
                return true;
        }

        return false;
    }
    public void TakeDamage(int damage)
    {
        stats.currentHp -= damage;
        if (stats.currentHp < 0) stats.currentHp = 0;

        DevLog.Log($"플레이어가 {damage}의 피해를 입었습니다. 남은 체력: {stats.currentHp}");
    }

    public float GetReflectRatio()
    {
        var courageSkill = unlockedSkills.Find(s => s.skillNameKey == "skill_name_sword1");
        if (courageSkill != null && courageSkill.currentEvolution == SkillEvolution.PathA)
            return courageSkill.evolutionA_Multipliers[Mathf.Clamp(courageSkill.skillLevel - 1, 0, 2)];
        return 0f;
    }

    public void AcquireItem(EquipmentItemData newItemData)
    {
        AcquireItemAndGetMergeResults(newItemData);
    }

    public List<ItemMergeResult> AcquireItemAndGetMergeResults(EquipmentItemData newItemData)
    {
        List<ItemMergeResult> mergeResults = new List<ItemMergeResult>();

        if (newItemData == null)
            return mergeResults;

        int oldMaxHp = GetItemModifiedStats().maxHp;

        AddItemAndMerge(newItemData, 1, mergeResults);

        int newMaxHp = GetItemModifiedStats().maxHp;
        int hpIncrease = newMaxHp - oldMaxHp;
        if (hpIncrease > 0)
        {
            stats.currentHp += hpIncrease;
            DevLog.Log($"[Item Acquire] Max HP increased by {hpIncrease}; current HP increased as well.");
        }

        return mergeResults;
    }

    private void AddItemAndMerge(EquipmentItemData itemData, int targetStarLevel)
    {
        AddItemAndMerge(itemData, targetStarLevel, null);
    }

    private void AddItemAndMerge(EquipmentItemData itemData, int targetStarLevel, List<ItemMergeResult> mergeResults)
    {
        if (itemData == null)
            return;

        if (itemData.grade == ItemGrade.Legendary || targetStarLevel >= 3)
        {
            inventory.Add(new OwnedItem(itemData, targetStarLevel));
            return;
        }

        inventory.Add(new OwnedItem(itemData, targetStarLevel));

        var identicalItems = inventory.FindAll(x => x.data.itemID == itemData.itemID && x.starLevel == targetStarLevel);

        if (identicalItems.Count >= 3)
        {
            DevLog.Log($"[Item Merge] {itemData.itemID} star {targetStarLevel} x3 merged.");
            mergeResults?.Add(new ItemMergeResult(itemData, targetStarLevel + 1));

            for (int i = 0; i < 3; i++)
            {
                inventory.Remove(identicalItems[i]);
            }

            AddItemAndMerge(itemData, targetStarLevel + 1, mergeResults);
        }
    }
    public Dictionary<ItemClass, int> GetCurrentSynergies()
    {
        Dictionary<string, OwnedItem> bestItemById = new Dictionary<string, OwnedItem>();

        foreach (OwnedItem item in inventory)
        {
            if (item == null || item.data == null)
                continue;

            string itemId = item.data.itemID;

            if (string.IsNullOrEmpty(itemId))
            {
                DevLog.LogWarning($"[시너지 계산] itemID가 비어있는 아이템이 있습니다: {item.data.name}");
                continue;
            }

            if (!bestItemById.TryGetValue(itemId, out OwnedItem currentBest))
            {
                bestItemById[itemId] = item;
                continue;
            }

            int currentBestPoints = currentBest.data.GetSynergyPoints(currentBest.starLevel);
            int newItemPoints = item.data.GetSynergyPoints(item.starLevel);

            if (newItemPoints > currentBestPoints)
            {
                bestItemById[itemId] = item;
            }
        }

        Dictionary<ItemClass, int> synergies = new Dictionary<ItemClass, int>();

        foreach (OwnedItem item in bestItemById.Values)
        {
            ItemClass itemClass = item.data.itemClass;
            int points = item.data.GetSynergyPoints(item.starLevel);

            if (!synergies.ContainsKey(itemClass))
                synergies[itemClass] = 0;

            synergies[itemClass] += points;
        }

        return synergies;
    }

    // Builds the battle stat snapshot from base stats, items, and synergies.
    public PlayerStats GetItemModifiedStats()
    {
        PlayerStats modified = stats.Clone();

        int flatStr = 0, flatDef = 0, flatSpd = 0, flatLuck = 0, flatMaxHp = 0, flatAP = 0, flatBR = 0;
        float pctStr = 0f, pctDef = 0f, pctSpd = 0f, pctLuck = 0f, pctMaxHp = 0f, pctAP = 0f, pctBR = 0f;

        foreach (var item in inventory)
        {
            int sl = item.starLevel;
            flatStr += item.data.GetFlatStr(sl);
            flatDef += item.data.GetFlatDef(sl);
            flatSpd += item.data.GetFlatSpd(sl);
            flatLuck += item.data.GetFlatLuck(sl);
            flatMaxHp += item.data.GetFlatMaxHp(sl);
            flatAP += item.data.GetFlatAP(sl);
            flatBR += item.data.GetFlatBR(sl);

            pctStr += item.data.GetPctStr(sl);
            pctDef += item.data.GetPctDef(sl);
            pctSpd += item.data.GetPctSpd(sl);
            pctLuck += item.data.GetPctLuck(sl);
            pctMaxHp += item.data.GetPctMaxHp(sl);
            pctAP += item.data.GetPctAP(sl);

            modified.finalDamageAmp += item.data.GetFinalDamageAmp(sl);
            modified.finalDamageReduction += item.data.GetFinalDamageReduction(sl);
            modified.critRate += item.data.GetCritRateBonus(sl);
            modified.critDamage += item.data.GetCritDamageBonus(sl);
            modified.lifeSteal += item.data.GetLifeStealRate(sl);
        }

        var syn = GetCurrentSynergies();

        // Tier 2 and 4 synergy bonuses.
        // Saber: 2 points STR +15%, 4 points final damage +30%.
        if (syn.GetValueOrDefault(ItemClass.Saber) >= 2) pctStr += 0.15f;
        if (syn.GetValueOrDefault(ItemClass.Saber) >= 4) modified.finalDamageAmp += 0.30f;

        // Shielder: 2 points DEF +20%, 4 points damage reduction +20%.
        if (syn.GetValueOrDefault(ItemClass.Shielder) >= 2) pctDef += 0.20f;
        if (syn.GetValueOrDefault(ItemClass.Shielder) >= 4) modified.finalDamageReduction += 0.20f;

        // Gunner: 2 points LUK +15%, 4 points crit rate +15%.
        if (syn.GetValueOrDefault(ItemClass.Gunner) >= 2) pctLuck += 0.15f;
        if (syn.GetValueOrDefault(ItemClass.Gunner) >= 4) modified.critRate += 0.15f;

        // Assassin 4-point damage is resolved by CombatManager.
        if (syn.GetValueOrDefault(ItemClass.Assassin) >= 2) pctAP += 0.15f;

        if (syn.GetValueOrDefault(ItemClass.Boxer) >= 2) pctSpd += 0.20f;

        if (syn.GetValueOrDefault(ItemClass.Boxer) >= 4)
        {
            modified.bonusAccuracy += 20f;
            modified.bonusEvasion += 20f;
        }

        if (syn.GetValueOrDefault(ItemClass.Beast) >= 2) pctMaxHp += 0.15f;
        if (syn.GetValueOrDefault(ItemClass.Beast) >= 4) pctBR += 0.20f;

        if (syn.GetValueOrDefault(ItemClass.Caster) >= 2) modified.finalDamageAmp += 0.05f;
        if (syn.GetValueOrDefault(ItemClass.Trickster) >= 2) modified.finalDamageAmp += 0.05f;
        if (syn.GetValueOrDefault(ItemClass.Berserker) >= 2) modified.finalDamageReduction += 0.10f;
        if (syn.GetValueOrDefault(ItemClass.Demon) >= 2) modified.lifeSteal += 0.03f;
        if (syn.GetValueOrDefault(ItemClass.Demon) >= 4) modified.healingReceivedAmp += 0.20f;

        var demonEpics = inventory.FindAll(x => x.data.itemClass == ItemClass.Demon && x.data.grade == ItemGrade.Epic);
        foreach (var dEpic in demonEpics)
        {
            if (dEpic.starLevel == 1) modified.healingReceivedAmp += 0.07f;
            else if (dEpic.starLevel == 2) modified.healingReceivedAmp += 0.27f;
            else if (dEpic.starLevel >= 3) modified.healingReceivedAmp += 1.00f;
        }

        // LoneWolf scales exponentially by rejected supporter count.
        float[] loneWolfAmps = { 0f, 0.05f, 0.10f, 0.20f, 0.40f, 0.75f, 1.30f, 2.00f };
        int rejectCount = Mathf.Clamp(stats.rejectedSupporterCount, 0, 7);
        float loneWolfBuff = loneWolfAmps[rejectCount];

        if (loneWolfBuff > 0f)
        {
            pctStr += loneWolfBuff;
            pctDef += loneWolfBuff;
            pctSpd += loneWolfBuff;
            pctLuck += loneWolfBuff;
            pctMaxHp += loneWolfBuff;
            pctAP += loneWolfBuff;
            pctBR += loneWolfBuff;

            DevLog.Log($"[인간 강도] 영입 거절 {rejectCount}회! 전 스탯이 {loneWolfBuff * 100}% 증폭됩니다.");
        }

        // Main stat formula: (base + flat) * (1 + percent).
        modified.strength = Mathf.Max(1, Mathf.RoundToInt((stats.strength + flatStr) * (1f + pctStr)));
        modified.defense = Mathf.Max(1, Mathf.RoundToInt((stats.defense + flatDef) * (1f + pctDef)));
        modified.speed = Mathf.Max(1, Mathf.RoundToInt((stats.speed + flatSpd) * (1f + pctSpd)));
        modified.luck = Mathf.Max(1, Mathf.RoundToInt((stats.luck + flatLuck) * (1f + pctLuck)));
        modified.ActionPoints = Mathf.Max(1, Mathf.RoundToInt((stats.ActionPoints + flatAP) * (1f + pctAP)));
        modified.maxHp = Mathf.Max(1, Mathf.RoundToInt((stats.maxHp + flatMaxHp) * (1f + pctMaxHp)));
        modified.breakResistance = Mathf.Max(1, Mathf.RoundToInt((stats.breakResistance + flatBR) * (1f + pctBR)));


        // Tier 6 and legendary conversions use finalized stats.

        // Saber: true damage conversion.
        if (syn.GetValueOrDefault(ItemClass.Saber) >= 6) modified.trueDamageConversion += 0.20f;
        if (inventory.Any(x => x.data.itemClass == ItemClass.Saber && x.data.grade == ItemGrade.Legendary))
            modified.trueDamageConversion += 0.10f;

        // Shielder: DEF to STR.
        float defToStrRatio = 0f;
        if (syn.GetValueOrDefault(ItemClass.Shielder) >= 6) defToStrRatio += 1.0f;
        if (inventory.Any(x => x.data.itemClass == ItemClass.Shielder && x.data.grade == ItemGrade.Legendary))
            defToStrRatio += 0.5f;
        modified.strength += Mathf.RoundToInt(modified.defense * defToStrRatio);

        // Gunner: LUK to crit damage.
        float luckToCritDmg = 0f;
        if (syn.GetValueOrDefault(ItemClass.Gunner) >= 6) luckToCritDmg += 1.0f;
        if (inventory.Any(x => x.data.itemClass == ItemClass.Gunner && x.data.grade == ItemGrade.Legendary))
            luckToCritDmg += 0.5f;
        modified.critDamage += modified.luck * luckToCritDmg;

        // Assassin: AP to crit stats.
        if (syn.GetValueOrDefault(ItemClass.Assassin) >= 6) modified.critDamage += modified.ActionPoints * 1.0f;
        if (inventory.Any(x => x.data.itemClass == ItemClass.Assassin && x.data.grade == ItemGrade.Legendary))
            modified.critRate += modified.ActionPoints * 0.25f;

        // Boxer: SPD to STR.
        float spdToStrRatio = 0f;
        if (syn.GetValueOrDefault(ItemClass.Boxer) >= 6) spdToStrRatio += 1.0f;
        if (inventory.Any(x => x.data.itemClass == ItemClass.Boxer && x.data.grade == ItemGrade.Legendary))
            spdToStrRatio += 0.5f;
        modified.strength += Mathf.RoundToInt(modified.speed * spdToStrRatio);

        // Beast: MaxHP to STR.
        float hpToStrRatio = 0f;
        if (syn.GetValueOrDefault(ItemClass.Beast) >= 6) hpToStrRatio += 0.10f;
        if (inventory.Any(x => x.data.itemClass == ItemClass.Beast && x.data.grade == ItemGrade.Legendary))
            hpToStrRatio += 0.05f;
        modified.strength += Mathf.RoundToInt(modified.maxHp * hpToStrRatio);


        modified.currentHp = Mathf.Clamp(stats.currentHp, 0, modified.maxHp);

        return modified;
    }
}
