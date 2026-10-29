using UnityEngine;

// Раньше Bootstrap создавал пустой объект и добавлял два скрипта, поэтому NetworkManager'а не было вообще.
// Теперь всё лежит в префабе Assets/Resources/NetworkRoot.prefab:
//   NetworkManager + FizzySteamworks + SteamLobbyService + SessionService
// Префаб создаётся до загрузки любой сцены, так что можно жать Play хоть из Tank_PC.
public static class Bootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        if (SessionService.I) return;

        var prefab = Resources.Load<GameObject>("NetworkRoot");
        if (!prefab)
        {
            Debug.LogError("[Bootstrap] Не найден Assets/Resources/NetworkRoot.prefab");
            return;
        }

        var go = Object.Instantiate(prefab);
        go.name = "[NetworkRoot]";
        Object.DontDestroyOnLoad(go);
    }
}
