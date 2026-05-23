using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BattleBalanceDatabase", menuName = "DevilsCut/Battle Balance Database")]
public class BattleBalanceDatabase : ScriptableObject
{
    [SerializeField] private List<PhaseBattleBalance> phases = new List<PhaseBattleBalance>();

    public PhaseBattleBalance GetPhaseBalance(int phase)
    {
        for (int i = 0; i < phases.Count; i++)
        {
            if (phases[i] != null && phases[i].phase == phase)
                return phases[i];
        }

        Debug.LogError($"BattleBalanceDatabase: phase {phase} 데이터를 찾을 수 없습니다.");
        return null;
    }
}