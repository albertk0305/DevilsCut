using UnityEngine;

//게임 매니저 최초 스폰해주는 코드
public static class GameManagerSpawner
{
    // 유니티가 플레이(▶) 버튼을 누르거나 빌드된 게임을 켤 때 '무조건 가장 먼저' 한 번 실행해 주는 마법의 코드
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void SpawnManagers()
    {
        // 1. 이미 씬에 매니저가 존재하는지 확인 (중복 소환 방지)
        if (Object.FindAnyObjectByType<LocalizationManager>() != null) return;

        // 2. Resources 폴더에 넣어둔 프리팹을 자동으로 찾아서 허공에 소환!
        GameObject prefab = Resources.Load<GameObject>("GlobalManagers");

        if (prefab != null)
        {
            Object.Instantiate(prefab);
            DevLog.Log("글로벌 매니저 자동 생성 완료!");
        }
        else
        {
            DevLog.LogError("Resources 폴더에서 'GlobalManagers' 프리팹을 찾을 수 없어!");
        }
    }
}