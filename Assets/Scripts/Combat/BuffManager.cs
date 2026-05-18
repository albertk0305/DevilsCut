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

    // [신규 추가] 실시간 특수 스탯 변동을 귀속 패시브 아이콘으로 UI에 반영하는 핵심 함수!
    // 값이 존재하면 999턴 귀속 버프로 띄우고, 변동되면 수치 갱신, 0(또는 기본값)이 되면 버프창에서 제거합니다.
    public void UpdatePermanentPassive(bool isPlayer, StatusEffectData data, float currentValue, float defaultValue = 0f)
    {
        if (data == null) return;
        data.isPermanentPassive = true; // 강제 귀속 플래그 세팅

        var list = isPlayer ? playerEffects : enemyEffects;
        var existing = list.Find(e => e.effectData == data);

        // 기본값과 다르다면 (즉, 효과가 활성화되어 있다면) 버프창에 유지/갱신
        if (Mathf.Abs(currentValue - defaultValue) > 0.001f)
        {
            if (existing != null)
            {
                existing.value = currentValue - defaultValue; // 변동된 순수 보너스 수치 반영
            }
            else
            {
                list.Add(new ActiveEffect
                {
                    effectData = data,
                    value = currentValue - defaultValue,
                    turnsLeft = 999, // 영구 유지
                    isNewlyApplied = false
                });
            }
        }
        else
        {
            // 수치가 기본값으로 돌아왔다면 버프 아이콘 제거
            if (existing != null) list.Remove(existing);
        }

        if (CombatUIManager.Instance != null) CombatUIManager.Instance.RefreshBuffUI();
    }

    // 버프/디버프 부여 로직
    public void AddEffect(bool isPlayerTarget, StatusEffectData data, float val, int duration)
    {
        if (data == null) return;

        var list = isPlayerTarget ? playerEffects : enemyEffects;

        list.Add(new ActiveEffect
        {
            effectData = data,
            value = val,
            turnsLeft = data.isPermanentPassive ? 999 : duration, // 귀속 패시브는 무조건 999턴 고정
            isNewlyApplied = !data.isPermanentPassive // 패시브는 새 버프 보호 플래그 제외
        });

        if (CombatUIManager.Instance != null) CombatUIManager.Instance.RefreshBuffUI();
    }


    public void AdvanceTurnActiveEffects(bool isPlayerTurn)
    {
        var list = isPlayerTurn ? playerEffects : enemyEffects;
        bool hasChanged = false;

        for (int i = list.Count - 1; i >= 0; i--)
        {
            // [수정] 귀속 패시브 종류는 턴 감소 로직을 완벽하게 패스합니다! (영원히 999턴 유지)
            if (list[i].effectData.isPermanentPassive) continue;

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
            // 패시브 가드가 아니라면 소모 처리
            if (!targetGuard.effectData.isPermanentPassive)
            {
                list.Remove(targetGuard);
                if (CombatUIManager.Instance != null) CombatUIManager.Instance.RefreshBuffUI();
                DevLog.Log($"[BuffManager] {(isPlayerTarget ? "아군" : "적군")}의 가드 버프 1스택이 소모되었습니다.");
            }
        }
    }
}