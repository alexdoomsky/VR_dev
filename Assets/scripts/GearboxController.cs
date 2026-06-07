using UnityEngine;

public class TankTransmission : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TankTelemetry telemetry;

    [Header("Vehicle Speed")]
    [SerializeField] private float reverseMaxSpeed = 8f;

    [SerializeField]
    private float[] gearMaxSpeeds =
    {
        12f,
        20f,
        32f,
        45f,
        60f
    };

    [Header("Clutch")]
    [SerializeField] private float clutchEngageThreshold = 0.2f;

    private void Update()
    {
        if (telemetry == null)
            return;

        UpdateTransmission();
    }

    private void UpdateTransmission()
    {
        int gear = telemetry.CurrentGear;

        float throttle = telemetry.ThrottleInput;
        float clutch = telemetry.ClutchInput;

        if (!telemetry.EngineRunning)
        {
            telemetry.TargetSpeedKmh = 0f;
            telemetry.GearLoad = 0f;
            return;
        }

        // сцепление выжато
        if (clutch > clutchEngageThreshold)
        {
            telemetry.TargetSpeedKmh = 0f;
            telemetry.GearLoad = 0f;
            return;
        }

        // нейтраль
        if (gear == 0)
        {
            telemetry.TargetSpeedKmh = 0f;
            telemetry.GearLoad = 0f;
            return;
        }

        float maxSpeed = GetGearMaxSpeed(gear);

        telemetry.TargetSpeedKmh =
        maxSpeed * throttle;

        // нагрузка на двигатель
        float speedDifference =
        Mathf.Abs(
            telemetry.TargetSpeedKmh -
            telemetry.SpeedKmh
        );

        telemetry.GearLoad =
        speedDifference * 0.5f;
    }

    private float GetGearMaxSpeed(int gear)
    {
        if (gear < 0)
            return -reverseMaxSpeed;

        int index = gear - 1;

        if (index < 0 || index >= gearMaxSpeeds.Length)
            return 0f;

        return gearMaxSpeeds[index];
    }
}
