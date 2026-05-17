using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// 플레이어의 스탯을 묶어두는 클래스 (Inspector에서 보기 좋게 직렬화)
[System.Serializable]
//플레이어 정보 저장 코드
public class PlayerStats
{
    public int level = 1;
    public int maxExp = 100;
    public int currentExp = 0;
    public int maxHp = 100;
    public int currentHp = 100;

    public int ActionPoints = 5;

    // 주인공 3번 칠 때 1번 침
    public int KarinAP => Mathf.Max(1, Mathf.RoundToInt(ActionPoints * 0.20f));
    // 주인공 5번 칠 때 1번 침
    public int SupporterAP => Mathf.Max(1, Mathf.RoundToInt(ActionPoints * 0.11f));

    public int breakResistance = 50; // 그로기 저항
    public float maxBreakGauge = 100f; // 최대 브레이크 수치
    public int strength = 10;        // 힘
    public int defense = 10;         // 방어
    public int speed = 10;           // 속도
    public int luck = 5;             // 운

    public int currentGold = 0;

    // [추가] 조력자 영입을 거절한 횟수 (최대 7)
    public int rejectedSupporterCount = 0;

    [Header("전투 파생 스탯 (아이템/시너지 전용)")]
    public float finalDamageAmp = 0f;        // 최종 피해 증폭 (%)
    public float finalDamageReduction = 0f;  // 받는 피해 감소 (%)
    public float critRate = 0f;              // 크리티컬 확률 (%)
    public float critDamage = 1.5f;          // 크리티컬 피해량 (기본 150%)
    public float lifeSteal = 0f;             // 흡혈률 (%)
    public float trueDamageConversion = 0f;  // 방어 무시 고정피해 전환율 (%)
    public float bonusAccuracy = 0f;
    public float bonusEvasion = 0f;
    public float healingReceivedAmp = 0f;

    // 전투용 임시 복사본을 만들어주는 함수!
    public PlayerStats Clone()
    {
        return (PlayerStats)this.MemberwiseClone();
    }
}

[System.Serializable]
public class OwnedItem
{
    public EquipmentItemData data; // 아이템의 원본 데이터 (이름, 스탯 배열 등)
    [Range(1, 3)] public int starLevel = 1; // 내 인벤토리에서 이 아이템의 현재 성급

