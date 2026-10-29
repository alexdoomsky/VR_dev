using System;
using System.Collections;
using Mirror;
using Steamworks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

// Порядок: Steam-лобби -> старт -> каждый грузит СВОЮ сцену -> хост (ПК) StartHost -> клиент (VR) StartClient
// -> оба NetPlayer на месте -> хост спавнит TankLink (владелец = VR) -> игра.
public class SessionService : MonoBehaviour
{
    public static SessionService I { get; private set; }
    public static string MenuMessage;   // можно показать в меню после возврата

    [Header("Сцены (все в Build Settings)")]
    [SerializeField] string vrScene = "Tank_VR";
    [SerializeField] string pcScene = "Tank_PC";
    [SerializeField] string menuScene = "MainMenu";
    [Tooltip("Общая карта, если вынесена в отдельную сцену. Пусто = карта лежит внутри Tank_VR/Tank_PC.")]
    [SerializeField] string levelScene = "";

    [Header("Сеть")]
    [SerializeField] NetworkManager networkManager;   // перетащи сюда в Inspector на NetworkRoot
    [SerializeField] GameObject linkPrefab;
    [SerializeField] float hostWaitTimeout = 90f;
    [SerializeField] float connectTimeout = 30f;
    [SerializeField] float partnerTimeout = 120f;

    Coroutine co;
    bool inGame, waitOk;

    void Awake()
    {
        if (I && I != this) { Destroy(gameObject); return; }
        I = this;
    }

    void Start()
    {
        if (I != this) return;
        var lobby = SteamLobbyService.I;
        if (!lobby) return;
        lobby.OnGameStart += OnGameStart;
        lobby.OnPartnerLeft += OnPartnerLeft;
    }

    void OnDestroy()
    {
        if (I != this) return;
        var lobby = SteamLobbyService.I;
        if (!lobby) return;
        lobby.OnGameStart -= OnGameStart;
        lobby.OnPartnerLeft -= OnPartnerLeft;
    }

    // Кнопка «Старт» у владельца лобби
    public void HostGame() => SteamLobbyService.I.StartGame();

    void OnGameStart(ulong hostSteamId)
    {
        if (inGame) return;   // Steam может прислать событие дважды
        inGame = true;
        co = StartCoroutine(Run(hostSteamId));
    }

    void OnPartnerLeft()
    {
        // В лобби (до старта) выход партнёра игру не рушит
        if (inGame) LeaveSession("Партнёр вышел из игры");
    }

    IEnumerator Run(ulong hostId)
    {
        Debug.Log($"[Session] Run: hostId={hostId}, я={SteamUser.GetSteamID().m_SteamID}, isHost={hostId == SteamUser.GetSteamID().m_SteamID}, mode={GameSettings.Mode}");

        var net = networkManager;
        if (!net) net = GetComponent<NetworkManager>();       // fallback: ищем на том же объекте
        if (!net) net = FindObjectOfType<NetworkManager>();   // последний шанс
        Debug.Log($"[Session] NetworkManager: {(net ? net.name : "NULL")}");
        if (!net) { LeaveSession("NetworkManager не найден: проверь префаб NetworkRoot"); yield break; }
        bool isHost = hostId == SteamUser.GetSteamID().m_SteamID;
        if (isHost && !linkPrefab) { LeaveSession("Не назначен префаб TankLink"); yield break; }

        // Закомментируй на время теста:
        // if (isHost && GameSettings.IsVR) { LeaveSession("Хостом должен быть игрок на ПК"); yield break; }
        // if (!isHost && !GameSettings.IsVR) { LeaveSession("Клиентом должен быть VR-игрок"); yield break; }

        if (isHost && !linkPrefab) { LeaveSession("В SessionService не назначен префаб TankLink"); yield break; }


        // 1. Своя сцена. Single: меню, его камеры и EventSystem уходят, [NetworkRoot] остаётся.
        yield return SceneManager.LoadSceneAsync(GameSettings.IsVR ? vrScene : pcScene);
        if (!string.IsNullOrEmpty(levelScene))
        {
            yield return SceneManager.LoadSceneAsync(levelScene, LoadSceneMode.Additive);
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(levelScene));   // сюда Mirror спавнит объекты
        }

        // 2. Подключение
        if (isHost)
        {
            net.StartHost();
            SteamLobbyService.I.SetHostUp();   // сигнал клиенту: можно подключаться

            yield return Wait(() => NetPlayer.All.Count >= 2, partnerTimeout);
            if (!waitOk) { LeaveSession("Второй игрок не подключился"); yield break; }

            SpawnLink();
        }
        else
        {
            yield return Wait(() => SteamLobbyService.I.IsHostUp(hostId), hostWaitTimeout);
            if (!waitOk) { LeaveSession("Хост не запустил игру"); yield break; }

            net.networkAddress = hostId.ToString();   // FizzySteamworks: адрес = SteamID хоста
            net.StartClient();

            yield return Wait(() => NetworkClient.isConnected, connectTimeout);
            if (!waitOk) { LeaveSession("Не удалось подключиться к хосту"); yield break; }

            yield return Wait(() => TankLink.Instance != null, connectTimeout);
            if (!waitOk) { LeaveSession("Связь с танком не установлена"); yield break; }
        }

        // 3. Игра идёт. Разрыв связи или выход партнёра = в меню.
        while (true)
        {
            if (!NetworkClient.isConnected || NetPlayer.All.Count < 2)
            {
                LeaveSession("Партнёр вышел из игры");
                yield break;
            }
            yield return null;
        }
    }

    void SpawnLink()
    {
        var vr = NetPlayer.All.Find(p => !p.isLocalPlayer);   // владелец = VR-игрок (клиент): он публикует позу корпуса

        // Появляемся там, где стоит ПК-танк, чтобы он не «прыгнул» до первого пакета от VR
        var follower = FindObjectOfType<HullFollower>();
        Vector3 pos = follower ? follower.transform.position : Vector3.zero;
        Quaternion rot = follower ? follower.transform.rotation : Quaternion.identity;

        var link = Instantiate(linkPrefab, pos, rot);
        NetworkServer.Spawn(link, vr != null ? vr.connectionToClient : null);
    }

    IEnumerator Wait(Func<bool> condition, float timeout)
    {
        waitOk = false;
        for (float t = 0f; t < timeout; t += Time.unscaledDeltaTime)
        {
            if (condition()) { waitOk = true; yield break; }
            yield return null;
        }
    }

    public void LeaveSession(string message = null)
    {
        if (co != null) { StopCoroutine(co); co = null; }
        inGame = false;

        var nm = NetworkManager.singleton;
        if (nm)
        {
            if (NetworkServer.active && NetworkClient.active) nm.StopHost();
            else if (NetworkClient.active) nm.StopClient();
            else if (NetworkServer.active) nm.StopServer();
        }
        NetPlayer.All.Clear();

        if (SteamLobbyService.I) SteamLobbyService.I.LeaveLobby();

        MenuMessage = message;
        SceneManager.LoadScene(menuScene);
    }
}
