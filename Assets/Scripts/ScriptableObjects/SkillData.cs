using UnityEngine;

public enum SkillCategory { Sword = 0, Gun = 1, Martial = 2, Magic = 3, Oni = 4 }

[CreateAssetMenu(fileName = "NewSkill", menuName = "GameData/Skill")]
public class SkillData : ScriptableObject
{
    [Header("다국어 번역 키")]
    public string skillNameKey; // 예: "skill_sword_slash"
    public string skillDescKey; // 예: "desc_sword_slash"

    [Header("스킬 로직 데이터")]
    public SkillCategory category; // 어느 카테고리에 속하는가?
    public float damageMultiplier = 1.0f; // 데미지 배율
    public int breakPower = 20; // 브레이크 깎는 수치
}