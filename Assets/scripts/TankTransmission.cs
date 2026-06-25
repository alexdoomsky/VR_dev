using UnityEngine;

public class TankTransmission : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TankTelemetry telemetry;
    [SerializeField] private ClutchController clutchController;

    [Header("Vehicle Speed")]
    [SerializeField] private float reverseMaxSpeed = 8f;

    [SerializeField]
    private float[] gearMaxSpeeds =
    {
        12f, 20f, 32f, 45f, 60f
    };

    [Header("Clutch / Fail Simulation")]
    [SerializeField] private float clutchEngageThreshold = 0.2f;

    [Tooltip("Enable realistic failure rules")]
    [SerializeField] private bool enableStallSimulation = true;

    [Tooltip("Disables all stall logic for testing")]
    [SerializeField] private bool debugNoStall = false;

    [Header("Internal state")]
    private int previousGear = 0;
    private float shiftTimer = 0f;
    private bool wasClutchPressed = false;

    private void Update()
    {
        if (telemetry == null)
            return;

        UpdateTransmission();
    }

    private void UpdateTransmission()
    {
        if (!telemetry.EngineRunning)
        {
            telemetry.TargetSpeedKmh = 0f;
            telemetry.GearLoad = 0f;
            return;
        }

        int gear = telemetry.CurrentGear;
        float throttle = telemetry.ThrottleInput;

        float clutch = clutchController != null
        ? clutchController.GetCoupling() // 1 = сцеплено
        : 1f;

        bool realism = enableStallSimulation && !debugNoStall;

        bool clutchPressed = clutch < clutchEngageThreshold;

        // -----------------------------
        // DETECT SHIFT EVENT
        // -----------------------------
        bool isShifting = gear != previousGear;

        if (isShifting)
        {
            shiftTimer = 0.25f; // окно "опасного переключения"
        }

        if (shiftTimer > 0f)
            shiftTimer -= Time.deltaTime;

        bool unsafeShift = isShifting && !clutchPressed;
        bool shiftUnderLoad = shiftTimer > 0f && throttle > 0.4f && !clutchPressed;

        // резкое отпускание сцепления под газом
        bool clutchDrop = wasClutchPressed && !clutchPressed && throttle > 0.5f;

        wasClutchPressed = clutchPressed;
        previousGear = gear;

        // -----------------------------
        // STALL LOGIC
        // -----------------------------
        if (realism)
        {
            if (unsafeShift || shiftUnderLoad || clutchDrop)
            {
                telemetry.EngineRunning = false;
                telemetry.TargetSpeedKmh = 0f;
                telemetry.GearLoad = 0f;
                return;
            }
        }

        // -----------------------------
        // NORMAL TRANSMISSION
        // -----------------------------
        if (gear == 0)
        {
            telemetry.TargetSpeedKmh = 0f;
            telemetry.GearLoad = 0f;
            return;
        }

        float maxSpeed = GetGearMaxSpeed(gear);

        float coupling = realism ? clutch : 1f;
        float effectiveThrottle = throttle * coupling;

        telemetry.TargetSpeedKmh = maxSpeed * effectiveThrottle;

        float speedDiff = Mathf.Abs(telemetry.TargetSpeedKmh - telemetry.SpeedKmh);
        telemetry.GearLoad = speedDiff * 0.5f;
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
