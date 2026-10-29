using UnityEngine;

public class LookController : MonoBehaviour
{
    [Header("Ѕазовые оси (перенеси то, что подобрал раньше)")]
    [SerializeField] Transform hull;
    [SerializeField] Transform referenceRoot;   // CommanderRoot: его стартова€ ориентаци€ = "пр€мо"
    [SerializeField] Vector3 yawAxis = Vector3.up;
    [SerializeField] Vector3 pitchAxis = Vector3.right;

    [Header("”правление")]
    [SerializeField] float sensitivity = 2f;
    [SerializeField] Vector2 pitchLimits = new(-15f, 70f);
    [SerializeField] bool aimTurret = true;

    [Header("Ќаведение в точку")]
    [SerializeField] bool aimAtPoint = true;     // true: целимс€ в то, на что смотрит камера
    [SerializeField] LayerMask aimMask = ~0;     // что считаетс€ целью (убери слои Tank и Projectile)
    [SerializeField] float maxAimDistance = 3000f;
    [SerializeField] float minAimDistance = 8f;  // ближе не наводимс€, иначе башн€ дЄргаетс€ на край люка

    public Vector3 AimPoint { get; private set; }

    static readonly RaycastHit[] buf = new RaycastHit[64];

    CameraSwitcher switcher;
    IAimTarget aimTarget;
    ILookView active;
    Quaternion baseLocal;
    float yaw, pitch;

    void Awake()
    {
        aimTarget = GetComponentInParent<IAimTarget>();
        baseLocal = Quaternion.Inverse(hull.rotation) * referenceRoot.rotation;
    }

    void OnEnable()
    {
        switcher = GetComponent<CameraSwitcher>();
        if (switcher) switcher.Changed += OnViewChanged;
    }

    void OnDisable()
    {
        if (switcher) switcher.Changed -= OnViewChanged;
    }

    void OnViewChanged(ICameraView view)
    {
        // —татичные виды (StaticView) не ILookView, тогда active = null и мышь не управл€ет
        active = view as ILookView;
    }

    void Update()
    {
        if (active == null) return;

        float k = sensitivity * active.SensitivityScale;
        yaw += Input.GetAxis("Mouse X") * k;
        pitch = Mathf.Clamp(pitch + Input.GetAxis("Mouse Y") * k, pitchLimits.x, pitchLimits.y);

        float scroll = Input.mouseScrollDelta.y;
        if (scroll != 0f) active.Zoom(scroll);
    }

    void LateUpdate()
    {
        if (active == null) return;

        Quaternion rot = hull.rotation
            * Quaternion.AngleAxis(yaw, yawAxis)
            * Quaternion.AngleAxis(-pitch, pitchAxis)
            * baseLocal;

        active.ApplyLook(rot);

        if (!aimTurret || aimTarget == null) return;

        Transform camT = active.ViewCamera.transform;

        if (aimAtPoint)
        {
            AimPoint = FindAimPoint(camT);
            aimTarget.AimAtPoint(AimPoint);
            Debug.DrawLine(camT.position, AimPoint, Color.red);
        }
        else
        {
            aimTarget.AimAt(camT.forward);
        }
    }

    //  уда попадает луч из камеры. —вой танк и лежащие снар€ды луч игнорирует.
    Vector3 FindAimPoint(Transform camT)
    {
        var ray = new Ray(camT.position, camT.forward);
        int n = Physics.RaycastNonAlloc(ray, buf, maxAimDistance, aimMask, QueryTriggerInteraction.Ignore);

        float best = maxAimDistance;   // ничего не попало: целимс€ далеко по лучу
        for (int i = 0; i < n; i++)
        {
            var h = buf[i];
            if (h.distance >= best) continue;
            if (h.collider.transform.IsChildOf(transform)) continue;        // сам танк
            if (h.collider.GetComponentInParent<Projectile>() != null) continue;  // снар€ды
            best = h.distance;
        }

        best = Mathf.Max(best, minAimDistance);
        return ray.origin + ray.direction * best;
    }
}