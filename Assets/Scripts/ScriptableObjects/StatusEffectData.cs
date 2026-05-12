using UnityEngine;

//  효과가 어떤 작용을 하는지 정의하는 열거형들
public enum EffectCategory { Buff, Debuff, Special }
public enum TargetStat { None, Strength, Defense, Speed, Luck, BreakResistance, AP }
public enum ModifierType { Flat, Percentage }
public enum SpecialEffectType { None, Guard, Reflect, AbsoluteGuard, EvasionUp, HpRegen, DamageAmp, TimeBomb, Overheat, DamageAccumulator, CyberRoulette, BreakRegen, Invincible, Stun }

[CreateAssetMenu(fileName = "NewStatusEffect", menuName = "GameData/StatusEffect")]
public class StatusEffectData : ScriptableObject
{
    public string effectID;       // 고유 ID 
    public string effectName;     // 효과 이름 
    public EffectCategory category;
    public Sprite icon;           // UI 아이콘

    [Header("효과 로직 설정")]
    public TargetStat targetStat;       // 올릴 스탯 (특수 효과면 None)
    public ModifierType modifierType;   // 합 연산(Flat)인지 곱 연산(Percentage)인지
    public SpecialEffectType specialType; // 가드, 독 등 특수 로직용

    [TextArea]
    public string baseDescription;

    // 합쳐진 수치 표현 (예: "(총 +{0})")
    public string valueFormat;
}