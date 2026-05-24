using UnityEngine;

public enum StyleRank { None, D, C, B, A, S, SS, SSS }

public class StyleRankManager : MonoBehaviour
{
    public static StyleRankManager Instance;

    public StyleRank currentRank = StyleRank.None;

    private SkillCategory previousCategory;
    private bool isFirstSkill = true;
    private bool hasCritThisTurn = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public void InitCombat()
    {
        currentRank = StyleRank.None;
        previousCategory = SkillCategory.None;
        isFirstSkill = true;
        hasCritThisTurn = false;

        UpdateUI();
        DevLog.Log("[스타일 랭크] 전투 시작! 랭크가 초기화되었습니다.");
    }

    public void ResetTurnState()
    {
        hasCritThisTurn = false;
    }

    public void OnSkillUsed(SkillCategory usedCategory)
    {
        if (isFirstSkill || usedCategory != previousCategory)
        {
            IncreaseRank();
        }

        previousCategory = usedCategory;
        isFirstSkill = false;
    }

    public void OnCriticalHit()
    {
        if (!hasCritThisTurn)
        {
            IncreaseRank();
            hasCritThisTurn = true;
        }
    }

    public void OnEvade()
    {
        IncreaseRank();
    }

    public void OnEnemyBreak()
    {
        IncreaseRank();
    }

    public void OnPlayerHit()
    {
        DecreaseRank();
    }

    public void OnSupportActionUsed()
    {
        IncreaseRank();

        // Support actions raise rank without changing Sherry's previous skill category.
    }

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
        // None=0, D=1 ... SSS=7 maps to 1.0x through 1.7x.
        return 1.0f + ((int)currentRank * 0.1f);
    }

    public void IncreaseRank(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            if (currentRank < StyleRank.SSS)
            {
                currentRank++;
            }
        }
        DevLog.Log($"[스타일 랭크] 급상승! 현재 랭크: {currentRank}");
        UpdateUI();
    }
}
