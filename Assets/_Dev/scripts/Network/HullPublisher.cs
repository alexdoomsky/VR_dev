using UnityEngine;

// VR-ТАНК. Вешается на корень VR-танка. Отдаёт позу корпуса в TankLink.
// Остальная механика VR-танка (езда, рычаги) не меняется.
[DefaultExecutionOrder(-100)]   // раньше NetworkTransform, чтобы он взял свежую позу
public class HullPublisher : MonoBehaviour
{
    [SerializeField] Transform hull;   // что реально ездит. Пусто = этот объект

    void Awake()
    {
        if (!hull) hull = transform;
    }

    void Update()
    {
        var link = TankLink.Instance;
        if (!link || !link.isOwned) return;
        link.transform.SetPositionAndRotation(hull.position, hull.rotation);
    }
}
