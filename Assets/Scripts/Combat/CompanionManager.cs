using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// [분리] 전투 중 셰리를 돕는 우호적 NPC(카린, 조력자)의 턴 연출과 로직, 표정 변화를 전담합니다.
public class CompanionManager : MonoBehaviour
{
    public static CompanionManager Instance;

    [Header("카린 데이터")]
    public KarinData karinData; // CombatManager에서 이쪽으로 이사 왔습니다!

    public enum Emotion { Normal, Happy, Worried }

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    // ==========================================
    // 1. 표정(감정) 업데이트 기능
    // ==========================================
    public void UpdateEmotion(Emotion emotion)
    {
        Sprite kSprite = null;
        if (karinData != null)
        {
            switch (emotion)
            {
                case Emotion.Normal: kSprite = karinData.normal; break;
                case Emotion.Happy: kSprite = karinData.happy; break;
                case Emotion.Worried: kSprite = karinData.worried; break;
            }
        }

        Sprite sSprite = null;
        SupporterData supData = PlayerManager.Instance != null ? PlayerManager.Instance.activeSupporter : null;
        if (supData != null)
        {
            switch (emotion)
            {
                case Emotion.Normal: sSprite = supData.mainImage; break;
                case Emotion.Happy: sSprite = supData.happy; break;
                case Emotion.Worried: sSprite = supData.worried; break;
            }
        }

        if (CombatUIManager.Instance != null)
            CombatUIManager.Instance.UpdateProfileImages(kSprite, sSprite);
    }

    // ==========================================
    // 2. 카린(아이템) 턴 처리
    // ==========================================
    public IEnumerator ExecuteKarinTurn()
    {
        DevLog.Log("카린의 턴입니다!");
        yield return new WaitForSeconds(1.0f);

        KarinItemData equippedItem = PlayerManager.Instance.equippedKarinItem;
        if (equippedItem == null)
        {
            yield return StartCoroutine(CombatUIManager.Instance.TypeCommentary("카린: \"어라? 쓸 수 있는 물건이 없네!\""));
            CombatManager.Instance.ResolveTurnEnd();
            yield break;
        }

        yield return StartCoroutine(PerformKarinItemRoutine(equippedItem));
    }

    private IEnumerator PerformKarinItemRoutine(KarinItemData item)
    {
        if (karinData != null && karinData.CutIn != null)
        {
            CombatUIManager.Instance.InterruptAndTypeCommentary("카린의 차례입니다!");
            yield return StartCoroutine(CombatUIManager.Instance.ShowCutIn(karinData.CutIn));
        }
        if (karinData != null && karinData.battle != null)
            CombatUIManager.Instance.SetCasterImage(true, karinData.battle);

        string itemName = GetTranslatedText(item.itemName);
        Coroutine textCoroutine = StartCoroutine(CombatUIManager.Instance.TypeCommentary($"카린이 {itemName}을(를) 사용했습니다!"));

        int damage = 0;
        PlayerStats pStats = CombatManager.Instance.GetCurrentPlayerStats();
        EnemyData eData = CombatManager.Instance.GetCurrentEnemyData();

        if (item.itemLogic != null)
        {
            damage = item.itemLogic.CalculateDamage(pStats, eData);
            item.itemLogic.ApplyEffect(pStats, eData);
        }

        UpdateEmotion(Emotion.Happy);
        if (StyleRankManager.Instance != null) StyleRankManager.Instance.OnSupportActionUsed();

        bool isPlayerDefending = false;
        Sprite defenderHitSprite = eData != null ? eData.hit : null;

        if (damage > 0)
        {
            CombatUIManager.Instance.SetDefenderImage(isPlayerDefending, defenderHitSprite);
            CombatUIManager.Instance.SpawnDamageText(damage.ToString(), false, isPlayerDefending);

            // [핵심] 여기서 즉시 데미지 적용 및 체력바 업데이트
            bool isDead = CombatManager.Instance.ApplyDamageToEnemy(damage);

            if (isDead)
            {
                // 연타 도중 적이 죽었으면 즉시 승리 처리 후 코루틴 종료!
                yield return new WaitForSeconds(1.0f);
                CombatUIManager.Instance.ClearCombatEffects();
                CombatUIManager.Instance.ResetCasterImage(true);
                CombatManager.Instance.EndCombat(true);
                yield break;
            }
        }

        yield return new WaitForSeconds(2.0f);
        yield return textCoroutine;

        CombatUIManager.Instance.ClearCombatEffects();
        CombatUIManager.Instance.ResetCasterImage(true);
        if (damage > 0) CombatManager.Instance.RestoreDefenderImage(isPlayerDefending);

        CombatManager.Instance.ResolveTurnEnd();
    }

