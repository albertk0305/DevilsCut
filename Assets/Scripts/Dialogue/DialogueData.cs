using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DialogueData", menuName = "DevilsCut/Dialogue/Dialogue Data")]
public class DialogueData : ScriptableObject
{
    public string dialogueID;
    public string nextSceneName;
    public bool useLineTSV;
    public TextAsset lineTSV;
    public List<DialogueLine> lines = new List<DialogueLine>();
}
