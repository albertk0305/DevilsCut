using UnityEngine;
using TMPro;

public class DamageText : MonoBehaviour
{
    public TextMeshProUGUI textMesh;
    public float moveSpeed = 2f;
    public float lifetime = 3.0f;

    private float defaultFontSize;

    private void Awake()
    {
        if (textMesh == null) textMesh = GetComponentInChildren<TextMeshProUGUI>();
        if (textMesh != null) defaultFontSize = textMesh.fontSize;
    }

    private void OnEnable()
    {
        Invoke("Deactivate", lifetime);
    }

    private void OnDisable()
    {
        CancelInvoke();
    }

    private void Deactivate()
    {
        gameObject.SetActive(false);
    }

    public void Setup(string text, bool isCrit)
    {
        if (textMesh == null) return;

        textMesh.fontSize = defaultFontSize;

        textMesh.color = Color.white;

        Color outlineCol = Color.red;

        if (text == "Miss")
        {
            outlineCol = Color.blue;
        }
        else if (text.StartsWith("+"))
        {
            outlineCol = Color.green;
        }
        else if (text.StartsWith("-"))
        {
            outlineCol = Color.red;
        }
        else if (text.StartsWith("★"))
        {
            text = text.Replace("★", "");
            outlineCol = new Color(0.6f, 0.1f, 0.9f);

            if (text == "THE DEVIL'S HAND")
            {
                textMesh.fontSize += 40;
            }
        }
        else if (text.StartsWith("♣"))
        {
            text = text.Replace("♣", "");
            outlineCol = new Color(0.2f, 0.8f, 0.2f);
        }
        else if (isCrit)
        {
            text += "!";
            outlineCol = Color.yellow;
            textMesh.fontSize += 20;
        }

        textMesh.text = text;

        textMesh.outlineWidth = 0.2f;
        textMesh.outlineColor = outlineCol;
    }

    void Update()
    {
        transform.position += Vector3.up * moveSpeed * Time.deltaTime;
    }
}
