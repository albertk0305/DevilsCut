using UnityEngine;

[RequireComponent(typeof(Camera))]

//카메라 비율 고정해주는 함수. 메인 카메라에 넣으면 됨.
public class CameraResolution : MonoBehaviour
{
    void Start()
    {
        // 메인 카메라 컴포넌트를 가져옵니다.
        Camera camera = GetComponent<Camera>();

        // 현재 카메라가 화면에 그려지는 영역(Rect)을 가져옵니다.
        Rect rect = camera.rect;

        // 목표로 하는 16:9 비율 (16 / 9 = 약 1.777...)
        float targetRatio = 16f / 9f;

        // 현재 실행 중인 기기의 화면 비율
        float currentRatio = (float)Screen.width / Screen.height;

        // 화면 비율을 비교해서 남는 공간(검은 띠)을 계산합니다.
        float scaleHeight = currentRatio / targetRatio;

        // 1. 기기 화면이 16:9보다 위아래로 길쭉할 때 (예: 요즘 스마트폰)
        if (scaleHeight < 1f)
        {
            rect.height = scaleHeight;
            rect.y = (1f - scaleHeight) / 2f; // 위아래에 검은 띠(레터박스) 생성
        }
        // 2. 기기 화면이 16:9보다 양옆으로 넓을 때 (예: 태블릿, 구형 모니터)
        else
        {
            float scaleWidth = 1f / scaleHeight;
            rect.width = scaleWidth;
            rect.x = (1f - scaleWidth) / 2f; // 양옆에 검은 띠(필러박스) 생성
        }

        // 계산된 영역을 카메라에 다시 적용!
        camera.rect = rect;
    }
}