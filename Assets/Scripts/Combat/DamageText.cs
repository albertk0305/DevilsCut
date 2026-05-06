using UnityEngine;
using TMPro;

public class DamageText : MonoBehaviour
{
    public TextMeshProUGUI textMesh;
    public float moveSpeed = 10f; // 위로 올라가는 속도
    public float lifetime = 1.0f; // 사라지기까지 걸리는 시간

    private void Awake()
    {
        if (textMesh == null)
        {
            textMesh = GetComponentInChildren<TextMeshProUGUI>();
        }
    }

    public void Setup(string text, bool isCrit)
    {
        if (textMesh == null) return;
        textMesh.text = text;

        // [신규 추가] 텍스트가 "Miss"라면 무조건 파란색으로 띄웁니다!
        if (text == "Miss")
        {
            textMesh.color = Color.blue;
        }
        else if (isCrit)
        {
            textMesh.text += "!";
            textMesh.color = Color.yellow; // 크리티컬은 노란색
            textMesh.fontSize += 20;       // 크리티컬은 글자도 더 크게!
        }
        else
        {
            textMesh.color = Color.red;    // 일반 적중은 붉은색
        }

        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        // 매 프레임마다 위쪽으로 조금씩 이동시킵니다.
        transform.position += Vector3.up * moveSpeed * Time.deltaTime;
    }
}