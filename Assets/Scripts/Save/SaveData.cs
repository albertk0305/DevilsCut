using System;
using System.Collections.Generic;

[Serializable]
public class ContinueSaveData
{
    public int version;
    public string savedAt;
    public PlayerGrowthSaveData player;
    public ExplorationContinueSaveData exploration;
}

[Serializable]
public class ClearRecordSaveData
{
    public int version;
    public string recordID;
    public string savedAt;
    public string playerName;
    public string resultType;
    public int finalCycle;
    public int finalLevel;
    public int finalGold;
    public int rejectedSupporterCount;
    public PlayerGrowthSaveData player;
    public int reachedCycle;
    public int defeatedBossCount;
    public int clearTurnOrScore;
}

[Serializable]
public class ClearRecordCollectionSaveData
{
    public int version;
    public List<ClearRecordSaveData> records = new List<ClearRecordSaveData>();
}

[Serializable]
public class PlayerGrowthSaveData
{
    public string playerName;

    public int level;
    public int maxExp;
    public int currentExp;
    public int maxHp;
    public int currentHp;
    public int actionPoints;
    public int breakResistance;
    public float maxBreakGauge;
    public int strength;
    public int defense;
    public int speed;
    public int luck;
    public int currentGold;
    public int rejectedSupporterCount;

    public List<SavedOwnedItem> inventory = new List<SavedOwnedItem>();
    public List<SavedSkillState> skills = new List<SavedSkillState>();
    public List<SavedSupporterState> supporters = new List<SavedSupporterState>();
    public List<string> ownedKarinItemIDs = new List<string>();
    public string equippedKarinItemID;
}

[Serializable]
public class ExplorationContinueSaveData
{
    public GamePhase currentPhase;
    public int currentCycle;
    public int currentTurnInPhase;
    public int currentKeys;
    public string currentTargetBossID;
    public List<string> remainingMidBossIDs = new List<string>();
    public List<SavedFacilityRank> facilityRanks = new List<SavedFacilityRank>();
    public string lastVisitedFacilityID;
    public string lastVisitedNodeID;
    public List<SavedExplorationOption> currentOptions = new List<SavedExplorationOption>();
}

[Serializable]
public class SavedOwnedItem
{
    public string itemID;
    public int starLevel;
}

[Serializable]
public class SavedSkillState
{
    public string skillID;
    public int skillLevel;
    public SkillEvolution currentEvolution;
    public bool unlocked;
}

[Serializable]
public class SavedSupporterState
{
    public string supporterID;
    public bool unlocked;
    public bool active;
    public SupporterChoiceState choiceState;
    public int passiveLevel;
    public int startSkillLevel;
    public int battleSkillLevel;
}

[Serializable]
public class SavedFacilityRank
{
    public string facilityID;
    public int rank;
}

[Serializable]
public class SavedExplorationOption
{
    public int slotIndex;
    public string optionType;
    public string nodeID;
    public string bossID;
    public BattleType battleType;
    public bool isBossBattle;
}
