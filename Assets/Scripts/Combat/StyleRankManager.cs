using UnityEngine;

// 랭크 단계를 정의하는 열거형
public enum StyleRank { None, D, C, B, A, S, SS, SSS }

public class StyleRankManager : MonoBehaviour
{
    public static StyleRankManager Instance;

    public StyleRank currentRank = StyleRank.None;

    // 이전 턴에 사용한 스킬 카테고리를 기억
    private SkillCategory previousCategory;
    private bool isFirstSkill = true; // 게임 시작 후 첫 스킬인지 확인
    private bool hasCritThisTurn = false; // 이번 턴에 크리티컬이 이미 터졌는지 확인

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public void InitCombat()
    {
        currentRank = StyleRank.None;
        previousCategory = SkillCategory.None; // 아직 아무 스킬도 안 쓴 상태로! (에러 방지를 위해 enum에 None이 없다면 적당히 초기화)
        isFirstSkill = true;
        hasCritThisTurn = false;

        UpdateUI(); // UI도 None 상태(투명)로 업데이트합니다.
        DevLog.Log("[스타일 랭크] 전투 시작! 랭크가 초기화되었습니다.");
    }

    // 매 턴이 끝날 때마다 호출하여 턴 단위 변수들을 초기화합니다.
    public void ResetTurnState()
    {
        hasCritThisTurn = false;
    }

    // 1. 스킬 사용 조건 (다른 계열 사용 시 상승)
    public void OnSkillUsed(SkillCategory usedCategory)
    {
        if (isFirstSkill || usedCategory != previousCategory)
        {
            IncreaseRank();
        }

        previousCategory = usedCategory;
        isFirstSkill = false;
    }

    // 2. 크리티컬 조건 (한 턴에 한 번만)
    public void OnCriticalHit()
    {
        if (!hasCritThisTurn)
        {
            IncreaseRank();
            hasCritThisTurn = true;
        }
    }

    // 3. 회피 성공 조건
    public void OnEvade()
    {
        IncreaseRank();
    }

    // 4. 적 그로기 조건
    public void OnEnemyBreak()
    {
        IncreaseRank();
    }

    // 5. 피격 조건 (랭크 하락)
    public void OnPlayerHit()
    {
        DecreaseRank();
    }

    public void OnSupportActionUsed()
    {
        IncreaseRank();

        // 참고: 카린과 조력자는 셰리의 스킬 카테고리(검, 총 등)와 무관하므로
        // previousCategory 변수를 덮어쓰지 않고 랭크만 깔끔하게 올립니다!
    }

    // --- 내부 랭크 조절 로직 ---
    private void IncreaseRank()
    {
        if (currentRank < StyleRank.SSS)
        {
            currentRank++;
            DevLog.Log($"[스타일 랭크 UP!] 현재 랭크: {currentRank}");
            UpdateUI();
        }
    }

    private void DecreaseRank()
    {
        if (currentRank > StyleRank.None)
        {
            currentRank--;
            DevLog.Log($"[스타일 랭크 DOWN...] 현재 랭크: {currentRank}");
            UpdateUI();
        }
    }

    private void UpdateUI()
    {
        // UI 매니저에게 랭크 이미지를 바꾸라고 지시합니다.
        CombatUIManager.Instance.UpdateStyleRankUI(currentRank);
    }

    public void ResetRankForUltimate()
    {
        currentRank = StyleRank.None;
        previousCategory = SkillCategory.None;
        isFirstSkill = true;

        UpdateUI();
        DevLog.Log("[스타일 랭크] 궁극기 사용! 랭크가 None으로 초기화되었습니다.");
    }

    public float GetRankDamageMultiplier()
    {
        // 선형 증가: None(1.0배)부터 SSS(1.7배)까지 0.1배씩 증가합니다.
        switch (currentRank)
        {
            case StyleRank.None: return 1.0f;
            case StyleRank.D: return 1.1f;
            case StyleRank.C: return 1.2f;
            case StyleRank.B: return 1.3f;
            case StyleRank.A: return 1.4f;
            case StyleRank.S: return 1.5f;
            case StyleRank.SS: return 1.6f;
            case StyleRank.SSS: return 1.7f;
            default: return 1.0f;
        }
    }
}