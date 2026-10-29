using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;
using Steamworks;

public class SessionService : MonoBehaviour
{
    public static SessionService I { get; private set; }

    NetworkManager net;
    Coroutine startCo;

    void Awake()
    {
        if (I && I != this) { Destroy(gameObject); return; }
        I = this;
    }

    void Start()
    {
        SteamLobbyService.I.OnGameStart += OnGameStart;
        SteamLobbyService.I.OnPartnerLeft += OnPartnerLeft;
    }

    // ?? Хост запускает игру ????????????????????????????????????
    public void HostGame()
    {
        SteamLobbyService.I.StartGame();    // это шлёт LobbyGameCreated всем, в т.ч. себе
    }

    // ?? Реакция на старт (приходит всем через Steam) ??????????
    void OnGameStart(ulong hostSteamId)
    {
        if (startCo != null) StopCoroutine(startCo);
        startCo = StartCoroutine(StartSession(hostSteamId));
    }

    IEnumerator StartSession(ulong hostSteamId)
    {
        net = FindObjectOfType<NetworkManager>();
        if (!net)
        {
            Debug.LogError("NetworkManager не найден. Добавь префаб на сцену или в Bootstrap.");
            yield break;
        }

        bool isHost = hostSteamId == SteamUser.GetSteamID().m_SteamID;
        string scene = GameSettings.Mode == ControlMode.VR
            ? "Tank_VR" : "Tank_PC";

        // Грузим свою сцену аддитивно (Menu остаётся для UI)
        var op = SceneManager.LoadSceneAsync(scene, LoadSceneMode.Additive);
        while (!op.isDone) yield return null;

        if (isHost)
        {
            net.StartHost();
        }
        else
        {
            // FizzySteamworks использует SteamID как адрес
            net.networkAddress = hostSteamId.ToString();
            net.StartClient();
        }
    }

    // ?? Партнёр вышел ?????????????????????????????????????????
    void OnPartnerLeft()
    {
        if (startCo != null) StopCoroutine(startCo);
        net?.StopHost();
        net?.StopClient();

        // Выгружаем игровую сцену, возвращаемся в меню
        string scene = GameSettings.Mode == ControlMode.VR ? "Tank_VR" : "Tank_PC";
        var s = SceneManager.GetSceneByName(scene);
        if (s.isLoaded) SceneManager.UnloadSceneAsync(s);

        SteamLobbyService.I.LeaveLobby();
        SceneManager.LoadScene("MainMenu");
    }

    public void LeaveSession()
    {
        OnPartnerLeft();
    }
}