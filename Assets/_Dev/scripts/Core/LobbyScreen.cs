using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Steamworks;

public class LobbyScreen : MonoBehaviour
{
    [Header("Слоты игроков")]
    [SerializeField] PlayerSlotUI slotA;           // ты
    [SerializeField] PlayerSlotUI slotB;           // партнёр
    [SerializeField] private RawImage slotAAvatarImage;
    [SerializeField] private RawImage slotBAvatarImage;
    [Header("Список друзей")]
    [SerializeField] Transform friendListRoot;
    [SerializeField] FriendRowUI friendRowPrefab;

    [Header("Кнопки")]
    [SerializeField] Button btnReady;
    [SerializeField] Button btnInvite;
    [SerializeField] Button btnStart;
    [SerializeField] Button btnLeave;
    [SerializeField] TMP_Text lblStatus;
    [SerializeField] TMP_Text lblHint;
    private bool subscribed;
    SteamLobbyService lobby;
    bool myReady;
    readonly List<FriendRowUI> friendRows = new();
    public System.Action OnReturnToMenu;

    private void Awake()
    {
        // Не отключаем объект изнутри Awake.
        // Выключи LobbyScreen через Inspector.

        if (!ValidateUI())
        {
            Debug.LogError(
                "[LobbyScreen] Не все UI-поля назначены в Inspector.",
                this
            );
            return;
        }

        BindLobby();
    }
    private bool ValidateUI()
    {
        bool ok =
            slotA != null &&
            slotB != null &&
            friendListRoot != null &&
            friendRowPrefab != null &&
            btnReady != null &&
            btnInvite != null &&
            btnStart != null &&
            btnLeave != null &&
            lblStatus != null &&
            lblHint != null;

        if (!ok)
            Debug.LogError(
                "[LobbyScreen] Проверь SlotA, SlotB, FriendListRoot, " +
                "FriendRowPrefab, кнопки и TMP-тексты в Inspector.",
                this
            );

        return ok;
    }

    private bool BindLobby()
    {
        // Сервис мог ещё не существовать во время Awake.
        if (lobby == null)
            lobby = SteamLobbyService.I;

        if (lobby == null)
        {
            Debug.LogError(
                "[LobbyScreen] SteamLobbyService.I == null. " +
                "Проверь Bootstrap и порядок инициализации Steam.",
                this
            );
            return false;
        }

        if (subscribed)
            return true;

        lobby.OnJoined += OnJoined;
        lobby.OnMembersChanged += Refresh;
        lobby.OnPartnerLeft += OnPartnerLeft;

        btnReady.onClick.AddListener(ToggleReady);
        btnInvite.onClick.AddListener(OnInvitePressed);
        btnStart.onClick.AddListener(OnStartPressed);
        btnLeave.onClick.AddListener(OnLeavePressed);

        subscribed = true;
        return true;
    }
    private void OnDestroy()
    {
        if (lobby != null && subscribed)
        {
            lobby.OnJoined -= OnJoined;
            lobby.OnMembersChanged -= Refresh;
            lobby.OnPartnerLeft -= OnPartnerLeft;

            if (btnReady != null)
                btnReady.onClick.RemoveListener(ToggleReady);

            if (btnInvite != null)
                btnInvite.onClick.RemoveListener(OnInvitePressed);

            if (btnStart != null)
                btnStart.onClick.RemoveListener(OnStartPressed);

            if (btnLeave != null)
                btnLeave.onClick.RemoveListener(OnLeavePressed);
        }
    }
    private void OnInvitePressed()
    {
        if (lobby != null)
            lobby.OpenInviteOverlay();
    }

    private void LoadSteamAvatar(CSteamID steamId, RawImage target)
    {
        if (target == null || steamId == CSteamID.Nil)
            return;

        int avatarHandle = SteamFriends.GetLargeFriendAvatar(steamId);

        // Steam ещё загружает аватар
        if (avatarHandle == -1)
            return;

        // Аватар недоступен
        if (avatarHandle == 0)
        {
            Debug.LogWarning(
                $"[LobbyUI] Аватар недоступен для SteamID={steamId.m_SteamID}"
            );
            return;
        }

        bool success = SteamUtils.GetImageSize(
            avatarHandle,
            out uint width,
            out uint height
        );

        if (!success || width == 0 || height == 0)
            return;

        byte[] imageData = new byte[width * height * 4];

        success = SteamUtils.GetImageRGBA(
            avatarHandle,
            imageData,
            imageData.Length
        );

        if (!success)
            return;

        Texture2D texture = new Texture2D(
            (int)width,
            (int)height,
            TextureFormat.RGBA32,
            false
        );

        texture.LoadRawTextureData(imageData);
        texture.Apply();

        target.texture = texture;
    }

