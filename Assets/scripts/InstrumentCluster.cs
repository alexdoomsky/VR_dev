using UnityEngine;

public class InstrumentCluster : MonoBehaviour
{
    [Header("Telemetry")]
    [SerializeField] private TankTelemetry telemetry;

    [Header("Needles")]
    [SerializeField] private GaugeNeedle2D rpmNeedle;
    [SerializeField] private GaugeNeedle2D speedNeedle;
    [SerializeField] private GaugeNeedle2D tempNeedle;
    [SerializeField] private GaugeNeedle2D waterNeedle;

    [Header("Limits")]
    [SerializeField] private float maxRPM = 2500f;
    [SerializeField] private float maxSpeed = 60f;

    [SerializeField] private float tempMin = 20f;
    [SerializeField] private float tempMax = 105f;

    private void Update()
    {
        if (telemetry == null) return;

        // RPM
        rpmNeedle.value = Mathf.Clamp01(telemetry.EngineRPM / maxRPM);

        // Speed
        speedNeedle.value = Mathf.Clamp01(telemetry.SpeedKmh / maxSpeed);

        // Temperature
        tempNeedle.value = Mathf.InverseLerp(tempMin, tempMax, telemetry.EngineTemperature);

        // Water
        waterNeedle.value = Mathf.Clamp01(telemetry.WaterLevel);
    }
}
