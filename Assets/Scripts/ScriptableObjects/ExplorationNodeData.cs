using UnityEngine;

// 이 클래스는 부모 역할만 하므로 CreateAssetMenu 속성을 없음
public class ExplorationNodeData : ScriptableObject
{
    [Header("기본 노드 정보")]
    public string nodeID;           // 예: "shop", "event_01", "combat_goblin"
    public Sprite nodeImage;        // 탐색 씬에서 보여질 버튼 이미지 (시설, ?표, 칼모양 등)
    //public string nodeNameKey;      // 번역용 이름 키
}