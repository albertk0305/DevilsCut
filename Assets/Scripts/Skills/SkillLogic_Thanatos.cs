using UnityEngine;

[CreateAssetMenu(fileName = "SkillLogic_Thanatos", menuName = "SkillLogic/Player/Thanatos")]
public class SkillLogic_Thanatos : SkillLogicBase
{
    [Header("디버프 데이터")]
    public StatusEffectData defDownDebuff; // 방어력 감소 SO 연결

    [Header("레벨별 방어력 감소율")]
    // 10%, 15%, 20% 감소
    public float[] defDownRates = { -0.10f, -0.15f, -0.20f };

    //  첫 번째 매개변수로 SkillData skill 추가
    public override void ApplyEffect(SkillData skill, PlayerStats pStats, EnemyData enemy, bool isPlayerAttacking)
    {
        if (isPlayerAttacking && defDownDebuff != null)
        {
            //  매개변수로 들어온 skill에서 바로 레벨을 꺼내 씁니다!
            int index = Mathf.Clamp(skill.skillLevel - 1, 0, defDownRates.Length - 1);
            float rate = defDownRates[index];

            // 적(false)에게 디버프를 3턴 동안 부여합니다!
            BuffManager.Instance.AddEffect(false, defDownDebuff, rate, 3);

            DevLog.Log($"[스킬 효과] 타나토스 적중! 적의 방어력이 3턴간 {Mathf.Abs(rate * 100)}% 감소합니다.");
        }
    }
}