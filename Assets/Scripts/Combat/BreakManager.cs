using UnityEngine;

// [분리] 그로기(Break) 게이지의 누적, 스노우볼 보정, 발동 판정 및 자연 회복을 전담합니다.
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

    // 전투 시작 시 초기화
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

    // 브레이크 데미지 누적 및 발동 확인 (방금 브레이크가 터졌다면 true 반환)
    public bool AddBreakDamage(bool isPlayerTarget, float damage)
    {
        if (IsBroken(isPlayerTarget)) return false; // 이미 브레이크면 무시

        float currentGauge = isPlayerTarget ? playerBreak : enemyBreak;
        float snowballMult = CombatMath.GetBreakSnowballMultiplier(currentGauge);
        float finalDamage = damage * snowballMult;

        if (isPlayerTarget)
        {
            playerBreak += finalDamage;
            if (playerBreak >= 100f) { TriggerBreak(true); return true; }
            else CombatUIManager.Instance.UpdatePlayerBreak(playerBreak);
        }
        else
        {
            enemyBreak += finalDamage;
            if (enemyBreak >= 100f) { TriggerBreak(false); return true; }
            else CombatUIManager.Instance.UpdateEnemyBreak(enemyBreak);
        }
        return false;
    }

    // 브레이크(그로기) 터졌을 때의 내부 처리
    private void TriggerBreak(bool isPlayerTarget)
    {
        if (isPlayerTarget)
        {
            playerBreak = 100f;
            isPlayerBroken = true;
            CombatUIManager.Instance.UpdatePlayerBreak(playerBreak);
            TurnManager.Instance.ResetGauge(EntityType.Player);

            // 그로기 전용 게이지 이미지로 교체!
            CombatUIManager.Instance.playerStatusUI.SetBreakGaugeState(true);

            if (CombatManager.Instance.playerData != null && CombatManager.Instance.playerData.breakImage != null)
                CombatUIManager.Instance.SetDefenderImage(true, CombatManager.Instance.playerData.breakImage);
        }
        else
        {
            enemyBreak = 100f;
            isEnemyBroken = true;
            CombatUIManager.Instance.UpdateEnemyBreak(enemyBreak);
            TurnManager.Instance.ResetGauge(EntityType.Enemy);

            // 그로기 전용 게이지 이미지로 교체!
            CombatUIManager.Instance.enemyStatusUI.SetBreakGaugeState(true);

            if (StyleRankManager.Instance != null) StyleRankManager.Instance.OnEnemyBreak();

            var enemyData = CombatManager.Instance.GetCurrentEnemyData();
            if (enemyData != null && enemyData.breakImage != null)
                CombatUIManager.Instance.SetDefenderImage(false, enemyData.breakImage);
        }
        DevLog.Log($"[브레이크 발동!] {(isPlayerTarget ? "아군" : "적")}이 그로기 상태에 빠졌습니다!");
    }

    // 턴 종료 시 자연 회복 로직
    public void RecoverBreakOnTurnEnd(bool isPlayerTarget, bool tookDamage)
    {
        if (IsBroken(isPlayerTarget)) return; // 그로기 중엔 회복 안 함
        if (tookDamage) return; // 이번 턴에 맞았으면 회복 안 함

        if (isPlayerTarget && playerBreak > 0f)
        {
            float recovery = CombatMath.GetBreakRecoveryAmount(playerBreak);
            playerBreak = Mathf.Max(0f, playerBreak - recovery);
            CombatUIManager.Instance.UpdatePlayerBreak(playerBreak);
            DevLog.Log($"[그로기 회복] 셰리: -{recovery:F1} (현재: {playerBreak:F1})");
        }
        else if (!isPlayerTarget && enemyBreak > 0f)
        {
            float recovery = CombatMath.GetBreakRecoveryAmount(enemyBreak);
            enemyBreak = Mathf.Max(0f, enemyBreak - recovery);
            CombatUIManager.Instance.UpdateEnemyBreak(enemyBreak);
            DevLog.Log($"[그로기 회복] 적: -{recovery:F1} (현재: {enemyBreak:F1})");
        }
    }

    // 그로기 기상 처리
    public void WakeUpFromBreak(bool isPlayer)
    {
        if (isPlayer)
        {
            isPlayerBroken = false;
            playerBreak = 0f;
            CombatUIManager.Instance.UpdatePlayerBreak(0f);

            // [추가] 기상 시 평소 게이지 이미지로 원상 복구!
            CombatUIManager.Instance.playerStatusUI.SetBreakGaugeState(false);
        }
        else
        {
            isEnemyBroken = false;
            enemyBreak = 0f;
            CombatUIManager.Instance.UpdateEnemyBreak(0f);

            // [추가] 기상 시 평소 게이지 이미지로 원상 복구!
            CombatUIManager.Instance.enemyStatusUI.SetBreakGaugeState(false);

            if (CombatManager.Instance != null)
            {
                CombatManager.Instance.hasUsedKiExtraTurn = false;
            }
        }
    }

    public void RecoverBreakInstantly(bool isPlayerTarget, float amount)
    {
        if (IsBroken(isPlayerTarget)) return; // 이미 그로기가 터진 상태면 게이지를 깎을 수 없습니다.

        if (isPlayerTarget && playerBreak > 0f)
        {
            playerBreak = Mathf.Max(0f, playerBreak - amount);
            CombatUIManager.Instance.UpdatePlayerBreak(playerBreak);
            DevLog.Log($"[그로기 즉시 회복] 셰리의 버스트 게이지가 {amount} 감소했습니다. (현재: {playerBreak:F1})");
        }
        else if (!isPlayerTarget && enemyBreak > 0f)
        {
            enemyBreak = Mathf.Max(0f, enemyBreak - amount);
            CombatUIManager.Instance.UpdateEnemyBreak(enemyBreak);
        }
    }
}