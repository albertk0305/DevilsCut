using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemy", menuName = "GameData/Enemy")]
public class EnemyData : ScriptableObject
{
    [Header("기본 정보")]
    public string enemyNameKey;
    public Sprite enemyImage;
    public Sprite CutIn;

    [Header("리액션 이미지")]
    public Sprite hit;
    public Sprite evade;
    public Sprite breakImage;
    public Sprite guardImage;

    [Header("적 AI 설정")]
    public EnemyAIBase aiBrain;

    [Header("스탯 보정치")]
    public EnemyStatModifier statModifier;

    [Header("전투 스탯")]
    public int level;
    public int maxHp;

    public int ActionPoints;

    public int currentHp;

    public int breakResistance;
    public float maxBreakGauge = 100f;
    public int strength;
    public int defense;
    public int speed;
    public int luck;

    [Header("특수 전투 스탯 (전투 중 실시간 변동)")]
    public float damageGivenAmp = 0f;
    public float damageReduction = 0f;
    public float critRate = 0f;
    public float critDamage = 1.5f;
    public float lifeSteal = 0f;
    public float trueDamageConversion = 0f;
    public float bonusAccuracy = 0f;
    public float bonusEvasion = 0f;
    public float healingReceivedAmp = 0f;
}