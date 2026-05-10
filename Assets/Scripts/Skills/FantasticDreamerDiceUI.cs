using UnityEngine;
using UnityEngine.UI; // Image 컴포넌트 접근을 위해 추가

public class FantasticDreamerDiceUI : MonoBehaviour
{
    [SerializeField] private GameObject[] diceObjects; // 주사위 오브젝트 배열 (인스펙터에서 할당됨)

    public void Setup(int count)
    {
        // 모든 주사위 비활성화
        foreach (var dice in diceObjects)
        {
            if (dice != null)
                dice.SetActive(false);
        }

        // 개수에 맞게 주사위 활성화
        int diceToActivate = Mathf.Min(count, diceObjects.Length);
        for (int i = 0; i < diceToActivate; i++)
        {
            if (diceObjects[i] != null)
                diceObjects[i].SetActive(true);
        }
    }

    // ==========================================
    // [신규 추가] 활성화된 주사위(버튼)들의 색상만 변경하는 함수
    // ==========================================
    public void SetDiceColor(Color color)
    {
        if (diceObjects == null) return;

        foreach (var dice in diceObjects)
        {
            // 활성화된 주사위 오브젝트만 대상으로 합니다.
            if (dice != null && dice.activeSelf)
            {
                // 주사위 오브젝트 자체에 붙어있는 Image 컴포넌트를 가져옵니다.
                Image diceImage = dice.GetComponent<Image>();
                if (diceImage != null)
                {
                    diceImage.color = color;
                }
            }
        }
    }
}