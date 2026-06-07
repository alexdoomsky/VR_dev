using UnityEngine;

public class GaugeNeedle2D : MonoBehaviour
{
    public enum GaugeMode
    {
        Linear,
        Circular
    }

    [Header("Mode")]
    [SerializeField] private GaugeMode mode;

    [Header("Input")]
    [Range(0f, 1f)]
    public float value;

    [Header("Smoothing")]
    [SerializeField] private float smooth = 10f;

    [Header("Base rotation (keeps model orientation)")]
    private Vector3 baseRot;

    private Vector3 currentRot;

    [System.Serializable]
    public struct Keyframe2D
    {
        [Range(0f, 1f)]
        public float t;
        public float x;
        public float z;
    }

    [Header("Linear Mapping (ONLY for Linear mode)")]
    public Keyframe2D[] points;

    [Header("Circular Mapping (ONLY for RPM)")]
    public float minAngleX = -50f;
    public float maxAngleX = 228f;

    public float minAngleZ = -75f;
    public float maxAngleZ = -110f;

    private void Awake()
    {
        baseRot = transform.localEulerAngles;
        currentRot = baseRot;
    }

    private void Update()
    {
        Vector3 target = Evaluate(value);

        Vector3 desired = new Vector3(
            target.x,
            baseRot.y,
            target.z
        );

        currentRot = Vector3.Lerp(
            currentRot,
            desired,
            Time.deltaTime * smooth
        );

        transform.localEulerAngles = currentRot;
    }

    private Vector3 Evaluate(float t)
    {
        t = Mathf.Clamp01(t);

        if (mode == GaugeMode.Circular)
        {
            return EvaluateCircular(t);
        }

        return EvaluateLinear(t);
    }

    private Vector3 EvaluateLinear(float t)
    {
        if (points == null || points.Length < 2)
            return Vector3.zero;

        for (int i = 0; i < points.Length - 1; i++)
        {
            if (t >= points[i].t && t <= points[i + 1].t)
            {
                float localT = Mathf.InverseLerp(points[i].t, points[i + 1].t, t);

                float x = Mathf.Lerp(points[i].x, points[i + 1].x, localT);
                float z = Mathf.Lerp(points[i].z, points[i + 1].z, localT);

                return new Vector3(x, 0f, z);
            }
        }

        Keyframe2D last = points[points.Length - 1];
        return new Vector3(last.x, 0f, last.z);
    }

    private Vector3 EvaluateCircular(float t)
    {
        // чистый поворот по дуге
        float x = Mathf.Lerp(minAngleX, maxAngleX, t);
        float z = Mathf.Lerp(minAngleZ, maxAngleZ, t);

        return new Vector3(x, 0f, z);
    }
}
