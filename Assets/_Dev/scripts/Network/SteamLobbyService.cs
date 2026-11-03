using Steamworks;
using System;
using System.Collections.Generic;
using UnityEngine;

public class SteamLobbyService : MonoBehaviour
{
    public static SteamLobbyService I { get; private set; }
    const string KEY_HOST = "mirrorhost", KEY_HOSTUP = "hostup";
    Callback<LobbyDataUpdate_t> cbDataUpdate;
    // ?? Ñîñòîÿíèå ??????????????????????????????????????????????
    public CSteamID LobbyId { get; private set; }
    public bool InLobby => LobbyId.IsValid() && LobbyId != CSteamID.Nil;
    public bool IsHost { get; private set; }
    public List<CSteamID> Members { get; } = new();

    public event Action<bool, string> OnJoined;          // joined ok + error
    public event Action OnMembersChanged;
    public event Action<string, string> OnChatMsg;       // (èìÿ, òåêñò)
    public event Action<ulong> OnGameStart;              // hostSteamId
    public event Action OnPartnerLeft;

    // ?? Êîëáýêè Steam ??????????????????????????????????????????
    Callback<LobbyCreated_t> cbCreated;
    Callback<GameLobbyJoinRequested_t> cbJoinReq;
    Callback<LobbyEnter_t> cbEnter;
    Callback<LobbyChatUpdate_t> cbUpdate;
    Callback<LobbyChatMsg_t> cbChat;
    Callback<LobbyGameCreated_t> cbGameCreated;

    const string KEY_PLATFORM = "platform";   // "VR" / "PC"
    const string KEY_READY = "ready";      // "1"
    const string KEY_VERSION = "version";    // çàùèòà îò ðàçíûõ áèëäîâ

    void Awake()
    {
        if (I && I != this) { Destroy(gameObject); return; }
        I = this;

        if (!SteamManager.Initialized) return;
        cbDataUpdate = Callback<LobbyDataUpdate_t>.Create(OnDataUpdate);

        cbCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        cbJoinReq = Callback<GameLobbyJoinRequested_t>.Create(OnJoinRequested);
        cbEnter = Callback<LobbyEnter_t>.Create(OnLobbyEnter);
        cbUpdate = Callback<LobbyChatUpdate_t>.Create(OnChatUpdate);
        cbChat = Callback<LobbyChatMsg_t>.Create(OnChatMessage);
        cbGameCreated = Callback<LobbyGameCreated_t>.Create(OnGameCreated);
    }

    public void SetHostUp() =>
    SteamMatchmaking.SetLobbyMemberData(LobbyId, KEY_HOSTUP, "1");
    public bool IsHostUp(ulong hostId) =>
    SteamMatchmaking.GetLobbyMemberData(LobbyId, new CSteamID(hostId), KEY_HOSTUP) == "1";
    void OnDataUpdate(LobbyDataUpdate_t p)
    {
        // Ñðàáàòûâàåò êîãäà ëþáîé ó÷àñòíèê ìåíÿåò SetLobbyMemberData
        if (new CSteamID(p.m_ulSteamIDLobby) != LobbyId) return;
        OnMembersChanged?.Invoke();
    }
    public void InviteFriend(CSteamID friendId)
    {
        if (!InLobby)
        {
            Debug.LogError("[SteamLobby] Íåëüçÿ ïðèãëàñèòü äðóãà: òû íå íàõîäèøüñÿ â ëîááè.");
            return;
        }

        bool sent = SteamMatchmaking.InviteUserToLobby(
            LobbyId,
            friendId
        );

        Debug.Log(
            $"[SteamLobby] Ïðèãëàøåíèå: " +
            $"FriendID={friendId.m_SteamID}, " +
            $"LobbyID={LobbyId.m_SteamID}, " +
            $"Sent={sent}"
        );

        if (!sent)
        {
            Debug.LogError(
                "[SteamLobby] Steam íå îòïðàâèë ïðèãëàøåíèå. " +
                "Ïðîâåðü, ÷òî äðóã äîñòóïåí è ëîááè äåéñòâèòåëüíî ñóùåñòâóåò."
            );
        }
    }

