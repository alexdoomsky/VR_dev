using System;
using Mirror;

// Сетевой объект-«мост» между двумя РАЗНЫМИ танками. Без модели.
// Префаб: NetworkIdentity + NetworkTransformReliable (Sync Direction = Client To Server) + TankLink.
// Спавнит хост (ПК), владелец = VR-клиент.
//
//  Поза корпуса:  VR -> ПК   (VR-танк пишет в transform этого объекта, NetworkTransform несёт его на хост)
//  Углы башни:    ПК -> VR   (SyncVar Yaw/Pitch)
//  Выстрел:       ПК -> VR   (событие Fired, чтобы у VR сыграла вспышка и откат)
public class TankLink : NetworkBehaviour
{
    public static TankLink Instance { get; private set; }

    [SyncVar] public float Yaw;     // градусы относительно стартового направления ствола
    [SyncVar] public float Pitch;   // градусы над горизонтом корпуса

    public event Action Fired;

    void Awake()
    {
        Instance = this;
        syncInterval = 0.04f;       // ~25 Гц, VR-башня дополнительно сглаживается
    }

    public override void OnStartClient() { Instance = this; }

    public override void OnStopClient()
    {
        if (Instance == this) Instance = null;
    }

    // Вызывает только хост (ПК)
    public void SetAim(float yaw, float pitch)
    {
        if (!isServer) return;
        Yaw = yaw;
        Pitch = pitch;
    }

    public void NotifyFired(int shell)
    {
        if (isServer) RpcFired();
    }

    [ClientRpc]
    void RpcFired()
    {
        Fired?.Invoke();
    }
}
