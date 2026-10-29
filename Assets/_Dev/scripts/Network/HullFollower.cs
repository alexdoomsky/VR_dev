using UnityEngine;

// ПК-ТАНК. Вешается на КОРЕНЬ ПК-танка (на объект, внутри которого и корпус, и башня, и камеры).
// Сам по себе танк не ездит, но пока идёт сеть, повторяет позу корпуса VR-танка.
// Без сети (тест в одиночку) ничего не делает.
public class HullFollower : MonoBehaviour
{
    [SerializeField] Rigidbody hullBody;   // Rigidbody корпуса: на время сети делаем kinematic

    bool prepared;

    void LateUpdate()
    {
        var link = TankLink.Instance;
        if (!link) return;

        if (!prepared)
        {
            if (hullBody) hullBody.isKinematic = true;
            prepared = true;
        }

        transform.SetPositionAndRotation(link.transform.position, link.transform.rotation);
    }
}
