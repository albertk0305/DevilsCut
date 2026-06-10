using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DialogueData", menuName = "DevilsCut/Dialogue/Dialogue Data")]
public class DialogueData : ScriptableObject
{
    public string dialogueID;
    public string nextDialogueID;
    public string nextSceneName;
    public DialogueSkipPolicy storySkipPolicy = DialogueSkipPolicy.NeverSkip;

    [Header("BGM")]
    public List<AudioClip> bgmPlaylist = new List<AudioClip>();
    public AudioClip bgmClip;
    public float bgmFadeTime = 0.5f;
    public bool stopBgmIfEmpty = false;

    [Header("Visuals")]
    public string initialBackgroundID;
    public bool useLineTSV;
    public TextAsset lineTSV;
    public List<DialogueLine> lines = new List<DialogueLine>();
}
