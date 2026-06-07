using System;
using UnityEngine;

public class GearShiftDetector : MonoBehaviour
{
    [System.Serializable]
    public struct GearSlot
    {
        public string label;
        public int gear;

        // локальные углы рычага (X = forward/back, Z = left/right)
        public Vector2 angles;
    }

    [Header("Gear layout")]
    [SerializeField] private GearSlot[] slots;

    [Header("Detection")]
    [SerializeField] private float snapThreshold = 10f;

    [Header("Smoothing")]
    [SerializeField] private float updateSpeed = 20f;

    private int currentGear = 0;

    private Quaternion baseRotation;

    public int CurrentGear => currentGear;

    public event Action<int> OnGearChanged;

    void Awake()
    {
        baseRotation = transform.localRotation;
    }

    void Update()
    {
        Vector2 angles = GetLocalAnglesFromRotation();

        int nearest = FindNearest(angles, out float dist);

        if (nearest < 0)
            return;

        if (dist < snapThreshold)
        {
            int gear = slots[nearest].gear;

            if (gear != currentGear)
            {
                currentGear = gear;
                OnGearChanged?.Invoke(currentGear);
            }
        }
    }

    // --------------------------------------------------
    // ROTATION → ANGLES (X/Z ONLY)
    // --------------------------------------------------

    Vector2 GetLocalAnglesFromRotation()
    {
        Quaternion localRot = Quaternion.Inverse(baseRotation) * transform.localRotation;

        localRot.ToAngleAxis(out float angle, out Vector3 axis);

        // нормализуем
        if (angle > 180f) angle -= 360f;

        Vector3 euler = localRot.eulerAngles;
        euler.x = NormalizeAngle(euler.x);
        euler.z = NormalizeAngle(euler.z);

        return new Vector2(euler.x, euler.z);
    }

    float NormalizeAngle(float a)
    {
        if (a > 180f) a -= 360f;
        return a;
    }

    // --------------------------------------------------
    // NEAREST SLOT
    // --------------------------------------------------

    int FindNearest(Vector2 v, out float bestDist)
    {
        int best = -1;
        bestDist = float.MaxValue;

        for (int i = 0; i < slots.Length; i++)
        {
            float d = Vector2.Distance(v, slots[i].angles);
            if (d < bestDist)
            {
                bestDist = d;
                best = i;
            }
        }

        return best;
    }
}
