using UnityEngine;

[CreateAssetMenu(fileName = "KarinItem_Rickenbacker4001", menuName = "KarinItems/Rickenbacker 4001")]
public class KarinItemLogic_Rickenbacker4001 : KarinItemLogicBase
{
    [Header("버프 설정 (2종)")]
    public StatusEffectData defBuffData;   // 위에서 만든 Buff_DEF_Up 연결
    public StatusEffectData speedBuffData; // 위에서 만든 Buff_SPD_Up 연결

    [Header("수치 설정")]
    public float buffValue = 0.15f;        // 15% 상승 
    public int duration = 3;               // 3턴 지속 

    public override int CalculateDamage(PlayerStats pStats, EnemyData eData)
    {
        // 유틸리티 무기이므로 직접적인 데미지는 0입니다.
        return 0;
    }

    public override void ApplyEffect(PlayerStats pStats, EnemyData eData)
    {
        bool isBuffApplied = false;

        // 1. 방어력(DEF) 버프 부여
        if (defBuffData != null)
        {
            BuffManager.Instance.AddEffect(true, defBuffData, buffValue, duration);
            isBuffApplied = true;
        }

        // 2. 속도(S) 버프 부여
        if (speedBuffData != null)
        {
            BuffManager.Instance.AddEffect(true, speedBuffData, buffValue, duration);
            isBuffApplied = true;
        }

        if (isBuffApplied)
        {
            DevLog.Log($"[Rickenbacker 4001] 셰리에게 3턴간 방어력(DEF)과 속도(S) {buffValue * 100}% 상승 버프를 부여했습니다.");

            // UI 갱신 (버프 아이콘들이 상태창에 표시됩니다)
            if (CombatUIManager.Instance != null)
            {
                CombatUIManager.Instance.RefreshBuffUI();
            }
        }
    }
}