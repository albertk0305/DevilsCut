using UnityEngine;

// [패시브 스킬 공간]
// TODO: 탐색 씬 구현 시, 레비아탄 패시브 '별자리가 될 수 있다면' 로직 추가 예정
// (보상 화면에서 10% / 20% / 35% 확률로 레비아탄의 정성 어린 선물 추가)

[CreateAssetMenu(fileName = "Leviathan_StartSkill", menuName = "SupporterLogic/Leviathan/Start Skill")]
public class SupporterLogic_Leviathan_Start : SupporterLogicBase
{
    [Header("버프/디버프 에셋 설정")]
    public StatusEffectData playerApBuff;   // 셰리 AP 증가 버프 (TargetStat = AP, Category = Buff)
    public StatusEffectData enemyApDebuff;  // 적 AP 감소 디버프 (TargetStat = AP, Category = Debuff)
    public int duration = 3;                // 3턴 지속 고정

    [Header("레벨별 AP(행동 게이지) 증감률 (%)")]
    // [수정] Flat 대신 0.15f = 15% 형태의 비율 데이터로 변경했습니다.
    public float[] playerApPercentages = { 0.15f, 0.25f, 0.40f };  // 셰리 AP 증가율
    public float[] enemyApPercentages = { -0.10f, -0.20f, -0.30f }; // 적 AP 감소율 (음수)

    public override void ApplyEffect(PlayerStats pStats, EnemyData enemy, int skillLevel = 1)
    {
        int index = Mathf.Clamp(skillLevel - 1, 0, playerApPercentages.Length - 1);

        // 1. 주인공(셰리)에게 3턴간 AP 증가 버프 부여 (UI 아이콘 표시됨)
        if (playerApBuff != null)
        {
            BuffManager.Instance.AddEffect(true, playerApBuff, playerApPercentages[index], duration);
        }

        // 2. 적에게 3턴간 AP 감소 디버프 부여 (UI 아이콘 표시됨)
        if (enemyApDebuff != null)
        {
            BuffManager.Instance.AddEffect(false, enemyApDebuff, enemyApPercentages[index], duration);
        }

        DevLog.Log($"[독점 스포트라이트] Lv.{skillLevel} 발동! 셰리 AP 버프 (+{playerApPercentages[index] * 100}%), 적 AP 디버프 ({enemyApPercentages[index] * 100}%) 3턴간 지속 부여 완료.");
    }
}