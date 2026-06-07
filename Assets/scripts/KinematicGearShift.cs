using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// =============================================================================
//  KinematicGearShift — ROTATION-BASED  (v3, Unity 6 / XRI 3.x)
//
//  Почему ротация, не позиция:
//    1. Физически правильно — рычаг вращается вокруг своей оси
//    2. Нет проблемы с родителем:
//       transform.rotation — это МИРОВОЕ пространство, не зависит от того,
//       кто сейчас родитель. Даже если XRI переприкрепит объект — всё верно.
//
//  Иерархия объектов (важно!):
//    LeverPivot     ← статичный GameObject, не двигается
//      └─ LeverMesh ← здесь скрипт + Rigidbody + Collider + XRGrabInteractable
//                     Ось вращения рычага должна быть в начале координат (0,0,0)
//
//  Если рычаг наклоняется в НЕПРАВИЛЬНУЮ сторону — поменяй знак в Inspector:
//    forwardAxisSign  = -1   (вперёд/назад)
//    lateralAxisSign  = -1   (влево/вправо)
// =============================================================================

[RequireComponent(typeof(Rigidbody), typeof(XRGrabInteractable))]
public class KinematicGearShift : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────
    //  ДАННЫЕ ПЕРЕДАЧ
    // ─────────────────────────────────────────────────────────────

    [System.Serializable]
    public struct GearSlot
    {
        public string label;
        public int    gear;
        [Tooltip("X = вперёд/назад (градусы), Y = влево/вправо (градусы).\n" +
        "Нейтраль = (0, 0). Отрегулируй под свою геометрию.")]
        public Vector2 angles;
        public bool   requiresReverse;
    }

    [Header("Позиции передач (градусы от нейтрали)")]
    [SerializeField] private GearSlot[] slots =
    {
        new() { label = "1", gear =  1, angles = new(-12f, -10f) },
        new() { label = "2", gear =  2, angles = new( 12f, -10f) },
        new() { label = "3", gear =  3, angles = new(-12f,   0f) },
        new() { label = "4", gear =  4, angles = new( 12f,   0f) },
        new() { label = "5", gear =  5, angles = new(-12f,  10f) },
        new() { label = "R", gear = -1, angles = new( 12f,  10f), requiresReverse = true },
    };

    [Header("Направление осей (1 или -1)")]
    [Tooltip("Если рычаг наклоняется назад когда надо вперёд — поставь -1")]
    [SerializeField] private float forwardAxisSign =  1f;
    [Tooltip("Если рычаг наклоняется вправо когда надо влево — поставь -1")]
    [SerializeField] private float lateralAxisSign =  1f;

    [Header("Движение")]
    [SerializeField] private float followSpeed     = 15f;

    [Header("H-гейт")]
    [Tooltip("Полуширина нейтрального коридора, градусы (горизонтальная перемычка H)")]
    [SerializeField] private float neutralHalfAngle = 5f;

    [Header("Магнетизм")]
    [Tooltip("Угловой радиус притяжения к передаче, градусы")]
    [SerializeField] private float snapAngle      = 5f;
    [SerializeField] private float snapSpeed      = 12f;
    [Tooltip("Угловой порог окончательного защёлкивания, градусы")]
    [SerializeField] private float snapLockAngle  = 1f;

    [Header("Сопротивление выбивки")]
    [Tooltip("Угол вытягивания до освобождения передачи, градусы")]
    [SerializeField] private float disengageAngle = 8f;
    [Range(0f, 0.98f)]
    [Tooltip("0 = нет сопротивления, 0.95 = очень тугая")]
    [SerializeField] private float resistBlend    = 0.88f;
    [SerializeField] private float resistCurve    = 1.3f;

    [Header("Хаптика")]
    [SerializeField] private float hapticIn        = 0.70f;
    [SerializeField] private float hapticOut       = 0.90f;
    [SerializeField] private float hapticResistMax = 0.55f;

    [Header("Задняя передача")]
    [SerializeField] private InputActionReference reverseButton;

    // ─────────────────────────────────────────────────────────────
    //  КОМПОНЕНТЫ И КЭШИ
    // ─────────────────────────────────────────────────────────────

    private Rigidbody          _rb;
    private XRGrabInteractable _grab;
    private Transform          _hand;

    // Ключ решения проблемы с родителем:
    //   кэшируем всё при старте — потом используем только кэш,
    //   а transform.rotation (мировое) не зависит от текущего родителя
    private Transform  _parent;           // оригинальный родитель
    private Vector3    _neutralLocalPos;  // позиция пивота в parent-пространстве
    private Quaternion _neutralLocalRot;  // базовая ротация рычага (обычно identity)

    // ─────────────────────────────────────────────────────────────
    //  СОСТОЯНИЕ
    // ─────────────────────────────────────────────────────────────

    private bool  _grabbed;
    private bool  _inGear;
    private int   _slotIndex = -1;
    private float _hapticTimer;

    // ─────────────────────────────────────────────────────────────
    //  PUBLIC API
    // ─────────────────────────────────────────────────────────────

    public int  CurrentGear => _inGear && _slotIndex >= 0 ? slots[_slotIndex].gear : 0;
    public bool IsGrabbed   => _grabbed;

    /// <summary>Срабатывает при смене передачи. 0 = нейтраль, -1 = задняя.</summary>
    public event System.Action<int> OnGearChanged;

    // ═════════════════════════════════════════════════════════════
    //  ИНИЦИАЛИЗАЦИЯ
    // ═════════════════════════════════════════════════════════════

    void Awake()
    {
        _rb   = GetComponent<Rigidbody>();
        _grab = GetComponent<XRGrabInteractable>();

        _rb.isKinematic    = true;
        _grab.trackPosition = false;   // XRI не трогает позицию
        _grab.trackRotation = false;   // XRI не трогает ротацию — мы сами
        _grab.movementType  = XRGrabInteractable.MovementType.Kinematic;

        // Кэшируем всё до того как XRI успеет что-то поменять
        _parent          = transform.parent;
        _neutralLocalPos = transform.localPosition;
        _neutralLocalRot = transform.localRotation;

        _grab.selectEntered.AddListener(OnGrab);
        _grab.selectExited.AddListener(OnRelease);
    }

    void OnDestroy()
    {
        _grab.selectEntered.RemoveListener(OnGrab);
        _grab.selectExited.RemoveListener(OnRelease);
    }

    // ─────────────────────────────────────────────────────────────
    //  СОБЫТИЯ ЗАХВАТА
    // ─────────────────────────────────────────────────────────────

    void OnGrab(SelectEnterEventArgs e)
    {
        _grabbed = true;
        _hand    = e.interactorObject.GetAttachTransform(_grab);

        // XRI может сразу переприкрепить объект к интерактору —
        // возвращаем к оригинальному родителю здесь же
        RestoreParent();
    }

    void OnRelease(SelectExitEventArgs e)
    {
        _grabbed = false;
        _hand    = null;
        RestoreParent(); // на всякий случай
    }

    void RestoreParent()
    {
        if (_parent != null && transform.parent != _parent)
            transform.SetParent(_parent, worldPositionStays: true);
    }

    // ═════════════════════════════════════════════════════════════
    //  ГЛАВНЫЙ ЦИКЛ
    // ═════════════════════════════════════════════════════════════

    void Update()
    {
        // Страховочная проверка: XRI иногда переприкрепляет объект
        // не сразу, а в следующем кадре — ловим здесь
        if (_grabbed) RestoreParent();

        Vector2 targetAngles;

        if (!_grabbed)
        {
            // Возвращаемся к позиции включённой передачи (или нейтрали)
            targetAngles = _inGear && _slotIndex >= 0
            ? slots[_slotIndex].angles
            : Vector2.zero;
            ApplyRotation(targetAngles, lerpSpeed: 8f);
            return;
        }

        Vector2 handAngles = GetHandAngles();

        targetAngles = _inGear && _slotIndex >= 0
        ? UpdateInGear(handAngles)
        : UpdateFree(handAngles);

        ApplyRotation(targetAngles, followSpeed);
    }

    // ─────────────────────────────────────────────────────────────
    //  STATE: IN GEAR — сопротивление выбивки
    // ─────────────────────────────────────────────────────────────
    //
    //  target = Lerp(рука, передача, blend)
    //  blend начинается высоким (0.88) и падает к 0 пропорционально
    //  тому, насколько далеко рука от передачи (в градусах)

    Vector2 UpdateInGear(Vector2 handAngles)
    {
        Vector2 gearAngles = slots[_slotIndex].angles;
        float   dist       = Vector2.Distance(handAngles, gearAngles);
        float   progress   = Mathf.Clamp01(dist / disengageAngle);

        // Нарастающая хаптика: едва заметная → нервная дрожь → хлопок
        if (progress > 0.15f)
            TickHaptic(
                amp:    Mathf.Lerp(0.04f, hapticResistMax, progress * progress),
                       period: Mathf.Lerp(0.11f, 0.021f, progress)
            );

        if (dist >= disengageAngle)
        {
            // ✅ ВЫБИВКА
            _inGear    = false;
            _slotIndex = -1;
            SendHapticOnce(hapticOut, 0.06f);
            StartCoroutine(DelayedHaptic(0.08f, hapticOut * 0.28f, 0.04f));
            OnGearChanged?.Invoke(0);
            return handAngles;
        }

        float blend = resistBlend * Mathf.Pow(1f - progress, resistCurve);
        return Vector2.Lerp(handAngles, gearAngles, blend);
    }

    // ─────────────────────────────────────────────────────────────
    //  STATE: FREE — H-гейт + магнетизм
    // ─────────────────────────────────────────────────────────────

    Vector2 UpdateFree(Vector2 handAngles)
    {
        Vector2 target = ApplyHGate(handAngles);

        int nearest = FindNearest(handAngles, out float dist);
        bool canSnap = nearest >= 0
        && dist < snapAngle
        && (!slots[nearest].requiresReverse || IsReverseHeld());

        if (!canSnap) return target;

        // Магнетизм: притяжение нарастает ближе к центру слота
        float t = 1f - dist / snapAngle;
        target = Vector2.Lerp(target, slots[nearest].angles, snapSpeed * t * Time.deltaTime);

        if (dist < snapLockAngle)
        {
            // ✅ ЗАЩЁЛКИВАНИЕ
            _inGear    = true;
            _slotIndex = nearest;
            SendHapticOnce(hapticIn, 0.06f);
            OnGearChanged?.Invoke(CurrentGear);
            return slots[nearest].angles;
        }

        return target;
    }

    // ─────────────────────────────────────────────────────────────
    //  H-ГЕЙТ (угловой)
    //
    //  |tiltX| < neutralHalfAngle → нейтральный коридор:
    //      X (вперёд/назад) прижимается к 0, Y (влево/вправо) свободен
    //  иначе → боковой коридор:
    //      Y прижимается к ближайшей колонке, X свободен
    // ─────────────────────────────────────────────────────────────

    Vector2 ApplyHGate(Vector2 handAngles)
    {
        if (Mathf.Abs(handAngles.x) < neutralHalfAngle)
        {
            // Нейтральный коридор: выбираем колонку (Y), блокируем X
            return new Vector2(handAngles.x * 0.08f, handAngles.y);
        }
        else
        {
            // В боковом коридоре: прижимаем Y к ближайшей колонке
            float col = FindNearestColumnAngle(handAngles.y);
            return new Vector2(handAngles.x, Mathf.Lerp(handAngles.y, col, 0.88f));
        }
    }

    float FindNearestColumnAngle(float lateralAngle)
    {
        float best = 0f, bestDist = float.MaxValue;
        foreach (var s in slots)
        {
            float d = Mathf.Abs(s.angles.y - lateralAngle);
            if (d < bestDist) { bestDist = d; best = s.angles.y; }
        }
        return best;
    }

    // ─────────────────────────────────────────────────────────────
    //  ЧТЕНИЕ ПОЗИЦИИ РУКИ → УГЛЫ РЫЧАГА
    //
    //  Вычисляем направление "пивот → рука" в parent-пространстве
    //  и конвертируем его в углы наклона рычага.
    //
    //  Если оси перепутаны — меняй forwardAxisSign / lateralAxisSign
    //  в Inspector на -1.
    // ─────────────────────────────────────────────────────────────

    Vector2 GetHandAngles()
    {
        if (_hand == null) return Vector2.zero;

        // Позиция руки в parent-пространстве (используем кэшированный _parent!)
        Vector3 handInParent = _parent != null
        ? _parent.InverseTransformPoint(_hand.position)
        : _hand.position;

        // Направление от пивота (нейтральная позиция рычага) к руке
        Vector3 dir = handInParent - _neutralLocalPos;
        if (dir.sqrMagnitude < 0.0001f) return Vector2.zero;

        // X-tilt (вперёд/назад): рука смещена по Z → рычаг наклоняется по X
        float tiltX = Mathf.Atan2(dir.z * forwardAxisSign, Mathf.Abs(dir.y)) * Mathf.Rad2Deg;
        // Z-tilt (влево/вправо): рука смещена по X → рычаг наклоняется по Z
        float tiltZ = Mathf.Atan2(dir.x * lateralAxisSign, Mathf.Abs(dir.y)) * Mathf.Rad2Deg;

        return new Vector2(tiltX, tiltZ);
    }

    // ─────────────────────────────────────────────────────────────
    //  ПРИМЕНЕНИЕ РОТАЦИИ (не зависит от текущего родителя)
    //
    //  Работаем через МИРОВУЮ ротацию (transform.rotation), а не
    //  локальную. Даже если XRI поменял родителя — мировая ротация
    //  остаётся корректной.
    // ─────────────────────────────────────────────────────────────

    void ApplyRotation(Vector2 angles, float lerpSpeed)
    {
        // Целевая локальная ротация = базовая ротация рычага + наклон от передачи
        Quaternion localTarget = _neutralLocalRot * Quaternion.Euler(angles.x, 0f, angles.y);

        // Конвертируем в мировую ротацию через кэшированного родителя
        Quaternion worldTarget = _parent != null
        ? _parent.rotation * localTarget
        : localTarget;

        // Применяем в мировом пространстве — независимо от текущего родителя
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            worldTarget,
            Time.deltaTime * lerpSpeed
        );
    }

    // ─────────────────────────────────────────────────────────────
    //  ВСПОМОГАТЕЛЬНЫЕ
    // ─────────────────────────────────────────────────────────────

    int FindNearest(Vector2 angles, out float minDist)
    {
        int best = -1; minDist = float.MaxValue;
        for (int i = 0; i < slots.Length; i++)
        {
            float d = Vector2.Distance(angles, slots[i].angles);
            if (d < minDist) { minDist = d; best = i; }
        }
        return best;
    }

    bool IsReverseHeld() =>
    reverseButton != null && reverseButton.action.IsPressed();

    // ─────────────────────────────────────────────────────────────
    //  ХАПТИКА
    // ─────────────────────────────────────────────────────────────

    void TickHaptic(float amp, float period)
    {
        _hapticTimer -= Time.deltaTime;
        if (_hapticTimer > 0f) return;
        _hapticTimer = period;
        SendHapticOnce(amp, period * 0.7f);
    }

    void SendHapticOnce(float amp, float dur)
    {
        // Явный UnityEngine.XR. — без "using UnityEngine.XR" конфликт с InputSystem исчезает
        var buf = new List<UnityEngine.XR.InputDevice>();
        foreach (var role in new[] {
            UnityEngine.XR.InputDeviceRole.LeftHanded,
            UnityEngine.XR.InputDeviceRole.RightHanded })
        {
            UnityEngine.XR.InputDevices.GetDevicesWithRole(role, buf);
            foreach (var d in buf)
                if (d.isValid
                    && d.TryGetHapticCapabilities(out UnityEngine.XR.HapticCapabilities c)
                    && c.supportsImpulse)
                    d.SendHapticImpulse(0, Mathf.Clamp01(amp), dur);
                buf.Clear();
        }
    }

    IEnumerator DelayedHaptic(float delay, float amp, float dur)
    {
        yield return new WaitForSeconds(delay);
        SendHapticOnce(amp, dur);
    }

    // ─────────────────────────────────────────────────────────────
    //  GIZMOS — визуализация в редакторе
    // ─────────────────────────────────────────────────────────────

    #if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Transform par = Application.isPlaying ? _parent : transform.parent;
        if (par == null) return;

        Vector3 origin = transform.position;
        Quaternion baseRot = Application.isPlaying
        ? par.rotation * _neutralLocalRot
        : par.rotation * transform.localRotation;

        foreach (var s in slots)
        {
            Quaternion slotRot = baseRot * Quaternion.Euler(s.angles.x, 0f, s.angles.y);
            Vector3 tip = origin + slotRot * Vector3.up * 0.09f;

            Gizmos.color = s.gear < 0 ? Color.red : Color.cyan;
            Gizmos.DrawLine(origin, tip);
            Gizmos.DrawWireSphere(tip, 0.004f);
            UnityEditor.Handles.Label(tip + Vector3.up * 0.01f, s.label);
        }

        // Нейтральный конус
        Gizmos.color = new Color(1f, 1f, 0f, 0.25f);
        for (float a = 0; a < 360; a += 30)
        {
            float r = Mathf.Tan(neutralHalfAngle * Mathf.Deg2Rad) * 0.09f;
            Vector3 p1 = origin + baseRot * new Vector3(
                r * Mathf.Cos(a * Mathf.Deg2Rad), 0.09f, r * Mathf.Sin(a * Mathf.Deg2Rad));
            Vector3 p2 = origin + baseRot * new Vector3(
                r * Mathf.Cos((a+30) * Mathf.Deg2Rad), 0.09f, r * Mathf.Sin((a+30) * Mathf.Deg2Rad));
            Gizmos.DrawLine(p1, p2);
        }
    }
    #endif
}
