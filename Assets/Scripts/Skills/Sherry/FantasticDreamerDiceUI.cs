using UnityEngine;
using UnityEngine.UI;

public class FantasticDreamerDiceUI : MonoBehaviour
{
    [SerializeField] private GameObject[] diceObjects;

    public void Setup(int count)
    {
        foreach (var dice in diceObjects)
        {
            if (dice != null)
                dice.SetActive(false);
        }

        int diceToActivate = Mathf.Min(count, diceObjects.Length);
        for (int i = 0; i < diceToActivate; i++)
        {
            if (diceObjects[i] != null)
                diceObjects[i].SetActive(true);
        }
    }

    // ==========================================
    // ==========================================
    public void SetDiceColor(Color color)
    {
        if (diceObjects == null) return;

        foreach (var dice in diceObjects)
        {
            if (dice != null && dice.activeSelf)
            {
                Image diceImage = dice.GetComponent<Image>();
                if (diceImage != null)
                {
                    diceImage.color = color;
                }
            }
        }
    }
}