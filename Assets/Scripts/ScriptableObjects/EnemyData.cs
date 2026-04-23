using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemy", menuName = "GameData/Enemy")]
public class EnemyData : ScriptableObject
{
    [Header("기본 정보")]
    public string enemyNameKey; // 다국어 번역을 위한 Key
    public Sprite enemyImage;   // 전투 씬에 띄울 이미지

    [Header("전투 스탯")]
    public int level;
    public int maxHp;

    public int ActionPoints; 

    public int breakResistance; // 그로기 저항
    public int strength;        // 힘
    public int defense;         // 방어
    public int speed;           // 속도
    public int luck;             // 운
}