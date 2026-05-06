using UnityEngine;
using System.Collections;
using TMPro; // 텍스트 제어에 필요
using UnityEngine.InputSystem; // 터치/클릭 감지에 필요
using UnityEngine.EventSystems; // UI 클릭 감지에 필수!

public class TypewriterUtility : MonoBehaviour
{
    // 어디서든 부를 수 있도록 싱글톤으로 만듭니다.
    public static TypewriterUtility Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬이 넘어가도 파괴되지 않게 유지합니다!
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 어떤 텍스트 창(targetText)이든 이 함수에 던져주면 타이핑 효과를 적용해 줍니다.
    public IEnumerator TypeText(TextMeshProUGUI targetText, string message, bool autoProceed = true, float delayAfter = 0.5f)
    {
        targetText.text = "";
        bool skipTyping = false;
        float typeSpeed = 0.03f; // 스피디한 텍스트 출력 속도

        // 1. 타이핑 연출
        for (int i = 0; i < message.Length; i++)
        {
            if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
            {
                // 핵심 로직: UI(버튼 등)를 클릭한 게 아닐 때만 스킵!
                if (!IsPointerOverUI())
                {
                    skipTyping = true;
                }
            }

            if (skipTyping)
            {
                targetText.text = message;
                break;
            }

            targetText.text += message[i];
            yield return new WaitForSeconds(typeSpeed);
        }

        targetText.text = message;

        // 2. 출력 완료 후 대기 연출
        if (autoProceed)
        {
            yield return null; // 이벤트 소모를 위해 한 프레임 쉼
            float timer = 0f;

            while (timer < delayAfter)
            {
                if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
                {
                    // 대기 중일 때도 UI 클릭이 아닐 때만 다음으로 즉시 넘어감!
                    if (!IsPointerOverUI()) break;
                }
                timer += Time.deltaTime;
                yield return null;
            }
        }
    }

    // 마우스나 모바일 터치가 현재 UI 요소(버튼, 패널 등)를 가리키고 있는지 검사하는 함수
    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;
        return EventSystem.current.IsPointerOverGameObject();
    }
}