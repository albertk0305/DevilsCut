using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

// [분리] 전투의 시각적 연출과 타이밍을 전담하는 '대본(Queue)' 시스템입니다.
public class BattleVisualizer : MonoBehaviour
{
    public static BattleVisualizer Instance;

    // 연출 대본이 쌓이는 대기열(Queue)
    private Queue<IEnumerator> visualQueue = new Queue<IEnumerator>();
    private bool isPlaying = false;

    // 모든 연출이 끝났을 때 지휘관에게 보고할 콜백 함수
    private Action onSequenceComplete;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    // ==========================================
    // 1. 대본 실행 제어 (지휘관용)
    // ==========================================

    // 쌓여있는 대본(연출)을 순서대로 실행하고, 다 끝나면 지휘관에게 완료 보고를 합니다.
    public void StartSequence(Action onComplete)
    {
        onSequenceComplete = onComplete;
        if (!isPlaying && visualQueue.Count > 0)
        {
            StartCoroutine(PlayQueueRoutine());
        }
        else if (visualQueue.Count == 0)
        {
            // 연출할 대본이 없으면 즉시 완료 처리
            CompleteSequence();
        }
    }

    // 큐에서 연출을 하나씩 꺼내서 실행하는 무한 루프 코루틴
    private IEnumerator PlayQueueRoutine()
    {
        isPlaying = true;
        while (visualQueue.Count > 0)
        {
            // 앞의 연출이 끝날 때까지 완벽하게 기다렸다가 다음 연출로 넘어갑니다.
            yield return StartCoroutine(visualQueue.Dequeue());
        }
        isPlaying = false;
        CompleteSequence();
    }

    private void CompleteSequence()
    {
        onSequenceComplete?.Invoke();
        onSequenceComplete = null;
    }

    // ==========================================
    // 2. 대본 작성용 헬퍼 함수들 (Enqueue)
    // ==========================================

    // 코루틴(애니메이션 등)을 대본에 넣습니다.
    public void EnqueueVisual(IEnumerator visualRoutine)
    {
        visualQueue.Enqueue(visualRoutine);
    }

    // [핵심] 특정 연출 타이밍에 순수 로직(HP 깎기, 방송 켜기 등)을 실행하게 만듭니다.
    public void EnqueueAction(Action logicAction)
    {
        visualQueue.Enqueue(ActionRoutine(logicAction));
    }

    private IEnumerator ActionRoutine(Action action)
    {
        action?.Invoke(); // 로직 즉시 실행
        yield return null; // 1프레임 대기 후 다음 연출로 스무스하게 넘어감
    }

    // 컷인 연출 대본
    public void EnqueueCutIn(Sprite cutInSprite)
    {
        if (cutInSprite != null)
            EnqueueVisual(CombatUIManager.Instance.ShowCutIn(cutInSprite));
    }

    // 텍스트 타이핑 대본
    public void EnqueueCommentary(string text, bool autoProceed = true, float delayAfter = 1.0f)
    {
        EnqueueVisual(CombatUIManager.Instance.TypeCommentary(text, autoProceed, delayAfter));
    }

    // 단순 대기 시간 대본
    public void EnqueueDelay(float seconds)
    {
        visualQueue.Enqueue(DelayRoutine(seconds));
    }

    private IEnumerator DelayRoutine(float seconds)
    {
        yield return new WaitForSeconds(seconds);
    }
}