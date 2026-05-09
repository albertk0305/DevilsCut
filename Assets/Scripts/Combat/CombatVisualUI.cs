using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class CombatVisualUI : MonoBehaviour
{
    public TextMeshProUGUI commentaryText;
    public Image cutInImage;
    public GameObject critAlertImage;

    public GameObject damageTextPrefab;
    public Transform playerDamagePos;
    public Transform enemyDamagePos;
    private List<DamageText> damageTextPool = new List<DamageText>();

    private Coroutine currentCommentaryCoroutine;
    private Coroutine innerTypingCoroutine;

    [Header("판타스틱 드리머 연출 프리팹")]
    public FantasticDreamerDiceUI diceVisualPrefab;

    private FantasticDreamerDiceUI currentDiceInstance;

    public IEnumerator TypeCommentary(string message, bool autoProceed = true, float delayAfter = 1.5f)
    {
        StopTypingCoroutines();
        if (commentaryText != null) commentaryText.text = "";
        innerTypingCoroutine = StartCoroutine(TypewriterUtility.Instance.TypeText(commentaryText, message, autoProceed, delayAfter));
        yield return innerTypingCoroutine;
    }

    public void InterruptAndTypeCommentary(string message)
    {
        if (currentCommentaryCoroutine != null) StopCoroutine(currentCommentaryCoroutine);
        StopTypingCoroutines();
        if (commentaryText != null) commentaryText.text = "";
        currentCommentaryCoroutine = StartCoroutine(TypeCommentary(message, false, 0f));
    }

    private void StopTypingCoroutines()
    {
        if (innerTypingCoroutine != null) StopCoroutine(innerTypingCoroutine);
        if (TypewriterUtility.Instance != null) TypewriterUtility.Instance.StopAllCoroutines();
    }

    public IEnumerator ShowCutIn(Sprite cutInSprite)
    {
        if (cutInImage != null && cutInSprite != null)
        {
            cutInImage.gameObject.SetActive(true);
            cutInImage.sprite = cutInSprite;
            yield return new WaitForSeconds(1.5f);
            cutInImage.gameObject.SetActive(false);
        }
    }

    public void SpawnDamageText(string text, bool isCrit, bool isPlayerTarget)
    {
        if (damageTextPrefab == null) return;
        Transform targetPos = isPlayerTarget ? playerDamagePos : enemyDamagePos;
        Vector3 spawnPos = targetPos.position + new Vector3(Random.Range(-50f, 50f), Random.Range(-50f, 50f), 0);

        DamageText pooledText = null;
        foreach (var txt in damageTextPool)
        {
            if (!txt.gameObject.activeSelf) { pooledText = txt; break; }
        }

        if (pooledText == null)
        {
            GameObject dmgObj = Instantiate(damageTextPrefab, targetPos.parent);
            pooledText = dmgObj.GetComponent<DamageText>();
            damageTextPool.Add(pooledText);
        }

        pooledText.transform.position = spawnPos;
        pooledText.gameObject.SetActive(true);
        pooledText.Setup(text, isCrit);
    }

    public IEnumerator ShowCritAlert()
    {
        if (critAlertImage != null)
        {
            critAlertImage.SetActive(true);
            yield return new WaitForSeconds(1.0f);
            critAlertImage.SetActive(false);
        }
    }

    public void ClearCombatEffects()
    {
        if (critAlertImage != null) critAlertImage.SetActive(false);
        foreach (DamageText txt in damageTextPool)
        {
            if (txt.gameObject.activeSelf) txt.gameObject.SetActive(false);
        }
    }

    public void ShowDiceVisual(int count, bool isPlayerCaster)
    {
        if (diceVisualPrefab == null) return;

        // 1. 연출 대상 캐스팅 panel 찾기 (머리 위 위치 잡기용)
        // 기존 UIManager 분할 시 EntityStatusUI가 들어있는 Panel RectTransform을 찾습니다.
        EntityStatusUI casterUI = isPlayerCaster ? CombatUIManager.Instance.playerStatusUI : CombatUIManager.Instance.enemyStatusUI;

        if (casterUI == null) return;

        // 2. 프리팹 생성 (부모를 CasterUI의 부모인 Canvas로 설정해야 UI 우선순위가 잡힙니다.)
        currentDiceInstance = Instantiate(diceVisualPrefab, casterUI.transform.parent);

        // 3. 위치 잡기
        RectTransform rt = currentDiceInstance.GetComponent<RectTransform>();

        // 기준: 1920x1080 캔버스 정중앙(0,0) 앵커
        // 플레이어는 왼쪽(-350), 적은 오른쪽(+350)에 띄웁니다. 높이는 420으로 고정!
        float targetX = isPlayerCaster ? -350f : 350f;
        float targetY = 420f;

        rt.anchoredPosition = new Vector2(targetX, targetY);

        // 4. 주사위 개수 셋업
        currentDiceInstance.Setup(count);

        DevLog.Log($"[연출] 주사위 {count}개 머리 위에 표출");
    }

    public void ClearDiceVisual()
    {
        if (currentDiceInstance != null)
        {
            Destroy(currentDiceInstance.gameObject);
            currentDiceInstance = null;
        }
    }
}