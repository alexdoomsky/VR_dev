using UnityEngine;

// Управляет движением и поворотом танка через Rigidbody
[RequireComponent(typeof(Rigidbody))]
public class TankMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TankTelemetry telemetry;
    [SerializeField] private Rigidbody rb;

    [Header("Movement")]

    [Tooltip("Максимальная тяга двигателя")]
    [SerializeField] private float maxDriveForce = 900000f;

    [Tooltip("Тормозное усилие")]
    [SerializeField] private float brakeForce = 25000f;

    [Tooltip("Смещение точки приложения тяги назад от центра")]
    [SerializeField] private float driveForceOffset = 1.5f;

    [Tooltip("Смещение точки приложения тяги вниз")]
    [SerializeField] private float driveForceDownOffset = 0.3f;

    [Header("Turning")]

    [SerializeField] private float turnTorque = 150000f;

    private void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();

        // Если начнёт сильно кувыркаться,
        // раскомментируй и подбери значение.
        // rb.centerOfMass = new Vector3(0f, -0.8f, 0f);
    }

    private void FixedUpdate()
    {
        if (telemetry == null)
            return;

        UpdateSpeed();
        UpdateTurning();
    }

    private void UpdateSpeed()
    {
        Vector3 planarForward =
        Vector3.ProjectOnPlane(
            transform.forward,
            Vector3.up
        ).normalized;

        float currentSpeedMs =
        Vector3.Dot(
            rb.linearVelocity,
            planarForward
        );

        float currentSpeedKmh =
        currentSpeedMs * 3.6f;

        float targetSpeedKmh =
        telemetry.TargetSpeedKmh;

        float speedError =
        targetSpeedKmh - currentSpeedKmh;

        // Ограничиваем ошибку скорости
        speedError =
        Mathf.Clamp(
            speedError,
            -20f,
            20f
        );

        float driveForce =
        speedError / 20f *
        maxDriveForce;

        Vector3 forcePoint =
        transform.position
        - transform.forward * driveForceOffset
        - transform.up * driveForceDownOffset;

        rb.AddForceAtPosition(
            planarForward * driveForce,
            forcePoint,
                ForceMode.Force
        );

        if (Mathf.Abs(targetSpeedKmh) < 0.1f)
        {
            Vector3 horizontalVelocity =
            new Vector3(
                rb.linearVelocity.x,
                0f,
                rb.linearVelocity.z
            );

            rb.AddForce(
                -horizontalVelocity * brakeForce,
                ForceMode.Force
            );
        }

        telemetry.SpeedMs = currentSpeedMs;
        telemetry.SpeedKmh = currentSpeedKmh;
    }

    private void UpdateTurning()
    {
        float steering =
        telemetry.RightBrakeInput -
        telemetry.LeftBrakeInput;

        float speedFactor =
        Mathf.Clamp01(
            Mathf.Abs(telemetry.SpeedKmh) / 10f
        );

        float torque =
        steering *
        turnTorque *
        speedFactor;

        rb.AddTorque(
            Vector3.up * torque,
            ForceMode.Force
        );

        telemetry.TurnRate = torque;
    }
}
