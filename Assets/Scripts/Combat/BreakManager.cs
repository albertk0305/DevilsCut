using UnityEngine;

// Owns break gauge accumulation, recovery, and break-state transitions.
public class BreakManager : MonoBehaviour
{
    public static BreakManager Instance;

    private float playerBreak = 0f;
    private float enemyBreak = 0f;

    private bool isPlayerBroken = false;
    private bool isEnemyBroken = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public void InitBreakState()
    {
        playerBreak = 0f;
        enemyBreak = 0f;
        isPlayerBroken = false;
        isEnemyBroken = false;

        if (CombatUIManager.Instance != null)
        {
            CombatUIManager.Instance.UpdatePlayerBreak(playerBreak);
            CombatUIManager.Instance.UpdateEnemyBreak(enemyBreak);
        }
        DevLog.Log("[BreakManager] 그로기(Break) 상태 초기화 완료");
    }

    public bool IsBroken(bool isPlayer) => isPlayer ? isPlayerBroken : isEnemyBroken;
    public float GetBreakGauge(bool isPlayer) => isPlayer ? playerBreak : enemyBreak;

    private float GetMaxGauge(bool isPlayer)
    {
        if (CombatManager.Instance == null) return 100f;
        return isPlayer
            ? CombatManager.Instance.GetCurrentPlayerStats().maxBreakGauge
            : CombatManager.Instance.GetCurrentEnemyData().maxBreakGauge;
    }

    // Returns true only when this damage triggers a new break.
    public bool AddBreakDamage(bool isPlayerTarget, float damage)
    {
        if (IsBroken(isPlayerTarget)) return false;

        float currentGauge = isPlayerTarget ? playerBreak : enemyBreak;
        float maxGauge = GetMaxGauge(isPlayerTarget);

        float snowballMult = CombatMath.GetBreakSnowballMultiplier(currentGauge, maxGauge);
        float finalDamage = damage * snowballMult;

        if (isPlayerTarget)
        {
            playerBreak += finalDamage;
            if (playerBreak >= maxGauge) { TriggerBreak(true); return true; }
            else CombatUIManager.Instance.UpdatePlayerBreak((playerBreak / maxGauge) * 100f);
        }
        else
        {
            enemyBreak += finalDamage;
            if (enemyBreak >= maxGauge) { TriggerBreak(false); return true; }
            else CombatUIManager.Instance.UpdateEnemyBreak((enemyBreak / maxGauge) * 100f);
        }
        return false;
    }

    private void TriggerBreak(bool isPlayerTarget)
    {
        bool wasBroken = IsBroken(isPlayerTarget);
        float maxGauge = GetMaxGauge(isPlayerTarget);

        if (isPlayerTarget)
        {
            playerBreak = maxGauge;
            isPlayerBroken = true;
            CombatUIManager.Instance.UpdatePlayerBreak(100f);
            TurnManager.Instance.ResetGauge(EntityType.Player);
            CombatUIManager.Instance.playerStatusUI.SetBreakGaugeState(true);

            if (CombatManager.Instance.playerData != null && CombatManager.Instance.playerData.breakImage != null)
                CombatUIManager.Instance.SetDefenderImage(true, CombatManager.Instance.playerData.breakImage);
        }
        else
        {
            enemyBreak = maxGauge;
            isEnemyBroken = true;
            CombatUIManager.Instance.UpdateEnemyBreak(100f);
            TurnManager.Instance.ResetGauge(EntityType.Enemy);
            CombatUIManager.Instance.enemyStatusUI.SetBreakGaugeState(true);

            if (StyleRankManager.Instance != null) StyleRankManager.Instance.OnEnemyBreak();

            var enemyData = CombatManager.Instance.GetCurrentEnemyData();
            if (enemyData != null && enemyData.breakImage != null)
                CombatUIManager.Instance.SetDefenderImage(false, enemyData.breakImage);
        }
        if (!wasBroken && IsBroken(isPlayerTarget))
            CombatSfxController.Instance?.PlayGroggy();

        DevLog.Log($"[브레이크 발동!] {(isPlayerTarget ? "아군" : "적")}이 그로기 상태에 빠졌습니다!");
    }

    public void RecoverBreakOnTurnEnd(bool isPlayerTarget, bool tookDamage)
    {
        if (IsBroken(isPlayerTarget)) return;
        if (tookDamage) return;

        float maxGauge = GetMaxGauge(isPlayerTarget);

        if (isPlayerTarget && playerBreak > 0f)
        {
            float recovery = CombatMath.GetBreakRecoveryAmount(playerBreak, maxGauge);
            playerBreak = Mathf.Max(0f, playerBreak - recovery);
            CombatUIManager.Instance.UpdatePlayerBreak((playerBreak / maxGauge) * 100f);
            DevLog.Log($"[그로기 회복] 셰리: -{recovery:F1} (현재: {playerBreak:F1})");
        }
        else if (!isPlayerTarget && enemyBreak > 0f)
        {
            float recovery = CombatMath.GetBreakRecoveryAmount(enemyBreak, maxGauge);
            enemyBreak = Mathf.Max(0f, enemyBreak - recovery);
            CombatUIManager.Instance.UpdateEnemyBreak((enemyBreak / maxGauge) * 100f);
            DevLog.Log($"[그로기 회복] 적: -{recovery:F1} (현재: {enemyBreak:F1})");
        }
    }

    public void WakeUpFromBreak(bool isPlayer)
    {
        if (isPlayer)
        {
            isPlayerBroken = false;
            playerBreak = 0f;
            CombatUIManager.Instance.UpdatePlayerBreak(0f);

            CombatUIManager.Instance.playerStatusUI.SetBreakGaugeState(false);
        }
        else
        {
            isEnemyBroken = false;
            enemyBreak = 0f;
            CombatUIManager.Instance.UpdateEnemyBreak(0f);

            CombatUIManager.Instance.enemyStatusUI.SetBreakGaugeState(false);

            if (CombatManager.Instance != null)
            {
                CombatManager.Instance.currentState.hasUsedKiExtraTurn = false;
            }
        }
    }

    public void RecoverBreakInstantly(bool isPlayerTarget, float amount)
    {
        if (IsBroken(isPlayerTarget)) return;

        float maxGauge = GetMaxGauge(isPlayerTarget);

        if (isPlayerTarget && playerBreak > 0f)
        {
            playerBreak = Mathf.Max(0f, playerBreak - amount);
            CombatUIManager.Instance.UpdatePlayerBreak((playerBreak / maxGauge) * 100f);
            DevLog.Log($"[그로기 즉시 회복] 셰리의 버스트 게이지가 {amount} 감소했습니다. (현재: {playerBreak:F1})");
        }
        else if (!isPlayerTarget && enemyBreak > 0f)
        {
            enemyBreak = Mathf.Max(0f, enemyBreak - amount);
            CombatUIManager.Instance.UpdateEnemyBreak((enemyBreak / maxGauge) * 100f);
        }
    }
}
