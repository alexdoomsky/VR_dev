using Mirror;
using UnityEngine;

// Вешается на префаб снаряда (рядом с NetworkIdentity и NetworkTransform).
// Сервер симулирует снаряд и урон. Клиент видит копию, которая только едет по NetworkTransform.
[RequireComponent(typeof(Projectile))]
public class ProjectileNet : NetworkBehaviour
{
    Projectile projectile;

    void Awake() => projectile = GetComponent<Projectile>();

    public override void OnStartClient()
    {
        if (isServer) return;

        GetComponent<Rigidbody>().isKinematic = true;
        foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
        projectile.enabled = false;   // у клиента ни попаданий, ни урона
    }

    // Сервер вызывает из Projectile.OnCollisionEnter
    [ClientRpc]
    public void RpcHit(Vector3 point, Quaternion rot)
    {
        if (isServer) return;         // у сервера эффекты уже сыграны
        projectile.PlayHit(point, rot);
    }
}
