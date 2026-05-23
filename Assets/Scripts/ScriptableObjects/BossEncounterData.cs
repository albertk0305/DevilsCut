using UnityEngine;

[CreateAssetMenu(fileName = "NewBossEncounter", menuName = "GameData/BossEncounter")]
public class BossEncounterData : ScriptableObject
{
    public string bossID;
    public string bossName;
    public EnemyData minionEnemy;
    public EnemyData bossEnemy;
    public Sprite nodeIcon;
    public Sprite defaultSD;
    public Sprite readySD;
}
