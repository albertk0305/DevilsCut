using UnityEngine;

// [패시브 스킬 공간]
// TODO: 탐색 씬 구현 시, 아스모데우스 패시브 '풀 착장 버프' 로직 추가 예정
// (아이템 세트 효과 갯수에 비례한 스탯 보너스 제공)

[CreateAssetMenu(fileName = "Asmodeus_StartSkill", menuName = "SupporterLogic/Asmodeus/Start Skill")]
public class SupporterLogic_Asmodeus_Start : SupporterLogicBase
{
    [Header("디버프 설정 (매혹)")]
    public StatusEffectData charmDebuff; // '공격력(STR) 감소' 디버프 에셋 연결
    public float debuffValue = -0.15f;   // 15% 감소
    public int duration = 3;             // 3턴 지속

    public override void ApplyEffect(PlayerStats pStats, EnemyData enemy)
    {
        // 1. 적에게 매혹 디버프 부여 (모든 적 적용을 대비해 현재 타겟에 우선 적용)
        if (charmDebuff != null)
        {
            BuffManager.Instance.AddEffect(false, charmDebuff, debuffValue, duration);
            DevLog.Log($"[아스모데우스 개전] 적에게 3턴간 공격력 {Mathf.Abs(debuffValue) * 100}% 감소 디버프를 부여했습니다.");

            if (CombatUIManager.Instance != null)
                CombatUIManager.Instance.RefreshBuffUI();
        }

        // 2. 주인공의 스타일 랭크 1단계 상승
        if (StyleRankManager.Instance != null)
        {
            StyleRankManager.Instance.IncreaseRank(1); 
            DevLog.Log("[아스모데우스 개전] 셰리의 스타일 랭크가 1단계 상승했습니다!");
        }
    }
}