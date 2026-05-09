using UnityEngine;
using UnityEngine.UI;

public class FantasticDreamerDiceUI : MonoBehaviour
{
    // 자식으로 만들어둔 주사위 이미지 5개를 인스펙터에서 연결합니다.
    public GameObject[] diceImages;

    public void Setup(int count)
    {
        // 0부터 5 사이로 제한
        count = Mathf.Clamp(count, 1, diceImages.Length);

        // 전체 주사위를 순회하며, 뽑힌 개수(count)만큼만 활성화합니다.
        for (int i = 0; i < diceImages.Length; i++)
        {
            // 예: count가 3이면 index 0,1,2는 켜지고 3,4는 꺼집니다.
            // Horizontal Layout Group이 알아서 켜진 3개만 가운데 정렬합니다.
            if (diceImages[i].activeSelf != (i < count))
            {
                diceImages[i].SetActive(i < count);
            }
        }
    }
}
