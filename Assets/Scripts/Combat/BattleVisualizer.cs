using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

// Queues combat visuals so logic can wait for presentation timing.
public class BattleVisualizer : MonoBehaviour
{
    public static BattleVisualizer Instance;

    private Queue<IEnumerator> visualQueue = new Queue<IEnumerator>();
    private bool isPlaying = false;

    private Action onSequenceComplete;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public void StartSequence(Action onComplete)
    {
        onSequenceComplete = onComplete;
        if (!isPlaying && visualQueue.Count > 0)
        {
            StartCoroutine(PlayQueueRoutine());
        }
        else if (visualQueue.Count == 0)
        {
            CompleteSequence();
        }
    }

    private IEnumerator PlayQueueRoutine()
    {
        isPlaying = true;
        while (visualQueue.Count > 0)
        {
            yield return StartCoroutine(visualQueue.Dequeue());
        }
        isPlaying = false;
        CompleteSequence();
    }

    private void CompleteSequence()
    {
        Action sequenceComplete = onSequenceComplete;
        onSequenceComplete = null;
        sequenceComplete?.Invoke();
    }

    public void EnqueueVisual(IEnumerator visualRoutine)
    {
        visualQueue.Enqueue(visualRoutine);
    }

    // Runs gameplay logic at a specific point in the visual queue.
    public void EnqueueAction(Action logicAction)
    {
        visualQueue.Enqueue(ActionRoutine(logicAction));
    }

    private IEnumerator ActionRoutine(Action action)
    {
        action?.Invoke();
        yield return null;
    }

    public void EnqueueCutIn(Sprite cutInSprite)
    {
        if (cutInSprite != null)
            EnqueueVisual(CombatUIManager.Instance.ShowCutIn(cutInSprite));
    }

    public void EnqueueCommentary(string text, bool autoProceed = true, float delayAfter = 1.0f)
    {
        EnqueueVisual(CombatUIManager.Instance.TypeCommentary(text, autoProceed, delayAfter));
    }

    public void EnqueueLocalizedCommentary(string key, string fallback, object[] args = null, bool autoProceed = true, float delayAfter = 1.0f)
    {
        EnqueueVisual(CombatUIManager.Instance.TypeLocalizedCommentary(key, fallback, args, autoProceed, delayAfter));
    }

    public void EnqueueDelay(float seconds)
    {
        visualQueue.Enqueue(DelayRoutine(seconds));
    }

    private IEnumerator DelayRoutine(float seconds)
    {
        yield return new WaitForSeconds(seconds);
    }
}
