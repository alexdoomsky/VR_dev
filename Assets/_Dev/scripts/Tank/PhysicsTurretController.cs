using UnityEngine;

public class PhysicsTurretController : MonoBehaviour, IAimable, IAimTarget
{
    [Header("Части")]
    [SerializeField] Rigidbody hull;          // Rigidbody корпуса
    [SerializeField] Transform muzzle;        // дочерний у Gun, Z вдоль ствола
    [SerializeField] HingeJoint turretJoint;  // на башне
    [SerializeField] HingeJoint gunJoint;     // на пушке

    [Header("Скорость (град/сек)")]
    [SerializeField] float yawSpeed = 25f;
    [SerializeField] float pitchSpeed = 15f;

    [Header("Привод")]
    [SerializeField] float yawForce = 50000f;
    [SerializeField] float pitchForce = 20000f;
    [SerializeField] float gain = 6f;
    [SerializeField] Vector2 pitchLimits = new(-8f, 18f);
    [SerializeField] bool invertYaw, invertPitch;

    public float Yaw { get; private set; }     // относительно стартового направления
    public float Pitch { get; private set; }   // относительно горизонта корпуса

    Vector3 upLocal;        // "вверх" в локальных осях корпуса
    Vector3 fwdLocal0;      // стартовое "вперёд" (горизонтальное) в осях корпуса
    Vector3 desiredDir;     // куда хотим навестись (мир)
    Vector3 aimPoint;       // точка, в которую целимся
    bool usePoint;          // true: направление считается от дула к aimPoint

    Vector3 Up => hull.transform.TransformDirection(upLocal);

    void Start()
    {
        Transform h = hull.transform;

        // Танк на старте должен стоять ровно
        upLocal = h.InverseTransformDirection(Vector3.up);
        Vector3 up = Up;

        Vector3 flat = Vector3.ProjectOnPlane(muzzle.forward, up).normalized;
        fwdLocal0 = h.InverseTransformDirection(flat);
        desiredDir = muzzle.forward;

        // Оси шарниров выставляем сами, оси моделей не важны
        turretJoint.axis = turretJoint.transform.InverseTransformDirection(up);
        Vector3 right = Vector3.Cross(up, flat);
        gunJoint.axis = gunJoint.transform.InverseTransformDirection(right);
    }

    // Относительное наведение (мышь в режиме "дельты")
    public void AddAimDelta(float yaw, float pitch)
    {
        if (usePoint)
        {
            usePoint = false;
            desiredDir = muzzle.forward;   // продолжаем от текущего направления ствола
        }

        Vector3 up = Up;
        Vector3 d = Quaternion.AngleAxis(yaw, up) * desiredDir;
        Vector3 right = Vector3.Cross(up, d).normalized;
        desiredDir = Quaternion.AngleAxis(-pitch, right) * d;
    }

    // Наведение по направлению
    public void AimAt(Vector3 worldDir)
    {
        usePoint = false;
        desiredDir = worldDir.normalized;
    }

    // Наведение в точку мира
    public void AimAtPoint(Vector3 worldPoint)
    {
        aimPoint = worldPoint;
        usePoint = true;
    }

    void FixedUpdate()
    {
        // Дуло движется вместе с башней, поэтому направление к точке пересчитываем каждый кадр
        if (usePoint)
        {
            Vector3 to = aimPoint - muzzle.position;
            if (to.sqrMagnitude > 0.01f) desiredDir = to.normalized;
        }

        Vector3 up = Up;
        Vector3 fwd = muzzle.forward;

        // Yaw: ошибка в плоскости, перпендикулярной "вверх"
        Vector3 fFlat = Vector3.ProjectOnPlane(fwd, up);
        Vector3 wFlat = Vector3.ProjectOnPlane(desiredDir, up);
        float yawErr = 0f;
        if (fFlat.sqrMagnitude > 1e-6f && wFlat.sqrMagnitude > 1e-6f)
            yawErr = Vector3.SignedAngle(fFlat, wFlat, up);

        Vector3 startFlat = hull.transform.TransformDirection(fwdLocal0);
        if (fFlat.sqrMagnitude > 1e-6f)
            Yaw = Vector3.SignedAngle(startFlat, fFlat, up);

        // Pitch: высота над горизонтом корпуса
        Pitch = Mathf.Asin(Mathf.Clamp(Vector3.Dot(fwd.normalized, up), -1f, 1f)) * Mathf.Rad2Deg;
        float wantPitch = Mathf.Asin(Mathf.Clamp(Vector3.Dot(desiredDir, up), -1f, 1f)) * Mathf.Rad2Deg;
        wantPitch = Mathf.Clamp(wantPitch, pitchLimits.x, pitchLimits.y);

        Drive(turretJoint, yawErr, yawSpeed, yawForce, invertYaw);
        Drive(gunJoint, wantPitch - Pitch, pitchSpeed, pitchForce, invertPitch);

        Debug.DrawRay(muzzle.position, muzzle.forward * 50f, Color.green);
    }

    void Drive(HingeJoint j, float error, float maxSpeed, float force, bool invert)
    {
        float v = Mathf.Clamp(error * gain, -maxSpeed, maxSpeed);
        if (invert) v = -v;

        var m = j.motor;
        m.targetVelocity = v;
        m.force = force;
        m.freeSpin = false;
        j.motor = m;
        j.useMotor = true;
    }
}