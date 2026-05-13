using UnityEngine;

[CreateAssetMenu(fileName = "KarinItem_OnTheWayDebuff", menuName = "KarinItems/On The Way (Debuff)")]
public class KarinItemLogic_OnTheWayDebuff : KarinItemLogicBase
{
    [Header("디버프 설정 (2종)")]
    public StatusEffectData defDebuffData;   // 위에서 만든 Debuff_DEF_Down 연결
    public StatusEffectData speedDebuffData; // 위에서 만든 Debuff_SPD_Down 연결

    [Header("수치 설정")]
    public float debuffValue = -0.15f;      // 15% 감소 (음수값 사용)
    public int duration = 3;                // 3턴 지속

    public override int CalculateDamage(PlayerStats pStats, EnemyData eData)
    {
        // 약화 무기이므로 직접적인 데미지는 0입니다.
        return 0;
    }

    public override void ApplyEffect(PlayerStats pStats, EnemyData eData)
    {
        if (eData == null) return;

        bool isApplied = false;

        // 1. 적의 방어력(DEF) 감소 디버프 부여
        if (defDebuffData != null)
        {
            // 대상(isPlayer=false)에게 디버프를 겁니다.
            BuffManager.Instance.AddEffect(false, defDebuffData, debuffValue, duration);
            isApplied = true;
        }

        // 2. 적의 속도(S) 감소 디버프 부여
        if (speedDebuffData != null)
        {
            BuffManager.Instance.AddEffect(false, speedDebuffData, debuffValue, duration);
            isApplied = true;
        }

        if (isApplied)
        {
            DevLog.Log($"[On the way] 적의 방어력과 속도를 {Mathf.Abs(debuffValue) * 100}% 감소시켰습니다.");

            // UI 갱신 (적 상태창에 디버프 아이콘 표시)
            if (CombatUIManager.Instance != null)
            {
                CombatUIManager.Instance.RefreshBuffUI();
            }
        }
    }
}