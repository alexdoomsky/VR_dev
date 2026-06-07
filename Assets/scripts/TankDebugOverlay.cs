using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class TankDebugOverlay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TankTelemetry telemetry;

    [SerializeField] private TextMeshProUGUI output;

    [Header("Input")]
    [SerializeField] private InputActionReference leftTrigger;

    [SerializeField] private InputActionReference rightTrigger;

    [Header("Performance")]
    [SerializeField] private float updateInterval = 0.1f;

    [SerializeField] private float goodFpsThreshold = 72f;
    [SerializeField] private float badFpsThreshold = 50f;

    private float deltaTime;
    private float milliseconds;
    private int fps;

    private float leftTriggerValue;
    private float rightTriggerValue;

    private WaitForSeconds waitTime;

    private void Awake()
    {
        if (output == null)
            output = GetComponent<TextMeshProUGUI>();
    }

    private void OnEnable()
    {
        leftTrigger?.action.Enable();
        rightTrigger?.action.Enable();
    }

    private void OnDisable()
    {
        leftTrigger?.action.Disable();
        rightTrigger?.action.Disable();
    }

    private void Start()
    {
        waitTime = new WaitForSeconds(updateInterval);

        StartCoroutine(UpdateOverlay());
    }

    private void Update()
    {
        CalculateFPS();

        if (leftTrigger != null)
            leftTriggerValue = leftTrigger.action.ReadValue<float>();

        if (rightTrigger != null)
            rightTriggerValue = rightTrigger.action.ReadValue<float>();
    }

    private void CalculateFPS()
    {
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;

        milliseconds = deltaTime * 1000f;

        if (deltaTime > 0f)
            fps = Mathf.RoundToInt(1f / deltaTime);
    }

    private IEnumerator UpdateOverlay()
    {
        while (true)
        {
            RefreshText();

            yield return waitTime;
        }
    }

    private void RefreshText()
    {
        if (output == null)
            return;

        if (fps >= goodFpsThreshold)
            output.color = Color.green;
        else if (fps >= badFpsThreshold)
            output.color = Color.yellow;
        else
            output.color = Color.red;

        StringBuilder sb = new StringBuilder(1024);

        sb.AppendLine("========== PERFORMANCE ==========");
        sb.AppendLine($"FPS        : {fps}");
        sb.AppendLine($"Frame Time : {milliseconds:F1} ms");

        sb.AppendLine();
        sb.AppendLine("============= INPUT =============");
        sb.AppendLine($"Left Trigger  : {leftTriggerValue:F2}");
        sb.AppendLine($"Right Trigger : {rightTriggerValue:F2}");

        if (telemetry == null)
        {
            sb.AppendLine();
            sb.AppendLine("TankTelemetry NOT ASSIGNED");

            output.text = sb.ToString();
            return;
        }

        sb.AppendLine();
        sb.AppendLine("============ ENGINE ============");
        sb.AppendLine($"State      : {telemetry.EngineState}");
        sb.AppendLine($"Running    : {telemetry.EngineRunning}");
        sb.AppendLine($"Stalled    : {telemetry.EngineStalled}");
        sb.AppendLine($"RPM        : {telemetry.EngineRPM:F0}");
        sb.AppendLine($"Torque     : {telemetry.EngineTorque:F1}");

        sb.AppendLine();
        sb.AppendLine("========= TRANSMISSION =========");
        sb.AppendLine($"Gear       : {telemetry.CurrentGear}");
        sb.AppendLine($"Neutral    : {telemetry.IsNeutral}");
        sb.AppendLine($"Reverse    : {telemetry.IsReverse}");
        sb.AppendLine($"Clutch     : {telemetry.ClutchInput:F2}");

        sb.AppendLine();
        sb.AppendLine("=========== CONTROLS ===========");
        sb.AppendLine($"Throttle   : {telemetry.ThrottleInput:F2}");
        sb.AppendLine($"Left Brake : {telemetry.LeftBrakeInput:F2}");
        sb.AppendLine($"RightBrake : {telemetry.RightBrakeInput:F2}");

        sb.AppendLine();
        sb.AppendLine("=========== MOVEMENT ===========");
        sb.AppendLine($"Moving     : {telemetry.IsMoving}");
        sb.AppendLine($"Speed km/h : {telemetry.SpeedKmh:F1}");
        sb.AppendLine($"Speed m/s  : {telemetry.SpeedMs:F1}");
        sb.AppendLine($"Turn Rate  : {telemetry.TurnRate:F1}");

        sb.AppendLine();
        sb.AppendLine("============ TRACKS ============");
        sb.AppendLine($"Left Force : {telemetry.LeftTrackForce:F1}");
        sb.AppendLine($"RightForce : {telemetry.RightTrackForce:F1}");

        sb.AppendLine($"Left Slip  : {telemetry.LeftTrackSlip:F2}");
        sb.AppendLine($"Right Slip : {telemetry.RightTrackSlip:F2}");

        sb.AppendLine($"Left Gnd   : {telemetry.LeftTrackGrounded}");
        sb.AppendLine($"Right Gnd  : {telemetry.RightTrackGrounded}");

        sb.AppendLine();
        sb.AppendLine("========== ENVIRONMENT =========");
        sb.AppendLine($"Surface    : {telemetry.CurrentSurface}");

        sb.AppendLine();
        sb.AppendLine("=========== SYSTEMS ============");
        sb.AppendLine($"Fuel       : {telemetry.FuelLevel:F1}");
        sb.AppendLine($"Temp       : {telemetry.EngineTemperature:F1}");
        sb.AppendLine($"Oil Press. : {telemetry.OilPressure:F1}");
        sb.AppendLine($"Water level : {telemetry.WaterLevel:F1}");
        sb.AppendLine();
        sb.AppendLine("============ START =============");
        sb.AppendLine($"Fuel       : {telemetry.FuelEnabled}");
        sb.AppendLine($"Air        : {telemetry.AirEnabled}");
        sb.AppendLine($"Ignition   : {telemetry.IgnitionEnabled}");
        sb.AppendLine($"Can Start  : {telemetry.CanStartEngine}");

        output.text = sb.ToString();
    }
}