    public OwnedItem(EquipmentItemData data, int starLevel)
    {
        this.data = data;
        this.starLevel = starLevel;
    }
}

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance;

    [Header("플레이어 스탯")]
    public PlayerStats stats = new PlayerStats();

    [Header("조력자 파티 관리")]
    // 게임 진행 중 해금된 모든 조력자를 담는 리스트
    public List<SupporterData> unlockedSupporters = new List<SupporterData>();

    // 현재 파티에 '합류'해 있는 조력자 (null이면 아무도 없는 상태)
    public SupporterData activeSupporter = null;

    [Header("카린 장비 관리")]
    public List<KarinItemData> ownedKarinItems = new List<KarinItemData>(); // 소지한 아이템 목록
    public KarinItemData equippedKarinItem = null; // 현재 착용 중인 아이템

    [Header("일반 장비 인벤토리")]
    public List<OwnedItem> inventory = new List<OwnedItem>();

    [Header("전투 진입 데이터 (임시 저장소)")]
    public EnemyData currentEnemyToFight; // 탐색 씬에서 넘겨준 적 데이터를 전투 씬까지 배달해 줄 변수

    [Header("플레이어 해금 스킬")]
    public List<SkillData> unlockedSkills = new List<SkillData>();

    // 특정 카테고리의 스킬만 쏙쏙 뽑아주는 헬퍼 함수
    public List<SkillData> GetSkillsByCategory(SkillCategory category)
    {
        return unlockedSkills.FindAll(s => s.category == category);
    }

    private void Awake()
    {
        // 싱글톤 & 씬이 넘어가도 파괴되지 않도록 설정 (매우 중요!)
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬이 바뀌어도 살아남아라!
        }
        else
        {
            Destroy(gameObject); // 이미 매니저가 있다면 새로 생긴 짝퉁은 파괴
        }
    }

    // 데미지를 입었을 때 호출할 함수 예시
    public void TakeDamage(int damage)
    {
        stats.currentHp -= damage;
        if (stats.currentHp < 0) stats.currentHp = 0;

        DevLog.Log($"플레이어가 {damage}의 피해를 입었습니다. 남은 체력: {stats.currentHp}");
    }

    public float GetReflectRatio()
    {
        var courageSkill = unlockedSkills.Find(s => s.skillNameKey == "skill_name_sword1"); // 하드코딩은 데이터의 주인인 여기서만 관리!
        if (courageSkill != null && courageSkill.currentEvolution == SkillEvolution.PathA)
            return courageSkill.evolutionA_Multipliers[Mathf.Clamp(courageSkill.skillLevel - 1, 0, 2)];
        return 0f;
    }

    // =========================================================
    // [핵심 1] 아이템 획득 및 체력 증가 보정 로직
    // =========================================================
    public void AcquireItem(EquipmentItemData newItemData)
    {
        // 1. 아이템을 먹기 전의 '아이템이 적용된 최대 체력' 스냅샷을 찍습니다.
        int oldMaxHp = GetItemModifiedStats().maxHp;

        // 2. 인벤토리에 아이템을 넣고 합성(Merge) 판정을 돌립니다.
        AddItemAndMerge(newItemData, 1);

        // 3. 아이템을 먹고(혹은 합성되고) 난 후의 최대 체력 스냅샷을 찍습니다.
        int newMaxHp = GetItemModifiedStats().maxHp;

        // 4. 아이템으로 인해 최대 체력이 늘어났다면, 그만큼 현재 체력도 공짜로 채워줍니다!
        int hpIncrease = newMaxHp - oldMaxHp;
        if (hpIncrease > 0)
        {
            stats.currentHp += hpIncrease;
            DevLog.Log($"[아이템 획득] 최대 체력이 {hpIncrease} 증가하여 현재 체력도 함께 차올랐습니다!");
        }
    }

    // =========================================================
    // [핵심 2] 오토 체스식 3성 연쇄 합성(Merge) 로직
    // =========================================================
    private void AddItemAndMerge(EquipmentItemData itemData, int targetStarLevel)
    {
        // 전설(Legendary) 등급이거나, 이미 3성(Max)이라면 합성하지 않고 바로 인벤토리에 넣습니다.
        if (itemData.grade == ItemGrade.Legendary || targetStarLevel >= 3)
        {
            inventory.Add(new OwnedItem(itemData, targetStarLevel));
            return;
        }

        // 일단 인벤토리에 해당 성급으로 아이템을 추가합니다.
        inventory.Add(new OwnedItem(itemData, targetStarLevel));

        // 내 인벤토리에서 '방금 추가한 아이템과 완전히 똑같은 아이템 & 같은 성급'인 것들을 전부 찾습니다.
        var identicalItems = inventory.FindAll(x => x.data.itemID == itemData.itemID && x.starLevel == targetStarLevel);

        // 똑같은 게 3개가 모였다면? 합성 시작!
        if (identicalItems.Count >= 3)
        {
            DevLog.Log($"[합성 연출] {itemData.itemID} {targetStarLevel}성 3개가 모여 빛을 발합니다!");

            // 1. 재료가 된 3개의 아이템을 인벤토리에서 삭제합니다.
            for (int i = 0; i < 3; i++)
            {
                inventory.Remove(identicalItems[i]);
            }

            // 2. 성급(Star Level)을 1 올려서 다시 AddItemAndMerge를 호출합니다. (재귀 함수)
            // 이렇게 하면 1성 3개 -> 2성 1개가 되는데, 마침 2성이 3개째였다면 알아서 3성으로 연쇄 합성됩니다!
            AddItemAndMerge(itemData, targetStarLevel + 1);
        }
    }

    // =========================================================
    // [핵심 3] 현재 보유 중인 클래스별 시너지 점수 계산기
    // =========================================================
    public Dictionary<ItemClass, int> GetCurrentSynergies()
    {
        var synergies = new Dictionary<ItemClass, int>();

        foreach (var item in inventory)
        {
            if (!synergies.ContainsKey(item.data.itemClass))
                synergies[item.data.itemClass] = 0;

            synergies[item.data.itemClass] += item.data.GetSynergyPoints(item.starLevel);
        }
        return synergies;
    }

    // =========================================================
    // [핵심 4] 아이템 및 시너지가 모두 반영된 전투 스냅샷 생성
    // =========================================================
    public PlayerStats GetItemModifiedStats()
    {
        PlayerStats modified = stats.Clone();

        int flatStr = 0, flatDef = 0, flatSpd = 0, flatLuck = 0, flatMaxHp = 0, flatAP = 0, flatBR = 0;
        float pctStr = 0f, pctDef = 0f, pctSpd = 0f, pctLuck = 0f, pctMaxHp = 0f, pctAP = 0f, pctBR = 0f;

        // 1. 인벤토리 순회: 개별 아이템의 합(Flat), 곱(Pct), 전투 스탯 누적
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

        // 2. 시너지 점수 가져오기
        var syn = GetCurrentSynergies();

        // 3. 기획서 기반 시너지 패시브 적용 (1단계~2단계)
        // [세이버] 2점: 힘 15% / 4점: 최종 피해 30%
        if (syn.GetValueOrDefault(ItemClass.Saber) >= 2) pctStr += 0.15f;
        if (syn.GetValueOrDefault(ItemClass.Saber) >= 4) modified.finalDamageAmp += 0.30f;

        // [실더] 2점: 방어력 20% / 4점: 받는 피해 20% 감소
        if (syn.GetValueOrDefault(ItemClass.Shielder) >= 2) pctDef += 0.20f;
        if (syn.GetValueOrDefault(ItemClass.Shielder) >= 4) modified.finalDamageReduction += 0.20f;

        // [거너] 2점: 운 15% / 4점: 크리티컬 15%
        if (syn.GetValueOrDefault(ItemClass.Gunner) >= 2) pctLuck += 0.15f;
        if (syn.GetValueOrDefault(ItemClass.Gunner) >= 4) modified.critRate += 0.15f;

        // [어새신] 2점: AP 15% (※ 4점 기습의 기회는 CombatManager에서 연산)
        if (syn.GetValueOrDefault(ItemClass.Assassin) >= 2) pctAP += 0.15f;

        // [복서] 2점: 속도 20%
        if (syn.GetValueOrDefault(ItemClass.Boxer) >= 2) pctSpd += 0.20f;

        // [복서] 4점: 명중률 및 회피율 20% 추가 상승
        if (syn.GetValueOrDefault(ItemClass.Boxer) >= 4)
        {
            modified.bonusAccuracy += 20f;
            modified.bonusEvasion += 20f;
        }

        // [비스트] 2점: 체력 15% / 4점: BR 20%
        if (syn.GetValueOrDefault(ItemClass.Beast) >= 2) pctMaxHp += 0.15f;
        if (syn.GetValueOrDefault(ItemClass.Beast) >= 4) pctBR += 0.20f;

        // [캐스터/트릭스터/버서커/데몬] 2점 시너지
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
            else if (dEpic.starLevel >= 3) modified.healingReceivedAmp += 1.00f; // 100% 증가!
        }

        // [추가] 11번째 클래스 '인간 강도(솔로 플레이)' 시너지 적용!
        // 기획된 지수적 보정 수치 배열 (0명 ~ 7명)
        float[] loneWolfAmps = { 0f, 0.05f, 0.10f, 0.20f, 0.40f, 0.75f, 1.30f, 2.00f };
        int rejectCount = Mathf.Clamp(stats.rejectedSupporterCount, 0, 7);
        float loneWolfBuff = loneWolfAmps[rejectCount];

        if (loneWolfBuff > 0f)
        {
            // 전 스탯(7종)에 보정치 100% 동일 적용
            pctStr += loneWolfBuff;
            pctDef += loneWolfBuff;
            pctSpd += loneWolfBuff;
            pctLuck += loneWolfBuff;
            pctMaxHp += loneWolfBuff;
            pctAP += loneWolfBuff;
            pctBR += loneWolfBuff;

            DevLog.Log($"[인간 강도] 영입 거절 {rejectCount}회! 전 스탯이 {loneWolfBuff * 100}% 증폭됩니다.");
        }

        // 4. (기본 + 합산) * (1 + 곱산) 메인 스탯 연산
        modified.strength = Mathf.Max(1, Mathf.RoundToInt((stats.strength + flatStr) * (1f + pctStr)));


        // 4. (기본 + 합산) * (1 + 곱산) 메인 스탯 연산
        modified.strength = Mathf.Max(1, Mathf.RoundToInt((stats.strength + flatStr) * (1f + pctStr)));
        modified.defense = Mathf.Max(1, Mathf.RoundToInt((stats.defense + flatDef) * (1f + pctDef)));
        modified.speed = Mathf.Max(1, Mathf.RoundToInt((stats.speed + flatSpd) * (1f + pctSpd)));
        modified.luck = Mathf.Max(1, Mathf.RoundToInt((stats.luck + flatLuck) * (1f + pctLuck)));
        modified.ActionPoints = Mathf.Max(1, Mathf.RoundToInt((stats.ActionPoints + flatAP) * (1f + pctAP)));
        modified.maxHp = Mathf.Max(1, Mathf.RoundToInt((stats.maxHp + flatMaxHp) * (1f + pctMaxHp)));
        modified.breakResistance = Mathf.Max(1, Mathf.RoundToInt((stats.breakResistance + flatBR) * (1f + pctBR)));


        // 5. 6시너지 및 전설 아이템의 하이엔드 '스탯 전환(Conversion)' 적용
        // 이 전환은 이미 곱연산까지 끝난 완성된 스탯을 끌어와서 다른 스탯에 더해줍니다.

        // [세이버] 6점: 고정피해 20% + 전설 10%
        if (syn.GetValueOrDefault(ItemClass.Saber) >= 6) modified.trueDamageConversion += 0.20f;
        if (inventory.Any(x => x.data.itemClass == ItemClass.Saber && x.data.grade == ItemGrade.Legendary))
            modified.trueDamageConversion += 0.10f;

        // [실더] 6점: DEF 100% -> STR + 전설 50%
        float defToStrRatio = 0f;
        if (syn.GetValueOrDefault(ItemClass.Shielder) >= 6) defToStrRatio += 1.0f;
        if (inventory.Any(x => x.data.itemClass == ItemClass.Shielder && x.data.grade == ItemGrade.Legendary))
            defToStrRatio += 0.5f;
        modified.strength += Mathf.RoundToInt(modified.defense * defToStrRatio);

        // [거너] 6점: LUCK 100% -> CritDMG + 전설 50%
        float luckToCritDmg = 0f;
        if (syn.GetValueOrDefault(ItemClass.Gunner) >= 6) luckToCritDmg += 1.0f;
        if (inventory.Any(x => x.data.itemClass == ItemClass.Gunner && x.data.grade == ItemGrade.Legendary))
            luckToCritDmg += 0.5f;
        modified.critDamage += modified.luck * luckToCritDmg;

        // [어새신] 6점: AP 100% -> CritDMG + 전설(확률전환 25%)
        if (syn.GetValueOrDefault(ItemClass.Assassin) >= 6) modified.critDamage += modified.ActionPoints * 1.0f;
        if (inventory.Any(x => x.data.itemClass == ItemClass.Assassin && x.data.grade == ItemGrade.Legendary))
            modified.critRate += modified.ActionPoints * 0.25f;

        // [복서] 6점: SPD 100% -> STR + 전설 50%
        float spdToStrRatio = 0f;
        if (syn.GetValueOrDefault(ItemClass.Boxer) >= 6) spdToStrRatio += 1.0f;
        if (inventory.Any(x => x.data.itemClass == ItemClass.Boxer && x.data.grade == ItemGrade.Legendary))
            spdToStrRatio += 0.5f;
        modified.strength += Mathf.RoundToInt(modified.speed * spdToStrRatio);

        // [비스트] 6점: MaxHP 10% -> STR + 전설 5%
        float hpToStrRatio = 0f;
        if (syn.GetValueOrDefault(ItemClass.Beast) >= 6) hpToStrRatio += 0.10f;
        if (inventory.Any(x => x.data.itemClass == ItemClass.Beast && x.data.grade == ItemGrade.Legendary))
            hpToStrRatio += 0.05f;
        modified.strength += Mathf.RoundToInt(modified.maxHp * hpToStrRatio);


        // 6. 체력 클램핑 마무리
        modified.currentHp = Mathf.Clamp(stats.currentHp, 0, modified.maxHp);

        return modified;
    }
}