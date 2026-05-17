using UnityEngine;

public enum ItemGrade { Common, Rare, Epic, Legendary }
public enum ItemClass
{
    Saber, Shielder, Gunner, Assassin, Boxer,
    Beast, Caster, Trickster, Berserker, Demon, LoneWolf
}

[CreateAssetMenu(fileName = "NewEquipment", menuName = "GameData/EquipmentItem")]
public class EquipmentItemData : ScriptableObject
{
    [Header("기본 정보")]
    public string itemID;
    public string itemNameKey; // 다국어 Key
    public Sprite itemIcon;
    [TextArea] public string itemDescKey; // 다국어 Key
    [Header("장비 스탯 효과")]
    public string itemBonusKey; // 다국어 Key

    [Header("등급 및 시너지")]
    public ItemGrade grade;
    public ItemClass itemClass; // 어떤 시너지에 속하는가?

    // 현재 아이템의 성급 (인게임에서 동일 아이템 획득 시 1 -> 2 -> 3으로 증가)
    // ScriptableObject 원본이 오염되지 않도록 인벤토리 저장용 클래스에서 따로 래핑해서 쓰는 것을 권장합니다.
    [Range(1, 3)]
    public int currentStarLevel = 1;

    // 현재 아이템이 제공하는 시너지 점수 계산 함수
    public int GetSynergyPoints()
    {
        // 전설 등급은 합성이 불가능하고 기본적으로 2점의 시너지 가치를 지닙니다.
        if (grade == ItemGrade.Legendary) return 2;

        // 일반, 희귀, 에픽은 자신의 성급(1~3)만큼 점수를 제공합니다.
        return currentStarLevel;
    }

    [Header("1단계 합 연산 스탯 보너스 (1성/2성/3성)")]
    // 배열 크기는 항상 3 (1성, 2성, 3성 수치)
    public int[] flatStrength = new int[3];
    public int[] flatDefense = new int[3];
    public int[] flatSpeed = new int[3];
    public int[] flatLuck = new int[3];
    public int[] flatMaxHp = new int[3];
    public int[] flatAP = new int[3];
    public int[] flatBreakResistance = new int[3];

    [Header("2단계 곱 연산 스탯 보너스 (1성/2성/3성)")]
    // 예: 0.15f = 15% 증가
    public float[] pctStrength = new float[3];
    public float[] pctDefense = new float[3];
    public float[] pctSpeed = new float[3];
    public float[] pctLuck = new float[3];
    public float[] pctMaxHp = new float[3];
    public float[] pctAP = new float[3];

    [Header("특수 전투 보너스 (1성/2성/3성)")]
    public float[] finalDamageAmp = new float[3]; // 최종 피해 증폭 (%)
    public float[] finalDamageReduction = new float[3]; // 받는 최종 피해 감소 (%)
    public float[] critRateBonus = new float[3];  // 크리티컬 확률 합산 (%)
    public float[] critDamageBonus = new float[3]; // 크리티컬 피해량 합산 (%)
    public float[] lifeStealRate = new float[3];  // 흡혈률 (%)

    // ==========================================
    // 데이터 겟(Get) 헬퍼 함수들
    // ==========================================
    private int GetIndex(int starLevel) => Mathf.Clamp(starLevel - 1, 0, 2);

    // 시너지 점수 계산기
    public int GetSynergyPoints(int starLevel)
    {
        if (grade == ItemGrade.Legendary) return 2; // 전설은 2점
        return starLevel; // 나머지는 성급(1~3)만큼
    }

    // 스탯 수치 반환기
    public int GetFlatStr(int starLevel) => flatStrength.Length > 0 ? flatStrength[GetIndex(starLevel)] : 0;
    public int GetFlatDef(int starLevel) => flatDefense.Length > 0 ? flatDefense[GetIndex(starLevel)] : 0;
    public int GetFlatSpd(int starLevel) => flatSpeed.Length > 0 ? flatSpeed[GetIndex(starLevel)] : 0;
    public int GetFlatLuck(int starLevel) => flatLuck.Length > 0 ? flatLuck[GetIndex(starLevel)] : 0;
    public int GetFlatMaxHp(int starLevel) => flatMaxHp.Length > 0 ? flatMaxHp[GetIndex(starLevel)] : 0;
    public int GetFlatAP(int starLevel) => flatAP.Length > 0 ? flatAP[GetIndex(starLevel)] : 0;
    public int GetFlatBR(int starLevel) => flatBreakResistance.Length > 0 ? flatBreakResistance[GetIndex(starLevel)] : 0;

    public float GetPctStr(int starLevel) => pctStrength.Length > 0 ? pctStrength[GetIndex(starLevel)] : 0f;
    public float GetPctDef(int starLevel) => pctDefense.Length > 0 ? pctDefense[GetIndex(starLevel)] : 0f;
    public float GetPctSpd(int starLevel) => pctSpeed.Length > 0 ? pctSpeed[GetIndex(starLevel)] : 0f;
    public float GetPctLuck(int starLevel) => pctLuck.Length > 0 ? pctLuck[GetIndex(starLevel)] : 0f;
    public float GetPctMaxHp(int starLevel) => pctMaxHp.Length > 0 ? pctMaxHp[GetIndex(starLevel)] : 0f;
    public float GetPctAP(int starLevel) => pctAP.Length > 0 ? pctAP[GetIndex(starLevel)] : 0f;

    public float GetFinalDamageAmp(int sl) => finalDamageAmp.Length > 0 ? finalDamageAmp[GetIndex(sl)] : 0f;
    public float GetFinalDamageReduction(int sl) => finalDamageReduction.Length > 0 ? finalDamageReduction[GetIndex(sl)] : 0f;
    public float GetCritRateBonus(int sl) => critRateBonus.Length > 0 ? critRateBonus[GetIndex(sl)] : 0f;
    public float GetCritDamageBonus(int sl) => critDamageBonus.Length > 0 ? critDamageBonus[GetIndex(sl)] : 0f;
    public float GetLifeStealRate(int sl) => lifeStealRate.Length > 0 ? lifeStealRate[GetIndex(sl)] : 0f;
}