    public void Open()
    {
        if (!ValidateUI())
            return;

        if (!BindLobby())
        {
            lblStatus.text = "Ошибка: Steam-сервис не готов.";
            return;
        }

        gameObject.SetActive(true);

        myReady = false;
        lblStatus.text = "Создаём лобби...";
        btnStart.gameObject.SetActive(false);

        SetReady(false);

        lobby.CreateLobby();
        StartCoroutine(LoadFriends());
    }

    private void OnJoined(bool ok, string err)
    {
        Debug.Log(
        $"[LobbyScreen] OnJoined: ok={ok}, error={err}"
    );

        if (!ok)
        {
            Debug.LogError(
                $"[LobbyScreen] Не удалось войти в лобби: {err}"
            );

            if (lblStatus != null)
                lblStatus.text = $"Ошибка входа: {err}";

            return;
        }

        if (lblStatus == null || lblHint == null)
        {
            Debug.LogError(
                "[LobbyScreen] Не назначены LblStatus или LblHint.",
                this
            );
            return;
        }

        if (!ok)
        {
            lblStatus.text = "Ошибка: " + err;
            return;
        }

        if (lobby == null)
        {
            lblStatus.text = "Ошибка: Steam-сервис недоступен.";
            return;
        }

        lblStatus.text = lobby.IsHost
            ? "Лобби создано"
            : "Вы вошли в лобби";

        lobby.SetMyData(GameSettings.Mode.ToString(), false);

        lblHint.text =
            "Нажмите «Пригласить» или выберите друга из списка.";

        Refresh();
    }
    void Refresh()
    {
        var members = lobby.Members;
        var me = SteamUser.GetSteamID();

        CSteamID idA = members.Count > 0 ? members[0] : CSteamID.Nil;
        CSteamID idB = members.Count > 1 ? members[1] : CSteamID.Nil;

        slotA.SetPlayer(idA, lobby.GetMemberPlatform(idA), lobby.GetMemberReady(idA));
        slotB.SetPlayer(idB, lobby.GetMemberPlatform(idB), lobby.GetMemberReady(idB));
        Debug.Log(
    $"[LobbyUI] Members={members.Count}, " +
    $"SlotA SteamID={idA.m_SteamID}, " +
    $"Local SteamID={SteamUser.GetSteamID().m_SteamID}");
        CSteamID slotAId = lobby.Members[0];

        LoadSteamAvatar(slotAId, slotAAvatarImage);

        bool partner = members.Count >= 2;
        btnStart.gameObject.SetActive(lobby.IsHost && lobby.AllReady());
        lblHint.text = partner ? "" : "Ожидаем второго игрока...";
    }

    void ToggleReady()
    {
        myReady = !myReady;
        SetReady(myReady);
        lobby.SetMyData(GameSettings.Mode.ToString(), myReady);
        Refresh();
    }

    void SetReady(bool r)
    {
        btnReady.GetComponentInChildren<TMP_Text>().text = r ? "Не готов" : "Готов";
    }

    void OnStartPressed()
    {
        if (!lobby.AllReady()) return;
        SessionService.I.HostGame();
        gameObject.SetActive(false);
    }

    void OnLeavePressed()
    {
        if (lobby != null)
            lobby.LeaveLobby();

        myReady = false;

        gameObject.SetActive(false);
        OnReturnToMenu?.Invoke();
    }

    void OnPartnerLeft()
    {
        lblStatus.text = "Партнёр вышел";
        Refresh();
    }

    // ?? Список друзей ?????????????????????????????????????????
    IEnumerator LoadFriends()
    {
        // очистить старые строки
        foreach (var r in friendRows) Destroy(r.gameObject);
        friendRows.Clear();

        int count = SteamFriends.GetFriendCount(EFriendFlags.k_EFriendFlagImmediate);
        for (int i = 0; i < count; i++)
        {
            var id = SteamFriends.GetFriendByIndex(i, EFriendFlags.k_EFriendFlagImmediate);
            var state = SteamFriends.GetFriendPersonaState(id);
    
            var row = Instantiate(friendRowPrefab, friendListRoot);
            row.Set(id, () => lobby.InviteFriend(id));
            friendRows.Add(row);
            yield return null;    // не грузим всё за один кадр
        }
    }
}