using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemy", menuName = "GameData/Enemy")]
public class EnemyData : ScriptableObject
{
    [Header("기본 정보")]
    public string enemyNameKey; // 다국어 번역을 위한 Key
    public Sprite enemyImage;   // 전투 씬에 띄울 이미지
    public Sprite CutIn;

    [Header("리액션 이미지")]
    public Sprite hit;
    public Sprite evade;
    public Sprite breakImage;
    public Sprite guardImage;

    [Header("적 AI 설정")]
    public EnemyAIBase aiBrain;

    [Header("전투 스탯")]
    public int level;
    public int maxHp;

    public int ActionPoints;

    public int currentHp;

    public int breakResistance; // 그로기 저항
    public float maxBreakGauge = 100f; // 최대 브레이크 수치
    public int strength;        // 힘
    public int defense;         // 방어
    public int speed;           // 속도
    public int luck;             // 운

    [Header("특수 전투 스탯 (전투 중 실시간 변동)")]
    public float damageGivenAmp = 0f;       // 가하는 피해 증폭 (%)
    public float damageReduction = 0f;      // 받는 피해 감소 (%)
    public float critRate = 0f;              // 크리티컬 확률 (%)
    public float critDamage = 1.5f;          // 크리티컬 피해량 (기본 1.5f = 150%)
    public float lifeSteal = 0f;            // 글로벌 흡혈률 (%)
    public float trueDamageConversion = 0f;  // 방어 무시 고정피해 전환율 (%)
    public float bonusAccuracy = 0f;         // 보너스 명중률 (%)
    public float bonusEvasion = 0f;          // 보너스 회피율 (%)
    public float healingReceivedAmp = 0f;   // 받는 회복량 증폭 (%)
}