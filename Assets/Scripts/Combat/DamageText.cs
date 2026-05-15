using UnityEngine;
using TMPro;

public class DamageText : MonoBehaviour
{
    public TextMeshProUGUI textMesh;
    public float moveSpeed = 2f;
    public float lifetime = 3.0f;

    private float defaultFontSize; // 재사용을 위한 기본 폰트 크기 기억

    private void Awake()
    {
        if (textMesh == null) textMesh = GetComponentInChildren<TextMeshProUGUI>();
        if (textMesh != null) defaultFontSize = textMesh.fontSize;
    }

    private void OnEnable()
    {
        // [최적화 2] 켜질 때마다 lifetime 초 뒤에 자신을 끄도록 예약 (Destroy 완전 대체)
        Invoke("Deactivate", lifetime);
    }

    private void OnDisable()
    {
        // 도중에 강제로 꺼지면 예약 취소 (메모리 꼬임 방지)
        CancelInvoke();
    }

    private void Deactivate()
    {
        gameObject.SetActive(false); // 수명이 다하면 스스로 꺼집니다.
    }

    public void Setup(string text, bool isCrit)
    {
        if (textMesh == null) return;

        textMesh.fontSize = defaultFontSize; // 재사용 시 폰트 크기 초기화

        // 1. 글자 몸통 색상은 무조건 깔끔한 흰색 고정!
        textMesh.color = Color.white;

        // 2. 텍스트 종류에 따라 테두리(Outline) 색상 결정
        Color outlineCol = Color.red; // 기본 테두리: 붉은색 (일반 타격)

        if (text == "Miss")
        {
            outlineCol = Color.blue;
        }
        else if (text.StartsWith("+"))
        {
            outlineCol = Color.green; // 체력 회복
        }
        else if (text.StartsWith("-"))
        {
            outlineCol = Color.red; // 코스트 지불
        }
        else if (text.StartsWith("★"))
        {
            // 벨페고르 전용 보라색 테두리 연출
            text = text.Replace("★", "");
            outlineCol = new Color(0.6f, 0.1f, 0.9f);

            // [신규] 잭팟 특수 연출: 문구가 최강의 패라면 폰트 크기를 대폭 키웁니다.
            if (text == "THE DEVIL'S HAND")
            {
                textMesh.fontSize += 40; // 크리티컬(+20)보다 두 배 더 큰 압박감!
            }
        }
        else if (text.StartsWith("♣"))
        {
            text = text.Replace("♣", "");
            outlineCol = new Color(0.2f, 0.8f, 0.2f); // 진한 초록색 (숙취 등 상태이상 연출용)
        }
        else if (isCrit)
        {
            text += "!"; // 느낌표 추가
            outlineCol = Color.yellow; // 크리티컬
            textMesh.fontSize += 20; // 크리티컬일 때만 폰트 키움
        }

        textMesh.text = text;

        // 3. TextMeshPro 테두리 적용
        // 두께가 너무 두껍거나 얇으면 0.2f 숫자를 입맛에 맞게 조절하세요! (보통 0.1f ~ 0.3f 사이)
        textMesh.outlineWidth = 0.2f;
        textMesh.outlineColor = outlineCol;
    }

    void Update()
    {
        transform.position += Vector3.up * moveSpeed * Time.deltaTime;
    }
}