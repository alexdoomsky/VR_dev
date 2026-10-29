using UnityEngine;

// ПК-ТАНК (хост). Вешается на корень ПК-танка.
// Отдаёт VR-игроку углы башни/пушки и сообщает о выстрелах.
public class TurretPublisher : MonoBehaviour
{
    [SerializeField] PhysicsTurretController turret;
    [SerializeField] GunController gun;

    TankLink subscribedTo;

    void Update()
    {
        var link = TankLink.Instance;
        if (!link || !link.isServer) return;

        if (subscribedTo != link)
        {
            Unsubscribe();
            gun.Fired += link.NotifyFired;
            subscribedTo = link;
        }

        link.SetAim(turret.Yaw, turret.Pitch);
    }

    void Unsubscribe()
    {
        if (!ReferenceEquals(subscribedTo, null)) gun.Fired -= subscribedTo.NotifyFired;
        subscribedTo = null;
    }

    void OnDestroy() => Unsubscribe();
}
