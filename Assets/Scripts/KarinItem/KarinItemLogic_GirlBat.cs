using UnityEngine;

[CreateAssetMenu(fileName = "KarinItem_GirlBat", menuName = "KarinItems/Girl Bat")]
public class KarinItemLogic_GirlBat : KarinItemLogicBase
{
    [Header("디버프 설정 (2종)")]
    public StatusEffectData strDebuffData;  // 위에서 만든 Debuff_STR_Down 연결
    public StatusEffectData luckDebuffData; // 위에서 만든 Debuff_LUK_Down 연결

    [Header("수치 설정")]
    public float debuffValue = -0.15f;      // 15% 감소 (음수값)
    public int duration = 3;                // 3턴 지속

    public override int CalculateDamage(PlayerStats pStats, EnemyData eData)
    {
        // 직접적인 데미지는 주지 않습니다.
        return 0;
    }

    public override void ApplyEffect(PlayerStats pStats, EnemyData eData)
    {
        if (eData == null) return;

        bool isApplied = false;

        // 1. 적의 힘(STR) 감소 디버프 부여
        if (strDebuffData != null)
        {
            // 대상(isPlayer=false)에게 디버프를 겁니다.
            BuffManager.Instance.AddEffect(false, strDebuffData, debuffValue, duration);
            isApplied = true;
        }

        // 2. 적의 운(LUK) 감소 디버프 부여
        if (luckDebuffData != null)
        {
            BuffManager.Instance.AddEffect(false, luckDebuffData, debuffValue, duration);
            isApplied = true;
        }

        if (isApplied)
        {
            DevLog.Log($"[소녀 배트] 적의 힘과 운을 {Mathf.Abs(debuffValue) * 100}% 감소시켰습니다.");

            // UI 갱신 (적의 상태창에 디버프 아이콘들이 표시됩니다)
            if (CombatUIManager.Instance != null)
            {
                CombatUIManager.Instance.RefreshBuffUI();
            }
        }
    }
}