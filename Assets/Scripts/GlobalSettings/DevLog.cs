using UnityEngine;
using System.Diagnostics; // [Conditional] 속성을 쓰기 위해 반드시 필요해!

// MonoBehaviour를 상속받지 않는 순수 도우미 클래스야
//Debug 대신 사용하는 함수. 빌드할때 포함 안됨. 최적화용.
public static class DevLog
{
    // [Conditional]은 "괄호 안의 조건이 맞을 때만 이 함수를 빌드에 포함시켜라!"라는 뜻이야.
    // UNITY_EDITOR: 유니티 에디터 안에서 플레이할 때
    // DEVELOPMENT_BUILD: 빌드 세팅에서 'Development Build'를 체크했을 때
    [Conditional("UNITY_EDITOR")]
    [Conditional("DEVELOPMENT_BUILD")]
    public static void Log(object message)
    {
        UnityEngine.Debug.Log(message);
    }

    [Conditional("UNITY_EDITOR")]
    [Conditional("DEVELOPMENT_BUILD")]
    public static void LogWarning(object message)
    {
        UnityEngine.Debug.LogWarning(message);
    }

    [Conditional("UNITY_EDITOR")]
    [Conditional("DEVELOPMENT_BUILD")]
    public static void LogError(object message)
    {
        UnityEngine.Debug.LogError(message);
    }
}