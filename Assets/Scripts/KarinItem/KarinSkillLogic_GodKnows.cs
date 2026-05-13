using UnityEngine;

[CreateAssetMenu(fileName = "KarinItem_GodKnows", menuName = "KarinItems/God Knows")]
public class KarinItemLogic_GodKnows : KarinItemLogicBase
{
    [Header("버프 설정 (2종)")]
    public StatusEffectData strBuffData;  // 위에서 만든 Buff_STR_Up 연결
    public StatusEffectData luckBuffData; // 위에서 만든 Buff_LUK_Up 연결

    [Header("수치 설정")]
    public float buffValue = 0.15f;       // 15% 상승
    public int duration = 3;              // 3턴 지속

    public override int CalculateDamage(PlayerStats pStats, EnemyData eData)
    {
        // 유틸리티 무기이므로 직접적인 데미지는 0입니다.
        return 0;
    }

    public override void ApplyEffect(PlayerStats pStats, EnemyData eData)
    {
        bool isBuffApplied = false;

        // 1. 힘(STR) 버프 부여
        if (strBuffData != null)
        {
            BuffManager.Instance.AddEffect(true, strBuffData, buffValue, duration);
            isBuffApplied = true;
        }

        // 2. 운(LUK) 버프 부여
        if (luckBuffData != null)
        {
            BuffManager.Instance.AddEffect(true, luckBuffData, buffValue, duration);
            isBuffApplied = true;
        }

        if (isBuffApplied)
        {
            DevLog.Log($"[God Knows] 셰리에게 3턴간 힘(STR)과 운(LUK) {buffValue * 100}% 상승 버프를 부여했습니다.");

            // UI 갱신 (버프 아이콘 2개가 동시에 상태창에 뜹니다)
            if (CombatUIManager.Instance != null)
            {
                CombatUIManager.Instance.RefreshBuffUI();
            }
        }
    }
}