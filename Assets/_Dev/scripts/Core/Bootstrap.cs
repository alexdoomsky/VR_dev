using UnityEngine;

public class Bootstrap : MonoBehaviour
{
    static bool created;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        if (created) return;
        created = true;
        var go = new GameObject("[Bootstrap]");
        DontDestroyOnLoad(go);
        go.AddComponent<SteamLobbyService>();
        go.AddComponent<SessionService>();
    }
}