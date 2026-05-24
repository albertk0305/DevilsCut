using UnityEngine;

[CreateAssetMenu(fileName = "KarinData", menuName = "GameData/Karin")]
public class KarinData : ScriptableObject
{
    public string charNameKey; // "Karin"

    [Header("표정 이미지들")]
    public Sprite normal;
    public Sprite worried;
    public Sprite happy;
    public Sprite ready;
    public Sprite CutIn;
    public Sprite UsingItem;
    public Sprite battle;
}