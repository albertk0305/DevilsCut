using System.Collections.Generic;
using UnityEngine;

// [분리] 버프 및 디버프의 저장, 갱신, UI 전달을 전담하는 매니저입니다.
public class BuffManager : MonoBehaviour
{
    public static BuffManager Instance;

    // 전투 중 부여되는 개별 효과 데이터 구조
    public class ActiveEffect
    {
        public StatusEffectData effectData;
        public float value;
        public int turnsLeft;
        public bool isNewlyApplied; // 부여된 턴에 바로 감소하는 것을 막는 플래그
    }

    private List<ActiveEffect> playerEffects = new List<ActiveEffect>();
    private List<ActiveEffect> enemyEffects = new List<ActiveEffect>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public void ClearAllEffects()
    {
        playerEffects.Clear();
        enemyEffects.Clear();
    }

    public List<ActiveEffect> GetEffects(bool isPlayer)
    {
        return isPlayer ? playerEffects : enemyEffects;
    }

    private Dictionary<StatusEffectData, float> cachedGroupedEffects = new Dictionary<StatusEffectData, float>();

    // UI 출력을 위해 같은 종류의 버프 수치를 합쳐주는 헬퍼 함수
    public Dictionary<StatusEffectData, float> GetGroupedEffects(bool isPlayer)
    {
        var list = isPlayer ? playerEffects : enemyEffects;

        // [최적화] 매번 new Dictionary를 만들지 않고, 기존 것을 비워서 재사용 (가비지 컬렉터 최적화)
        cachedGroupedEffects.Clear();

        foreach (var effect in list)
        {
            if (cachedGroupedEffects.ContainsKey(effect.effectData))
                cachedGroupedEffects[effect.effectData] += effect.value;
            else
                cachedGroupedEffects.Add(effect.effectData, effect.value);
        }
        return cachedGroupedEffects;
    }

    // 버프/디버프 부여 로직
    public void AddEffect(bool isPlayerTarget, StatusEffectData data, float value, int turns)
    {
        var list = isPlayerTarget ? playerEffects : enemyEffects;

        // CombatManager에게 물어봐서 '셀프 버프(자신의 턴에 자신에게 검)'인지 확인합니다.
        bool isSelfBuff = CombatManager.Instance != null && CombatManager.Instance.IsCurrentTurnOwner(isPlayerTarget);

        list.Add(new ActiveEffect
        {
            effectData = data,
            value = value,
            turnsLeft = turns,
            isNewlyApplied = isSelfBuff
        });

        DevLog.Log($"[효과 부여] {(isPlayerTarget ? "아군" : "적")}에게 {data.effectName} 적용! (수치: {value}, {turns}턴)");
        if (CombatUIManager.Instance != null) CombatUIManager.Instance.RefreshBuffUI();
    }

    // 턴 종료 시 남은 턴수 차감 및 만료된 버프 삭제
    public void UpdateEffectsOnTurnEnd(bool isPlayerTarget)
    {
        var list = isPlayerTarget ? playerEffects : enemyEffects;
        bool hasChanged = false;

        // 리스트를 삭제할 때는 역순(Reverse) 순회가 가장 안전하고 최적화된 표준 방식입니다.
        for (int i = list.Count - 1; i >= 0; i--)
        {
            if (list[i].isNewlyApplied)
            {
                list[i].isNewlyApplied = false;
                continue;
            }

            list[i].turnsLeft--;
            hasChanged = true;

            if (list[i].turnsLeft <= 0)
            {
                list.RemoveAt(i);
            }
        }

        if (hasChanged && CombatUIManager.Instance != null)
            CombatUIManager.Instance.RefreshBuffUI();
    }

    // [최적화] 가드 스택 소모 로직을 캡슐화 (CombatManager가 리스트를 직접 건드리지 않게 함)
    public void ConsumeGuardEffect(bool isPlayerTarget)
    {
        var list = isPlayerTarget ? playerEffects : enemyEffects;
        ActiveEffect targetGuard = null;
        int minTurns = int.MaxValue;

        foreach (var e in list)
        {
            if (e.effectData.specialType == SpecialEffectType.Guard || e.effectData.specialType == SpecialEffectType.AbsoluteGuard)
            {
                if (e.turnsLeft < minTurns)
                {
                    minTurns = e.turnsLeft;
                    targetGuard = e;
                }
            }
        }

        if (targetGuard != null)
        {
            list.Remove(targetGuard);
            if (CombatUIManager.Instance != null) CombatUIManager.Instance.RefreshBuffUI();
        }
    }
}