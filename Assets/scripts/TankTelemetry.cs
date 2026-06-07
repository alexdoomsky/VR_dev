using UnityEngine;

public enum EngineState
{
    Off,
    Starting,
    Running,
    Stalled
}

public enum SurfaceType
{
    Unknown,
    Asphalt,
    Dirt,
    Mud,
    Wood
}

public class TankTelemetry : MonoBehaviour
{
    [SerializeField] private GearShiftDetector gearShift;
    [Header("Engine")]
    public EngineState EngineState = EngineState.Off;

    [Tooltip("Current engine RPM")]
    public float EngineRPM;

    [Tooltip("Current engine torque")]
    public float EngineTorque;

    [Tooltip("Engine running flag")]
    public bool EngineRunning;

    [Tooltip("Engine stalled flag")]
    public bool EngineStalled;

    [Header("Transmission")]
    [Tooltip("-1 = Reverse, 0 = Neutral, 1..5 = Forward gears")]
    public int CurrentGear;

    [Tooltip("0 = released, 1 = fully pressed")]
    [Range(0f, 1f)]
    public float ClutchInput;
    public float ClutchCoupling;
    public float EngineLoad;
    public float GearLoad;
    [Header("Controls")]
    [Tooltip("0 = released, 1 = full throttle")]
    [Range(0f, 1f)]
    public float ThrottleInput;

    [Tooltip("0 = released, 1 = full brake")]
    [Range(0f, 1f)]
    public float LeftBrakeInput;

    [Tooltip("0 = released, 1 = full brake")]
    [Range(0f, 1f)]
    public float RightBrakeInput;

    [Header("Movement")]
    [Tooltip("Vehicle speed in km/h")]
    public float SpeedKmh;

    [Tooltip("Linear velocity magnitude in m/s")]
    public float SpeedMs;

    [Tooltip("Angular velocity around Y axis")]
    public float TurnRate;

    [Header("Tracks")]
    public float LeftTrackForce;
    public float RightTrackForce;

    [Tooltip("0 = no slip")]
    public float LeftTrackSlip;

    [Tooltip("0 = no slip")]
    public float RightTrackSlip;

    public bool LeftTrackGrounded;
    public bool RightTrackGrounded;

    [Header("Environment")]
    public SurfaceType CurrentSurface = SurfaceType.Unknown;

    [Header("Vehicle Systems")]
    [Range(0f, 100f)]
    public float FuelLevel = 100f;
    public float OilPressure;
    public float EngineTemperature;
    public float WaterLevel = 1f; // 0..1
    [Header("Diagnostics")]
    public bool IgnitionEnabled;

    public bool FuelEnabled;

    public bool AirEnabled;

    public bool CanStartEngine;

    public bool DriverPresent;

    public bool IsMoving;

    public bool IsNeutral;

    public bool IsReverse;

    private void Update()
    {
        IsNeutral = CurrentGear == 0;
        IsReverse = CurrentGear < 0;
        IsMoving = SpeedKmh > 0.1f;
    }
    private void Awake()
    {
        if (gearShift != null)
            gearShift.OnGearChanged += OnGearChanged;
    }

    private void OnDestroy()
    {
        if (gearShift != null)
            gearShift.OnGearChanged -= OnGearChanged;
    }
    private void OnGearChanged(int gear)
    {
        CurrentGear = gear;
    }
}
