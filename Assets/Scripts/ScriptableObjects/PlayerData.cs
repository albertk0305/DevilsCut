using UnityEngine;

[CreateAssetMenu(fileName = "NewPlayerData", menuName = "GameData/Player")]
public class PlayerData : ScriptableObject
{
    [Header("플레이어 기본 정보")]
    public string playerNamekey; 

    [Header("플레이어 스프라이트 설정")]
    public Sprite normal; // 기본 UI 및 스탠딩 얼굴
    public Sprite cutIn;  // 턴 대기열용 작은 얼굴
    public Sprite hit;    // 피격 시 얼굴
    public Sprite evade;  // 회피 시 얼굴
}