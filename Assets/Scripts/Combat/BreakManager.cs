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

    // [추가] 실시간으로 플레이어/적의 최대 브레이크 수치를 가져오는 헬퍼 함수
    private float GetMaxGauge(bool isPlayer)
    {
        if (CombatManager.Instance == null) return 100f;
        return isPlayer
            ? CombatManager.Instance.GetCurrentPlayerStats().maxBreakGauge
            : CombatManager.Instance.GetCurrentEnemyData().maxBreakGauge;
    }

    // 브레이크 데미지 누적 및 발동 확인 (방금 브레이크가 터졌다면 true 반환)
    public bool AddBreakDamage(bool isPlayerTarget, float damage)
    {
        if (IsBroken(isPlayerTarget)) return false;

        float currentGauge = isPlayerTarget ? playerBreak : enemyBreak;
        float maxGauge = GetMaxGauge(isPlayerTarget); // 대상의 최대치 호출

        // 수학 연산에 최대치 전달
        float snowballMult = CombatMath.GetBreakSnowballMultiplier(currentGauge, maxGauge);
        float finalDamage = damage * snowballMult;

        if (isPlayerTarget)
        {
            playerBreak += finalDamage;
            if (playerBreak >= maxGauge) { TriggerBreak(true); return true; }
            else CombatUIManager.Instance.UpdatePlayerBreak((playerBreak / maxGauge) * 100f); // UI에는 0~100% 비율로 변환 전달
        }
        else
        {
            enemyBreak += finalDamage;
            if (enemyBreak >= maxGauge) { TriggerBreak(false); return true; }
            else CombatUIManager.Instance.UpdateEnemyBreak((enemyBreak / maxGauge) * 100f); // UI에는 0~100% 비율로 변환 전달
        }
        return false;
    }

    // 브레이크(그로기) 터졌을 때의 내부 처리
    private void TriggerBreak(bool isPlayerTarget)
    {
        float maxGauge = GetMaxGauge(isPlayerTarget);

        if (isPlayerTarget)
        {
            playerBreak = maxGauge; // 100f 대신 maxGauge로 고정
            isPlayerBroken = true;
            CombatUIManager.Instance.UpdatePlayerBreak(100f); // 꽉 찬 UI(100%) 표출
            TurnManager.Instance.ResetGauge(EntityType.Player);
            CombatUIManager.Instance.playerStatusUI.SetBreakGaugeState(true);

            if (CombatManager.Instance.playerData != null && CombatManager.Instance.playerData.breakImage != null)
                CombatUIManager.Instance.SetDefenderImage(true, CombatManager.Instance.playerData.breakImage);
        }
        else
        {
            enemyBreak = maxGauge; // 100f 대신 maxGauge로 고정
            isEnemyBroken = true;
            CombatUIManager.Instance.UpdateEnemyBreak(100f); // 꽉 찬 UI(100%) 표출
            TurnManager.Instance.ResetGauge(EntityType.Enemy);
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
        if (IsBroken(isPlayerTarget)) return;
        if (tookDamage) return;

        float maxGauge = GetMaxGauge(isPlayerTarget);

        if (isPlayerTarget && playerBreak > 0f)
        {
            float recovery = CombatMath.GetBreakRecoveryAmount(playerBreak, maxGauge);
            playerBreak = Mathf.Max(0f, playerBreak - recovery);
            CombatUIManager.Instance.UpdatePlayerBreak((playerBreak / maxGauge) * 100f); // 비율 환산
            DevLog.Log($"[그로기 회복] 셰리: -{recovery:F1} (현재: {playerBreak:F1})");
        }
        else if (!isPlayerTarget && enemyBreak > 0f)
        {
            float recovery = CombatMath.GetBreakRecoveryAmount(enemyBreak, maxGauge);
            enemyBreak = Mathf.Max(0f, enemyBreak - recovery);
            CombatUIManager.Instance.UpdateEnemyBreak((enemyBreak / maxGauge) * 100f); // 비율 환산
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