    public void CreateLobby()
    {
        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, 2);
    }

    // ?? Ïðèñîåäèíèòüñÿ ïî ID ???????????????????????????????????
    public void JoinLobby(CSteamID id)
    {
        SteamMatchmaking.JoinLobby(id);
    }

    // ?? Âûéòè èç ëîááè ????????????????????????????????????????
    public void LeaveLobby()
    {
        if (!InLobby) return;
        SteamMatchmaking.LeaveLobby(LobbyId);
        LobbyId = CSteamID.Nil;
        Members.Clear();
        IsHost = false;
    }

    // ?? Îïóáëèêîâàòü ñâîè äàííûå ???????????????????????????????
    public void SetMyData(string platform, bool ready)
    {
        if (!InLobby) return;
        var me = SteamUser.GetSteamID();
        SteamMatchmaking.SetLobbyMemberData(LobbyId, KEY_PLATFORM, platform);
        SteamMatchmaking.SetLobbyMemberData(LobbyId, KEY_READY, ready ? "1" : "0");
        SteamMatchmaking.SetLobbyMemberData(LobbyId, "platform",GameSettings.Mode == ControlMode.VR ? "VR" : "PC");
    }

    // ?? Îòïðàâèòü ñîîáùåíèå â ÷àò ëîááè ???????????????????????
    public void SendChat(string text)
    {
        if (!InLobby) return;
        var bytes = System.Text.Encoding.UTF8.GetBytes(text);
        SteamMatchmaking.SendLobbyChatMsg(LobbyId, bytes, bytes.Length);
    }

    // ?? Çàïóñòèòü èãðó (òîëüêî õîñò) ??????????????????????????
    public void StartGame()
    {
        if (!IsHost || !InLobby) return;
        var me = SteamUser.GetSteamID();
        SteamMatchmaking.SetLobbyGameServer(LobbyId, 0, 0, me);
    }

    // ?? Ïðèãëàñèòü äðóãa ??????????????????????????????????????
    public void OpenInviteOverlay()
    {
        if (!InLobby) return;
        SteamFriends.ActivateGameOverlayInviteDialog(LobbyId);
    }

    // ?? Äàííûå ó÷àñòíèêà ??????????????????????????????????????
    public string GetMemberPlatform(CSteamID id) =>
    SteamMatchmaking.GetLobbyMemberData(LobbyId, id, KEY_PLATFORM);
    public bool GetMemberReady(CSteamID id) =>
    SteamMatchmaking.GetLobbyMemberData(LobbyId, id, KEY_READY) == "1";

    // ?? Ïðîâåðêà: âñå ãîòîâû ???????????????????????????????????
    public bool AllReady()
    {
        if (Members.Count < 2) return false;
        foreach (var m in Members)
            if (!GetMemberReady(m)) return false;
            return true;
    }

    // ?????????????????????????????????????????????????????????
    // Êîëáýêè
    // ?????????????????????????????????????????????????????????

    void OnLobbyCreated(LobbyCreated_t p)
    {
        SteamLog(
            $"LobbyCreated: Result={p.m_eResult}, " +
            $"LobbyID={p.m_ulSteamIDLobby}"
        );

        if (p.m_eResult != EResult.k_EResultOK)
        {
            Debug.LogError(
                $"[SteamLobby] Ñîçäàíèå ëîááè çàâåðøèëîñü îøèáêîé: {p.m_eResult}"
            );
            // Çäåñü îñòàâü ñóùåñòâóþùóþ îáðàáîòêó îøèáêè.
            return;
        }
        bool ok = p.m_eResult == EResult.k_EResultOK;
        if (ok)
        {
            LobbyId = new CSteamID(p.m_ulSteamIDLobby);
            IsHost = true;
            SteamMatchmaking.SetLobbyData(LobbyId, KEY_VERSION, Application.version);
            RefreshMembers();
        }
        OnJoined?.Invoke(ok, ok ? "" : $"Steam error: {p.m_eResult}");
    }
    private static void SteamLog(string message)
    {
        Debug.Log($"[SteamLobby] {message}");
    }
    void OnJoinRequested(GameLobbyJoinRequested_t p)
    {
        if (InLobby) LeaveLobby();   // äîáàâü ýòó ñòðîêó
        JoinLobby(p.m_steamIDLobby);
    }

    void OnLobbyEnter(LobbyEnter_t p)
    {
        CSteamID lobbyId = new CSteamID(p.m_ulSteamIDLobby);

        Debug.Log(
            $"[SteamLobby] OnLobbyEnter: " +
            $"LobbyID={lobbyId.m_SteamID}, " +
            $"Response={p.m_EChatRoomEnterResponse}"
        );

        if (p.m_EChatRoomEnterResponse !=
            (uint)EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess)
        {
            string error =
            $"Steam îòêàçàë âî âõîäå. Response={p.m_EChatRoomEnterResponse}";

            Debug.LogError("[SteamLobby] " + error);
            OnJoined?.Invoke(false, error);
            return;
        }

        Debug.Log($"[SteamLobby] Óñïåøíî âîøëè â ëîááè {lobbyId.m_SteamID}");
        bool ok = (EChatRoomEnterResponse)p.m_EChatRoomEnterResponse
        == EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess;

        if (!ok) { OnJoined?.Invoke(false, "Íå óäàëîñü âîéòè â ëîááè."); return; }

        LobbyId = new CSteamID(p.m_ulSteamIDLobby);
        IsHost = SteamMatchmaking.GetLobbyOwner(LobbyId) == SteamUser.GetSteamID();
        RefreshMembers();
        OnJoined?.Invoke(true, "");
    }

    void OnChatUpdate(LobbyChatUpdate_t p)
    {
        RefreshMembers();

        var changed = new CSteamID(p.m_ulSteamIDUserChanged);
        var state = (EChatMemberStateChange)p.m_rgfChatMemberStateChange;

        if ((state & (EChatMemberStateChange.k_EChatMemberStateChangeDisconnected
            | EChatMemberStateChange.k_EChatMemberStateChangeLeft
            | EChatMemberStateChange.k_EChatMemberStateChangeKicked)) != 0)
        {
            if (changed != SteamUser.GetSteamID())
                OnPartnerLeft?.Invoke();
        }

        OnMembersChanged?.Invoke();
    }

    void OnChatMessage(LobbyChatMsg_t p)
    {
        var buf = new byte[4096];
        var sender = new CSteamID();
        var type = EChatEntryType.k_EChatEntryTypeChatMsg;
        int len = SteamMatchmaking.GetLobbyChatEntry(
            LobbyId, (int)p.m_iChatID, out sender, buf, buf.Length, out type);
        string text = System.Text.Encoding.UTF8.GetString(buf, 0, len);
        string name = SteamFriends.GetFriendPersonaName(sender);
        OnChatMsg?.Invoke(name, text);
    }

    void OnGameCreated(LobbyGameCreated_t p)
    {
        string s = SteamMatchmaking.GetLobbyData(LobbyId, KEY_HOST);
        ulong host = ulong.TryParse(s, out var h) ? h : p.m_ulSteamIDGameServer;
        OnGameStart?.Invoke(host);
    }

    void RefreshMembers()
    {
        Members.Clear();
        int n = SteamMatchmaking.GetNumLobbyMembers(LobbyId);
        for (int i = 0; i < n; i++)
            Members.Add(SteamMatchmaking.GetLobbyMemberByIndex(LobbyId, i));
    }
}
