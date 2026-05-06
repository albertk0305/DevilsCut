#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

public class UIOptimizer : MonoBehaviour
{
    [MenuItem("Tools/UI 레이캐스트 최적화 (버튼 제외 전부 끄기)")]
    public static void OptimizeRaycasts()
    {
        int disabledCount = 0;

        // [수정됨] 최신 유니티 방식 적용! 
        // FindObjectsInactive.Include : 꺼져있는(비활성화된) UI도 포함해서 찾습니다.
        // FindObjectsSortMode.None : 불필요한 정렬을 생략해서 속도를 극대화합니다.
        Graphic[] allGraphics = FindObjectsByType<Graphic>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (Graphic graphic in allGraphics)
        {
            if (graphic.GetComponent<Button>() != null || 
                graphic.GetComponent<ScrollRect>() != null || 
                graphic.GetComponent<InputField>() != null ||
                graphic.GetComponent<TMP_InputField>() != null)
            {
                continue; 
            }

            if (graphic.raycastTarget)
            {
                Undo.RecordObject(graphic, "Disable Raycast Target");
                graphic.raycastTarget = false;
                disabledCount++;
            }
        }

        Debug.Log($"[UI 최적화 완료] 총 {disabledCount}개의 불필요한 레이캐스트 타겟을 껐습니다!");
    }
}
#endif