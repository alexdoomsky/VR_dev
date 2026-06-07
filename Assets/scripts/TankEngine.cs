using UnityEngine;

public class TankEngine : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TankTelemetry telemetry;

    [Header("Engine settings")]
    [SerializeField] private float idleRPM = 700f;
    [SerializeField] private float maxRPM = 2500f;

    [SerializeField] private float rpmRiseSpeed = 1200f;
    [SerializeField] private float rpmFallSpeed = 1800f;

    [SerializeField] private float engineInertia = 0.25f;

    [Header("Torque")]
    [SerializeField] private float maxTorque = 2000f;

    [Header("Stall")]
    [SerializeField] private float stallRPM = 400f;
    [Header("Thermal")]
    [SerializeField] private float ambientTemp = 20f;

    [SerializeField] private float overheatThreshold = 90f;
    [SerializeField] private float criticalThreshold = 105f;

    [SerializeField] private float heatFromRPM = 0.02f;
    [SerializeField] private float heatFromLoad = 0.0008f;

    [SerializeField] private float coolingBase = 0.5f;
    [SerializeField] private float coolingWaterEffect = 2.0f;

    [SerializeField] private float waterLossRate = 0.02f;
    private float rpmVelocity;
    public bool IsRunning =>
    telemetry.EngineState == EngineState.Running;

    public bool IsStarting =>
    telemetry.EngineState == EngineState.Starting;

    public bool IsDead =>
    telemetry.EngineState == EngineState.Stalled;
    public void StartEngine()
    {
        if (telemetry.EngineState == EngineState.Running ||
            telemetry.EngineState == EngineState.Starting)
            return;

        telemetry.EngineState = EngineState.Starting;

        telemetry.EngineRunning = false;
        telemetry.EngineStalled = false;

        telemetry.EngineRPM = 0f;
    }

    public void StopEngine()
    {
        telemetry.EngineState = EngineState.Off;

        telemetry.EngineRunning = false;
        telemetry.EngineStalled = false;

        telemetry.EngineRPM = 0f;
        telemetry.EngineTorque = 0f;
    }

    private void Update()
    {

        if (telemetry == null)
            return;
       UpdateTemperature();
        switch (telemetry.EngineState)
        {
            case EngineState.Starting:
                SimulateStart();
                break;

            case EngineState.Running:
                SimulateRunning();
                break;

            case EngineState.Stalled:
                SimulateStall();
                break;

            case EngineState.Off:
                telemetry.EngineRPM = 0f;
                telemetry.EngineTorque = 0f;
                break;
        }
    }

    private void SimulateStart()
    {
        telemetry.EngineRPM += rpmRiseSpeed * Time.deltaTime;

        if (telemetry.EngineRPM >= idleRPM)
        {
            telemetry.EngineRPM = idleRPM;

            telemetry.EngineState = EngineState.Running;
            telemetry.EngineRunning = true;
        }

        telemetry.EngineTorque = 0f;
    }

    private void SimulateRunning()
    {
        float throttle = telemetry.ThrottleInput;

        float clutch = telemetry.ClutchInput;

        // если сцепление выжато → двигатель не нагружен коробкой
        float loadPenalty = CalculateSimpleLoad() * (1f - clutch);

        float targetRPM =
        idleRPM +
        throttle * (maxRPM - idleRPM)
        - loadPenalty;


        float coupling = 1f - clutch;

        // нагрузка от трансмиссии
        float load = CalculateSimpleLoad() * coupling;

        targetRPM -= load;

        float speed = (throttle > 0.1f)
        ? rpmRiseSpeed
        : rpmFallSpeed;

        telemetry.EngineRPM = Mathf.SmoothDamp(
            telemetry.EngineRPM,
            targetRPM,
            ref rpmVelocity,
            engineInertia
        );

        telemetry.EngineRPM = Mathf.Clamp(
            telemetry.EngineRPM,
            idleRPM,
            maxRPM
        );


        telemetry.EngineTorque =
        (telemetry.EngineRPM / maxRPM) *
        maxTorque *
        telemetry.ThrottleInput *
        coupling;

        CheckStall();
    }

    private void SimulateStall()
    {
        telemetry.EngineRPM -= rpmFallSpeed * Time.deltaTime;

        if (telemetry.EngineRPM <= 0f)
        {
            telemetry.EngineRPM = 0f;
            telemetry.EngineState = EngineState.Off;
        }

        telemetry.EngineTorque = 0f;
    }

    private float CalculateSimpleLoad()
    {
        float movementLoad =
        telemetry.SpeedKmh * 8f;

        float brakeLoad =
        (telemetry.LeftBrakeInput +
        telemetry.RightBrakeInput) * 300f;

        float gearboxLoad =
        telemetry.GearLoad * 100f;

        return movementLoad +
        brakeLoad +
        gearboxLoad;
    }

    private void CheckStall()
    {
        bool heavyLoad =
        CalculateSimpleLoad() > 800f;

        bool lowRPM =
        telemetry.EngineRPM < stallRPM;

        bool noThrottle =
        telemetry.ThrottleInput < 0.1f;

        if (heavyLoad && lowRPM && noThrottle)
        {
            telemetry.EngineState = EngineState.Stalled;
            telemetry.EngineStalled = true;
            telemetry.EngineRunning = false;
        }
    }
    private void UpdateTemperature()
    {
        float rpmFactor = telemetry.EngineRPM * heatFromRPM;
        float loadFactor = CalculateSimpleLoad() * heatFromLoad;

        float heat = rpmFactor + loadFactor;

        float waterEffect = coolingBase + telemetry.WaterLevel * coolingWaterEffect;

        // нагрев / охлаждение
        telemetry.EngineTemperature += (heat - waterEffect) * Time.deltaTime;

        // стартовая защита
        if (telemetry.EngineTemperature < ambientTemp)
            telemetry.EngineTemperature = ambientTemp;

        HandleOverheat();
    }
    private void HandleOverheat()
    {
        float temp = telemetry.EngineTemperature;

        // зона перегрева
        if (temp > overheatThreshold)
        {
            // вода начинает расходоваться только при длительном перегреве
            telemetry.WaterLevel -= waterLossRate * Time.deltaTime;

            telemetry.WaterLevel = Mathf.Clamp01(telemetry.WaterLevel);
        }

        // критическая зона → смерть двигателя
        if (temp > criticalThreshold || telemetry.WaterLevel <= 0f)
        {
            telemetry.EngineState = EngineState.Stalled;
            telemetry.EngineStalled = true;
            telemetry.EngineRunning = false;
        }
    }
}
