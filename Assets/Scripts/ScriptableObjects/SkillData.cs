using UnityEngine;

public enum SkillCategory { Sword = 0, Gun = 1, Martial = 2, Magic = 3, Oni = 4, None = 5}

[CreateAssetMenu(fileName = "NewSkill", menuName = "GameData/Skill")]
public class SkillData : ScriptableObject
{
    [Header("스킬 고유 로직 (진화 포함)")]
    public SkillLogicBase skillLogic;

    [Header("다국어 번역 키")]
    public string skillNameKey; // 예: "skill_sword_slash"
    public string skillDescKey; // 예: "desc_sword_slash"

    [Header("스킬 기본 설정")]
    public SkillCategory category; // 적은 None으로 설정
    public int skillLevel = 1; // 적은 1로 설정

    [Header("스킬 연출")]
    public Sprite skillActionImage; // 스킬 사용 시 주인공 얼굴을 교체할 이미지!

    [Header("스킬 로직 데이터")]
    public float damageMultiplier = 1.0f; // 기본 딜 계수
    public float breakPower = 20f;        // 브레이크 깎는 수치
    public float baseAccuracy = 80f; // [추가] 기본 명중률
    public float bonusCritRate = 0f; // [추가] 스킬 고유 크리티컬 추가 확률 (예: 15f)
}