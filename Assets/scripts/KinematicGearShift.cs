using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;                           // InputActionReference
using UnityEngine.XR.Interaction.Toolkit;               // SelectEnterEventArgs и т.п.
using UnityEngine.XR.Interaction.Toolkit.Interactables; // XRGrabInteractable (XRI 3.x)
// ↑ ВАЖНО: НЕ добавляй "using UnityEngine.XR;" — создаёт конфликт InputDevice
//          Вместо этого используем UnityEngine.XR.InputDevice, InputDevices и т.д. явно

// =============================================================================
//  KinematicGearShift  —  Вариант A: рычаг КПП без физики
//  Unity 6 / XR Interaction Toolkit 3.x
//
//  Добавь на GameObject с:
//    • Rigidbody    (isKinematic = true выставится автоматически)
//    • Collider     (любой, для детектирования захвата)
//    • XRGrabInteractable
//
//  Ощущение усилия выбивки:
//    leverPos = Lerp(рука, передача, blend)
//    blend плавно падает 0.88 → 0 пока ты тянешь
//    + хаптика нарастает по квадратичной кривой
//    = рычаг почти не двигается → дрожит сильнее → "хлопает" при выходе
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
        public string  label;
        public int     gear;              // 1..N = вперёд, -1 = задняя
        public Vector2 pos;              // локальные XZ-метры от нейтрали
        public bool    requiresReverse;  // нужна кнопка для разблокировки
    }

    [Header("Позиции передач (XZ от нейтрали, метры)")]
    [SerializeField] private GearSlot[] slots =
    {
        new() { label = "1", gear =  1, pos = new(-0.05f,  0.04f) },
        new() { label = "2", gear =  2, pos = new(-0.05f, -0.04f) },
        new() { label = "3", gear =  3, pos = new( 0.00f,  0.04f) },
        new() { label = "4", gear =  4, pos = new( 0.00f, -0.04f) },
        new() { label = "5", gear =  5, pos = new( 0.05f,  0.04f) },
        new() { label = "R", gear = -1, pos = new( 0.05f, -0.04f), requiresReverse = true },
    };

    // ─────────────────────────────────────────────────────────────
    //  ПАРАМЕТРЫ (все доступны в Inspector, просто тюни)
    // ─────────────────────────────────────────────────────────────

    [Header("Движение рычага")]
    [Tooltip("Насколько быстро рычаг догоняет целевую позицию (выше = отзывчивее)")]
    [SerializeField] private float followSpeed   = 18f;

    [Header("H-гейт")]
    [Tooltip("Полуширина нейтрального коридора (горизонтальная перемычка H)")]
    [SerializeField] private float neutralHalfW  = 0.012f;

    [Header("Магнетизм к передачам")]
    [Tooltip("Радиус начала притяжения к позиции передачи")]
    [SerializeField] private float snapRadius    = 0.022f;
    [Tooltip("Скорость притяжения (выше = магнит сильнее)")]
    [SerializeField] private float snapSpeed     = 15f;
    [Tooltip("Расстояние до окончательного защёлкивания")]
    [SerializeField] private float snapLockDist  = 0.004f;

    [Header("Сопротивление выбивки")]
    [Tooltip("Расстояние вытягивания до освобождения передачи, метры")]
    [SerializeField] private float disengageDist  = 0.028f;
    [Tooltip("Максимальное сопротивление (0 = нет, 0.95 = очень тугая)")]
    [Range(0f, 0.98f)]
    [SerializeField] private float resistBlend    = 0.88f;
    [Tooltip("Форма кривой сопротивления: >1 = быстро отпускает к концу")]
    [SerializeField] private float resistCurve    = 1.3f;

    [Header("Хаптика")]
    [SerializeField] private float hapticIn        = 0.70f;  // щелчок при включении
    [SerializeField] private float hapticOut       = 0.90f;  // щелчок при выбивке
    [SerializeField] private float hapticResistMax = 0.55f;  // пик вибрации сопротивления

    [Header("Задняя передача")]
    [Tooltip("InputAction кнопки разблокировки задней (напр. primaryButton)")]
    [SerializeField] private InputActionReference reverseButton;

    // ─────────────────────────────────────────────────────────────
    //  СОСТОЯНИЕ
    // ─────────────────────────────────────────────────────────────

    private Rigidbody          _rb;
    private XRGrabInteractable _grab;
    private Transform          _hand;   // attach point захватившего интерактора
    // Нет поля _hapticDevice — UnityEngine.XR.InputDevice кэшируем при отправке,
    // чтобы не конфликтовать с UnityEngine.InputSystem.InputDevice

    private bool    _grabbed;
    private bool    _inGear;
    private int     _slotIndex = -1;
    private Vector3 _neutralLocalPos;

    private float _hapticTimer;  // таймер между импульсами сопротивления

    // ─────────────────────────────────────────────────────────────
    //  ПУБЛИЧНЫЙ API
    // ─────────────────────────────────────────────────────────────

    public int  CurrentGear  => _inGear && _slotIndex >= 0 ? slots[_slotIndex].gear : 0;
    public bool IsGrabbed    => _grabbed;

    /// <summary>Срабатывает при каждой смене передачи. Аргумент: номер (0 = нейтраль, -1 = задняя).</summary>
    public event System.Action<int> OnGearChanged;

    // ═════════════════════════════════════════════════════════════
    //  ИНИЦИАЛИЗАЦИЯ
    // ═════════════════════════════════════════════════════════════

    void Awake()
    {
        _rb   = GetComponent<Rigidbody>();
        _grab = GetComponent<XRGrabInteractable>();

        // Движение только через Transform — физика не нужна
        _rb.isKinematic     = true;
        _grab.trackPosition = false;
        _grab.trackRotation = false;
        _grab.movementType  = XRGrabInteractable.MovementType.Kinematic;

        _neutralLocalPos = transform.localPosition;

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
        // XRBaseController удалён в XRI 3.x — девайс ищем в SendHapticOnce по роли
    }

    void OnRelease(SelectExitEventArgs e)
    {
        _grabbed = false;
        _hand    = null;
        // При отпускании рычаг сам вернётся на передачу (или нейтраль) через Update
    }

    // ═════════════════════════════════════════════════════════════
    //  ГЛАВНЫЙ ЦИКЛ
    // ═════════════════════════════════════════════════════════════

    void Update()
    {
        Vector3 targetOffset; // целевое смещение от _neutralLocalPos

        if (!_grabbed)
        {
            // Не захвачен — плавно возвращаем на текущую позицию (передача / нейтраль)
            targetOffset = _inGear && _slotIndex >= 0
                ? SlotOffset(slots[_slotIndex])
                : Vector3.zero;
            MoveToOffset(targetOffset, lerpSpeed: 8f);
            return;
        }

        Vector3 handOff = GetHandOffset();      // позиция руки как смещение от нейтрали
        Vector2 hand2D  = new(handOff.x, handOff.z);

        if (_inGear && _slotIndex >= 0)
            targetOffset = UpdateInGearState(handOff, hand2D);
        else
            targetOffset = UpdateFreeState(handOff, hand2D);

        MoveToOffset(targetOffset, followSpeed);
    }

    // ─────────────────────────────────────────────────────────────
    //  STATE: IN GEAR  (главная фишка — сопротивление)
    // ─────────────────────────────────────────────────────────────
    //
    //  Формула: target = Lerp(рука, передача, blend)
    //           blend = resistBlend * (1 - progress)^resistCurve
    //
    //  Когда рука у передачи:   blend ≈ 0.88 → рычаг почти не двигается
    //  Когда рука у порога:     blend ≈ 0    → рычаг полностью следует за рукой
    //  Когда рука за порогом:   передача выбита, хлопок

    Vector3 UpdateInGearState(Vector3 handOff, Vector2 hand2D)
    {
        Vector2 gearPos = slots[_slotIndex].pos;
        float   dist    = Vector2.Distance(hand2D, gearPos);
        float   progress = Mathf.Clamp01(dist / disengageDist);  // 0 у передачи, 1 у порога

        // Нарастающая вибрация: едва заметна в начале, нервная дрожь перед порогом
        if (progress > 0.15f)
        {
            TickHaptic(
                amp:    Mathf.Lerp(0.04f, hapticResistMax, progress * progress),
                period: Mathf.Lerp(0.11f, 0.021f, progress)
            );
        }

        if (dist >= disengageDist)
        {
            // ✅ ВЫБИВКА — передача освобождена
            _inGear    = false;
            _slotIndex = -1;
            SendHapticOnce(hapticOut, 0.06f);
            StartCoroutine(DelayedHaptic(delay: 0.08f, amp: hapticOut * 0.28f, dur: 0.04f));
            OnGearChanged?.Invoke(0);
            return handOff; // рычаг сразу следует за рукой
        }

        // Сопротивление: рычаг "прилип" к передаче, но поддаётся под давлением
        float blend = resistBlend * Mathf.Pow(1f - progress, resistCurve);
        return Vector3.Lerp(handOff, SlotOffset(slots[_slotIndex]), blend);
    }

    // ─────────────────────────────────────────────────────────────
    //  STATE: FREE  (H-гейт + магнетизм)
    // ─────────────────────────────────────────────────────────────

    Vector3 UpdateFreeState(Vector3 handOff, Vector2 hand2D)
    {
        Vector3 target = ApplyHGate(handOff);

        int nearest = FindNearest(hand2D, out float dist);

        bool canSnap = nearest >= 0
            && dist < snapRadius
            && (!slots[nearest].requiresReverse || IsReverseHeld());

        if (!canSnap) return target;

        // Магнетизм: тянет к позиции передачи, сильнее ближе к центру
        float t = 1f - dist / snapRadius;   // 0 на краю зоны → 1 в центре
        target = Vector3.Lerp(target, SlotOffset(slots[nearest]), snapSpeed * t * Time.deltaTime);

        if (dist < snapLockDist)
        {
            // ✅ ЗАЩЁЛКИВАНИЕ
            _inGear    = true;
            _slotIndex = nearest;
            SendHapticOnce(hapticIn, 0.06f);
            OnGearChanged?.Invoke(CurrentGear);
            return SlotOffset(slots[nearest]);
        }

        return target;
    }

    // ─────────────────────────────────────────────────────────────
    //  H-ГЕЙТ
    // ─────────────────────────────────────────────────────────────
    //
    //  |Z| < neutralHalfW  → нейтральный коридор:
    //      разрешаем X (выбор колонки), прижимаем Z к 0
    //  иначе → в боковом коридоре:
    //      разрешаем Z (включение/выключение), X прижимаем к ближайшей колонке

    Vector3 ApplyHGate(Vector3 h)
    {
        if (Mathf.Abs(h.z) < neutralHalfW)
            return new Vector3(h.x, 0f, h.z * 0.08f);

        float col = FindNearestColumnX(h.x);
        return new Vector3(Mathf.Lerp(h.x, col, 0.88f), 0f, h.z);
    }

    float FindNearestColumnX(float x)
    {
        float best = 0f, bestDist = float.MaxValue;
        foreach (var s in slots)
        {
            float d = Mathf.Abs(s.pos.x - x);
            if (d < bestDist) { bestDist = d; best = s.pos.x; }
        }
        return best;
    }

    // ─────────────────────────────────────────────────────────────
    //  ВСПОМОГАТЕЛЬНЫЕ
    // ─────────────────────────────────────────────────────────────

    void MoveToOffset(Vector3 offset, float lerpSpeed) =>
        transform.localPosition = Vector3.Lerp(
            transform.localPosition,
            _neutralLocalPos + offset,
            Time.deltaTime * lerpSpeed
        );

    Vector3 GetHandOffset()
    {
        if (_hand == null) return Vector3.zero;
        Vector3 local = transform.parent != null
            ? transform.parent.InverseTransformPoint(_hand.position)
            : _hand.position;
        return local - _neutralLocalPos;
    }

    static Vector3 SlotOffset(GearSlot s) => new(s.pos.x, 0f, s.pos.y);

    int FindNearest(Vector2 pos, out float minDist)
    {
        int best = -1; minDist = float.MaxValue;
        for (int i = 0; i < slots.Length; i++)
        {
            float d = Vector2.Distance(pos, slots[i].pos);
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
        // Используем UnityEngine.XR.InputDevice явно — без using UnityEngine.XR нет конфликтов
        var buf = new List<UnityEngine.XR.InputDevice>();
        foreach (var role in new[]
        {
            UnityEngine.XR.InputDeviceRole.LeftHanded,
            UnityEngine.XR.InputDeviceRole.RightHanded
        })
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
    //  ДЕБАГ GIZMOS
    // ─────────────────────────────────────────────────────────────

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Vector3 basePos = Application.isPlaying ? _neutralLocalPos : transform.localPosition;
        Transform parent = transform.parent;

        foreach (var s in slots)
        {
            Vector3 worldPos = parent != null
                ? parent.TransformPoint(basePos + SlotOffset(s))
                : transform.position + SlotOffset(s);

            Gizmos.color = s.gear < 0 ? Color.red : Color.green;
            Gizmos.DrawWireSphere(worldPos, snapRadius);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(worldPos, snapLockDist);
            UnityEditor.Handles.Label(worldPos + Vector3.up * 0.02f, s.label);
        }
    }
#endif
}
