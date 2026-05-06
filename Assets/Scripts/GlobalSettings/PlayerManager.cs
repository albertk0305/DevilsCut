using UnityEngine;
using System.Collections.Generic;

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
    public int strength = 10;        // 힘
    public int defense = 10;         // 방어
    public int speed = 10;           // 속도
    public int luck = 5;             // 운

    // 전투용 임시 복사본을 만들어주는 함수!
    public PlayerStats Clone()
    {
        return (PlayerStats)this.MemberwiseClone();
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
    public List<EquipmentItemData> ownedEquipments = new List<EquipmentItemData>();

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
}