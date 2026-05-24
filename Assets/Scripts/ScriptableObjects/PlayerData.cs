using UnityEngine;

[CreateAssetMenu(fileName = "NewPlayerData", menuName = "GameData/Player")]
public class PlayerData : ScriptableObject
{
    [Header("플레이어 기본 정보")]
    public string playerNamekey; 

    [Header("플레이어 스프라이트 설정")]
    public Sprite normal;
    public Sprite cutIn;
    public Sprite hit;
    public Sprite evade;
    public Sprite breakImage;
    public Sprite guardImage;
    public Sprite reflectImage;
}