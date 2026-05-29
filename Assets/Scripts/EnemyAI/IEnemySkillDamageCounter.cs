using UnityEngine;

public interface IEnemySkillDamageCounter
{
    bool CanCounterAfterSkillDamage();
    int GetCounterDamage(EnemyData enemy);
    float GetCounterBreakDamage();
    Sprite GetCounterImage(EnemyData enemy);
    string GetCounterMessage(int damage);
    void OnCounterTriggered(EnemyData enemy);
}
