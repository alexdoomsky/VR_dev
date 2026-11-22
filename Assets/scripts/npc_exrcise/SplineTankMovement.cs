
using System;
using UnityEngine;
using UnityEngine.Splines;

public class SplineTankMovement : MonoBehaviour
{
    [Header("Spline")]
    [SerializeField] private SplineContainer splineContainer;

    [Header("Movement")]
    [SerializeField] private float speed = 3f;

    [Tooltip("Автоматически запускать движение при Play")]
    [SerializeField] private bool moveOnStart = false;

    [Tooltip("Общая тестовая задержка при автоматическом запуске")]
    [SerializeField] private float testSharedStartDelay = 2f;

    [Tooltip("Дополнительная индивидуальная задержка")]
    [SerializeField] private float individualStartDelay = 0f;

    [Header("Visuals")]
    [SerializeField] private GameObject normalVisual;
    [SerializeField] private GameObject destroyedVisual;

    [Header("Rotation")]
    [SerializeField] private bool rotateToPath = true;
    [SerializeField] private Vector3 rotationOffset;

    [Header("Spline Sampling")]
    [SerializeField, Min(10)] private int resolution = 500;

    private Vector3[] points;
    private float[] distances;

    private float totalLength;
    private float currentDistance;

    private float lateralOffset;
    private float verticalOffset;

    private bool isMoving;
    private bool isWaiting;
    private bool isDestroyed;
    private bool isEscaped;

    private float delayTimer;

    public bool IsDestroyed => isDestroyed;
    public bool IsEscaped => isEscaped;
    public bool IsMoving => isMoving;

    public event Action<SplineTankMovement> OnDestroyed;
    public event Action<SplineTankMovement> OnEscaped;

    private void Awake()
    {
        BuildDistanceTable();
    }

    private void Start()
    {
        if (points == null || points.Length == 0)
            return;

        if (normalVisual != null)
            normalVisual.SetActive(true);

        if (destroyedVisual != null)
            destroyedVisual.SetActive(false);

        CalculateInitialPosition();
        UpdateTransform();

        if (moveOnStart)
            StartMoving(testSharedStartDelay);
    }

    private void Update()
    {
        if (isDestroyed || isEscaped)
            return;

        if (isWaiting)
        {
            delayTimer -= Time.deltaTime;

            if (delayTimer <= 0f)
            {
                isWaiting = false;
                isMoving = true;
            }

            return;
        }

        if (!isMoving)
            return;

        currentDistance += speed * Time.deltaTime;

        if (currentDistance >= totalLength)
        {
            currentDistance = totalLength;
            UpdateTransform();

            isMoving = false;
            MarkEscaped();
            return;
        }

        UpdateTransform();
    }

    public void StartMoving(float sharedDelay = 0f)
    {
        if (isDestroyed || isEscaped)
            return;

        if (points == null || points.Length == 0)
            return;

        if (isMoving || isWaiting)
            return;

        delayTimer = Mathf.Max(0f, sharedDelay) +
        Mathf.Max(0f, individualStartDelay);

        if (delayTimer > 0f)
            isWaiting = true;
        else
            isMoving = true;
    }

    public void StopMoving()
    {
        isMoving = false;
        isWaiting = false;
    }

    public void DestroyTank()
    {
        if (isDestroyed || isEscaped)
            return;

        isDestroyed = true;
        StopMoving();

        if (normalVisual != null)
            normalVisual.SetActive(false);

        if (destroyedVisual != null)
            destroyedVisual.SetActive(true);

        OnDestroyed?.Invoke(this);
    }

    public void MarkEscaped()
    {
        if (isDestroyed || isEscaped)
            return;

        isEscaped = true;
        StopMoving();

        OnEscaped?.Invoke(this);
    }

    [ContextMenu("Test/Destroy NPC")]
    private void TestDestroyNPC()
    {
        DestroyTank();
    }

    private void BuildDistanceTable()
    {
        if (splineContainer == null)
        {
            Debug.LogError(
                "[SplineTankMovement] SplineContainer не назначен.",
                this
            );
            return;
        }

        points = new Vector3[resolution + 1];
        distances = new float[resolution + 1];

        totalLength = 0f;

        for (int i = 0; i <= resolution; i++)
        {
            float t = (float)i / resolution;

            points[i] = ToVector3(
                splineContainer.EvaluatePosition(t)
            );

            if (i > 0)
            {
                totalLength += Vector3.Distance(
                    points[i - 1],
                    points[i]
                );
            }

            distances[i] = totalLength;
        }
    }