    // ==========================================
    // 3. 조력자(서포터) 턴 처리
    // ==========================================
    public IEnumerator ExecuteSupporterTurn(SupporterData supporter, bool isStartSkill)
    {
        string supName = GetTranslatedText(supporter.supporterName);
        Sprite cutIn = (isStartSkill && supporter.startSkillCutIn != null) ? supporter.startSkillCutIn : supporter.CutIn;
        if (cutIn != null)
        {
            string turnText = isStartSkill ? $"{supName}의 개전 지원!" : $"{supName}의 차례입니다!";
            CombatUIManager.Instance.InterruptAndTypeCommentary(turnText);
            yield return StartCoroutine(CombatUIManager.Instance.ShowCutIn(cutIn));
        }

        Sprite actionImage = isStartSkill ? supporter.startSkillImage : supporter.battleSkillImage;
        if (actionImage != null) CombatUIManager.Instance.SetCasterImage(true, actionImage);

        string skillType = isStartSkill ? "개전 스킬" : "전투 스킬";
        Coroutine textCoroutine = StartCoroutine(CombatUIManager.Instance.TypeCommentary($"{supName}의 {skillType} 발동!"));

        SupporterLogicBase logic = isStartSkill ? supporter.startSkillLogic : supporter.battleSkillLogic;
        int currentLevel = isStartSkill ? supporter.startSkillLevel : supporter.battleSkillLevel;
        PlayerStats pStats = CombatManager.Instance.GetCurrentPlayerStats();
        EnemyData eData = CombatManager.Instance.GetCurrentEnemyData();

        List<int> hitDamages = new List<int>();

        if (logic != null)
        {
            // 1. 다단히트인지 단일 타격인지 확인하여 리스트를 채웁니다.
            List<int> multiDamages = logic.CalculateMultiHitDamages(pStats, eData, currentLevel);
            if (multiDamages != null && multiDamages.Count > 0)
            {
                hitDamages = multiDamages;
            }
            else
            {
                int singleDamage = logic.CalculateDamage(pStats, eData, currentLevel);
                if (singleDamage > 0) hitDamages.Add(singleDamage);
            }

            // 2. 특수 효과(버프, 디버프 등) 우선 적용
            logic.ApplyEffect(pStats, eData, currentLevel);
        }

        UpdateEmotion(Emotion.Happy);
        if (!isStartSkill && StyleRankManager.Instance != null) StyleRankManager.Instance.OnSupportActionUsed();

        bool isPlayerDefending = false;

        if (hitDamages.Count > 0)
        {
            for (int i = 0; i < hitDamages.Count; i++)
            {
                int currentDamage = hitDamages[i];
                Sprite defenderHitSprite = eData != null ? eData.hit : null;

                CombatUIManager.Instance.SetDefenderImage(isPlayerDefending, defenderHitSprite);
                CombatUIManager.Instance.SpawnDamageText(currentDamage.ToString(), false, isPlayerDefending);

                // 타격마다 즉시 적 체력 차감
                bool isDead = CombatManager.Instance.ApplyDamageToEnemy(currentDamage);

                if (isDead)
                {
                    // 연타 도중 적이 죽었으면 즉시 승리 처리 후 코루틴 종료!
                    yield return new WaitForSeconds(1.0f);
                    CombatUIManager.Instance.ClearCombatEffects();
                    CombatUIManager.Instance.ResetCasterImage(true);
                    CombatManager.Instance.EndCombat(true);
                    yield break;
                }

                // 타격 간격 대기 (마지막 타격이 아닐 때만 0.15초 대기)
                if (i < hitDamages.Count - 1)
                {
                    yield return new WaitForSeconds(0.15f);
                    CombatUIManager.Instance.ResetDefenderImage(isPlayerDefending);
                }
            }
        }

        yield return new WaitForSeconds(2.0f);
        yield return textCoroutine;

        CombatUIManager.Instance.ClearCombatEffects();
        CombatUIManager.Instance.ResetCasterImage(true);
        if (hitDamages.Count > 0) CombatManager.Instance.RestoreDefenderImage(isPlayerDefending);

        if (!isStartSkill)
        {
            CombatManager.Instance.ResolveTurnEnd();
        }
    }

    private string GetTranslatedText(string key)
    {
        if (string.IsNullOrEmpty(key)) return "";
        if (LocalizationManager.Instance != null) return LocalizationManager.Instance.GetText(key);
        return key;
    }
}