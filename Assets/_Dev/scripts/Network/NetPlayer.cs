using System.Collections.Generic;
using Mirror;
using Steamworks;

// PLAYER PREFAB. Пустой объект (NetworkIdentity + NetPlayer), без модели.
// Один на каждого человека. Нужен, чтобы сервер знал, кто есть кто (VR / ПК).
// Сам танк - отдельный объект, его спавнит хост (см. SessionService).
public class NetPlayer : NetworkBehaviour
{
    public static readonly List<NetPlayer> All = new();

    [SyncVar] public ControlMode Mode;
    [SyncVar] public ulong SteamId;

    public override void OnStartClient() => All.Add(this);
    public override void OnStopClient() => All.Remove(this);

    public override void OnStartLocalPlayer()
    {
        CmdInit(GameSettings.Mode, SteamUser.GetSteamID().m_SteamID);
    }

    [Command]
    void CmdInit(ControlMode mode, ulong steamId)
    {
        Mode = mode;
        SteamId = steamId;
    }
}
