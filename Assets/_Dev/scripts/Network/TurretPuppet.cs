using UnityEngine;

// VR-ТАНК. Вешается на корень VR-танка.
// Поворачивает башню и пушку VR-танка по углам, которые прислал ПК-игрок. Сама башня ничем не управляется.
//
// Условия: на старте VR-танк стоит ровно, а ствол смотрит прямо вперёд и горизонтально
// (то же условие, что у PhysicsTurretController на ПК-танке). У Turret и Gun VR-танка
// НЕТ Rigidbody/HingeJoint (или они kinematic), иначе будут драться с этим скриптом.
public class TurretPuppet : MonoBehaviour
{
    [SerializeField] Transform hull;       // корпус (откуда берём «вверх»)
    [SerializeField] Transform turret;
    [SerializeField] Transform gun;
    [SerializeField] Transform muzzle;     // конец ствола
    [SerializeField] GunController fx;     // необязательно: вспышка и откат при выстреле
    [SerializeField] bool invertYaw, invertPitch;   // если вращается не в ту сторону
    [SerializeField] float smoothTime = 0.06f;

    Vector3 yawAxis, pitchAxis;
    Quaternion turret0, gun0;
    float pitch0, curYaw, curPitch, yawVel, pitchVel;
    TankLink subscribedTo;

    void Start()
    {
        Vector3 up = hull.up;
        Vector3 flat = Vector3.ProjectOnPlane(muzzle.forward, up).normalized;
        Vector3 right = Vector3.Cross(up, flat);

        yawAxis = turret.parent.InverseTransformDirection(up);
        pitchAxis = gun.parent.InverseTransformDirection(right);
        turret0 = turret.localRotation;
        gun0 = gun.localRotation;
        pitch0 = Mathf.Asin(Mathf.Clamp(Vector3.Dot(muzzle.forward.normalized, up), -1f, 1f)) * Mathf.Rad2Deg;
    }

    void LateUpdate()
    {
        var link = TankLink.Instance;
        if (!link) return;

        if (subscribedTo != link)
        {
            Unsubscribe();
            if (fx) link.Fired += fx.PlayFireVisuals;
            subscribedTo = link;
        }

        curYaw = Mathf.SmoothDampAngle(curYaw, link.Yaw, ref yawVel, smoothTime);
        curPitch = Mathf.SmoothDamp(curPitch, link.Pitch, ref pitchVel, smoothTime);

        float y = invertYaw ? -curYaw : curYaw;
        float p = invertPitch ? -curPitch : curPitch;

        turret.localRotation = Quaternion.AngleAxis(y, yawAxis) * turret0;
        gun.localRotation = Quaternion.AngleAxis(-(p - pitch0), pitchAxis) * gun0;
    }

    void Unsubscribe()
    {
        if (fx && !ReferenceEquals(subscribedTo, null)) subscribedTo.Fired -= fx.PlayFireVisuals;
        subscribedTo = null;
    }

    void OnDestroy() => Unsubscribe();
}
