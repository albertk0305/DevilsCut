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

        textMesh.text = text;
        textMesh.fontSize = defaultFontSize; // 재사용 시 폰트 크기 초기화

        if (text == "Miss")
        {
            textMesh.color = Color.blue;
        }
        else if (isCrit)
        {
            textMesh.text += "!";
            textMesh.color = Color.yellow;
            textMesh.fontSize += 20; // 크리티컬일 때만 폰트 키움
        }
        else
        {
            textMesh.color = Color.red;
        }
    }

    void Update()
    {
        transform.position += Vector3.up * moveSpeed * Time.deltaTime;
    }
}