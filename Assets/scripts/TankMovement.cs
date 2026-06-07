using UnityEngine;

public class TankMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TankTelemetry telemetry;

    [Header("Movement")]
    [SerializeField] private float acceleration = 8f;
    [SerializeField] private float deceleration = 3f;

    [Header("Turning")]
    [SerializeField] private float turnRate = 50f;

    private float currentSpeedKmh;

    private void Update()
    {
        if (telemetry == null)
            return;

        UpdateSpeed();
        UpdateTurning();
    }

    private void UpdateSpeed()
    {
        float targetSpeed = telemetry.TargetSpeedKmh;

        float speedChangeRate =
        Mathf.Abs(targetSpeed) > Mathf.Abs(currentSpeedKmh)
        ? acceleration
        : deceleration;

        currentSpeedKmh = Mathf.MoveTowards(
            currentSpeedKmh,
            targetSpeed,
            speedChangeRate * Time.deltaTime * 10f
        );

        telemetry.SpeedKmh = currentSpeedKmh;
        telemetry.SpeedMs = currentSpeedKmh / 3.6f;

        transform.position +=
        transform.forward *
        telemetry.SpeedMs *
        Time.deltaTime;
    }

    private void UpdateTurning()
    {
        float steering =
        telemetry.RightBrakeInput -
        telemetry.LeftBrakeInput;

        float speedFactor =
        Mathf.Clamp01(
            Mathf.Abs(currentSpeedKmh) / 20f
        );

        float rotation =
        steering *
        turnRate *
        speedFactor;

        transform.Rotate(
            0f,
            rotation * Time.deltaTime,
            0f
        );

        telemetry.TurnRate = rotation;
    }
}
