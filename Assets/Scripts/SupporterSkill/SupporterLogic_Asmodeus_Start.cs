using UnityEngine;

// [패시브 스킬 공간]
// TODO: 탐색 씬 구현 시, 아스모데우스 패시브 '풀 착장 버프' 로직 추가 예정
// (아이템 세트 효과 갯수에 비례한 스탯 보너스 제공)

[CreateAssetMenu(fileName = "Asmodeus_StartSkill", menuName = "SupporterLogic/Asmodeus/Start Skill")]
public class SupporterLogic_Asmodeus_Start : SupporterLogicBase
{
    [Header("디버프 설정 (매혹)")]
    public StatusEffectData charmDebuff; // '공격력(STR) 감소' 디버프 에셋 연결
    public int duration = 3;             // 3턴 지속

    [Header("레벨별 수치 설정")]
    public float[] debuffValues = { -0.10f, -0.15f, -0.20f }; // 적 공격력 감소율
    public int[] rankUpValues = { 1, 1, 2 };                  // 셰리 랭크업 수치

    public override void ApplyEffect(PlayerStats pStats, EnemyData enemy, int skillLevel = 1)
    {
        // 레벨에 맞는 인덱스(0, 1, 2)를 안전하게 가져옵니다.
        int index = Mathf.Clamp(skillLevel - 1, 0, debuffValues.Length - 1);

        // 1. 적에게 매혹 디버프 부여
        if (charmDebuff != null)
        {
            BuffManager.Instance.AddEffect(false, charmDebuff, debuffValues[index], duration);
            DevLog.Log($"[아스모데우스 개전] Lv.{skillLevel} 발동! 적 공격력 {Mathf.Abs(debuffValues[index]) * 100}% 감소.");

            if (CombatUIManager.Instance != null)
                CombatUIManager.Instance.RefreshBuffUI();
        }

        // 2. 주인공의 스타일 랭크 상승
        if (StyleRankManager.Instance != null)
        {
            StyleRankManager.Instance.IncreaseRank(rankUpValues[index]);
            DevLog.Log($"[아스모데우스 개전] 셰리의 스타일 랭크가 {rankUpValues[index]}단계 상승했습니다!");
        }
    }
}