    private void CalculateInitialPosition()
    {
        Vector3 initialPosition = transform.position;

        float closestSqrDistance = float.MaxValue;
        float closestDistance = 0f;
        Vector3 closestPoint = Vector3.zero;
        float closestT = 0f;

        for (int i = 1; i < points.Length; i++)
        {
            Vector3 segmentStart = points[i - 1];
            Vector3 segmentEnd = points[i];

            Vector3 segment = segmentEnd - segmentStart;
            float segmentLengthSqr = segment.sqrMagnitude;

            if (segmentLengthSqr < 0.0001f)
                continue;

            float projection = Mathf.Clamp01(
                Vector3.Dot(
                    initialPosition - segmentStart,
                    segment
                ) / segmentLengthSqr
            );

            Vector3 projectedPoint =
            segmentStart + segment * projection;

            float sqrDistance =
            (initialPosition - projectedPoint).sqrMagnitude;

            if (sqrDistance >= closestSqrDistance)
                continue;

            closestSqrDistance = sqrDistance;
            closestPoint = projectedPoint;

            closestDistance = Mathf.Lerp(
                distances[i - 1],
                distances[i],
                projection
            );

            closestT = Mathf.Lerp(
                (float)(i - 1) / resolution,
                                  (float)i / resolution,
                                  projection
            );
        }

        currentDistance = closestDistance;

        Vector3 tangent = ToVector3(
            splineContainer.EvaluateTangent(closestT)
        ).normalized;

        if (tangent.sqrMagnitude < 0.0001f)
            tangent = Vector3.forward;

        Quaternion pathRotation = Quaternion.LookRotation(
            tangent,
            Vector3.up
        );

        Vector3 right = pathRotation * Vector3.right;
        Vector3 offset = initialPosition - closestPoint;

        lateralOffset = Vector3.Dot(offset, right);
        verticalOffset = Vector3.Dot(offset, Vector3.up);
    }

    private void UpdateTransform()
    {
        float t = GetNormalizedPosition(currentDistance);

        Vector3 pathPosition = ToVector3(
            splineContainer.EvaluatePosition(t)
        );

        transform.position =
        pathPosition +
        GetRightDirection(t) * lateralOffset +
        Vector3.up * verticalOffset;

        if (!rotateToPath)
            return;

        Vector3 tangent = ToVector3(
            splineContainer.EvaluateTangent(t)
        );

        if (tangent.sqrMagnitude < 0.0001f)
            return;

        Quaternion pathRotation = Quaternion.LookRotation(
            tangent.normalized,
            Vector3.up
        );

        transform.rotation = pathRotation *
        Quaternion.Euler(rotationOffset);
    }

    private Vector3 GetRightDirection(float t)
    {
        Vector3 tangent = ToVector3(
            splineContainer.EvaluateTangent(t)
        );

        if (tangent.sqrMagnitude < 0.0001f)
            return Vector3.right;

        return Quaternion.LookRotation(
            tangent.normalized,
            Vector3.up
        ) * Vector3.right;
    }

    private float GetNormalizedPosition(float distance)
    {
        distance = Mathf.Clamp(distance, 0f, totalLength);

        int low = 0;
        int high = distances.Length - 1;

        while (low < high)
        {
            int middle = (low + high) / 2;

            if (distances[middle] < distance)
                low = middle + 1;
            else
                high = middle;
        }

        int upperIndex = low;
        int lowerIndex = Mathf.Max(0, upperIndex - 1);

        float segmentLength =
        distances[upperIndex] - distances[lowerIndex];

        float segmentProgress = segmentLength > 0.0001f
        ? (distance - distances[lowerIndex]) / segmentLength
        : 0f;

        float lowerT = (float)lowerIndex / resolution;
        float upperT = (float)upperIndex / resolution;

        return Mathf.Lerp(
            lowerT,
            upperT,
            segmentProgress
        );
    }

    private static Vector3 ToVector3(Unity.Mathematics.float3 value)
    {
        return new Vector3(value.x, value.y, value.z);
    }

    private void OnValidate()
    {
        speed = Mathf.Max(0f, speed);
        testSharedStartDelay = Mathf.Max(0f, testSharedStartDelay);
        individualStartDelay = Mathf.Max(0f, individualStartDelay);
        resolution = Mathf.Max(10, resolution);
    }
}
