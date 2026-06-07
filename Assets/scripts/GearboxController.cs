using UnityEngine;

public class TankTransmission : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TankEngine engine;
    [SerializeField] private TankTelemetry telemetry;

    [Header("Gear ratios")]
    [SerializeField] private float reverseRatio = -3.2f;
    [SerializeField] private float neutralRatio = 0f;
    [SerializeField] private float[] forwardRatios =
    {
        3.5f, 2.2f, 1.6f, 1.2f, 0.9f
    };

    [Header("Drivetrain")]
    [SerializeField] private float finalDrive = 4.1f;

    [Header("Clutch behavior")]
    [SerializeField] private float clutchEngageThreshold = 0.2f;

    private float currentWheelTorque;
    private float currentLoadFactor;

    void Update()
    {
        if (engine == null || telemetry == null)
            return;

        ApplyTransmission();
    }

    void ApplyTransmission()
    {
        int gear = telemetry.CurrentGear;
        float clutch = telemetry.ClutchInput;
        float rpm = telemetry.EngineRPM;
        float engineTorque = telemetry.EngineTorque;

        float ratio = GetGearRatio(gear);

        // если сцепление выжато — разрываем связь
        if (clutch > clutchEngageThreshold || ratio == 0f)
        {
            currentWheelTorque = 0f;
            currentLoadFactor = 0f;
            return;
        }

        // базовая передача момента
        float drivetrainRatio = ratio * finalDrive;

        currentWheelTorque = engineTorque * drivetrainRatio;

        // нагрузка обратно на двигатель
        currentLoadFactor = Mathf.Abs(currentWheelTorque) * 0.002f;

        ApplyLoadToEngine(currentLoadFactor);
    }

    float GetGearRatio(int gear)
    {
        if (gear < 0)
            return reverseRatio;

        if (gear == 0)
            return neutralRatio;

        int index = gear - 1;

        if (index < 0 || index >= forwardRatios.Length)
            return 0f;

        return forwardRatios[index];
    }

    void ApplyLoadToEngine(float load)
    {
        // это ключевая связка:
        // двигатель “чувствует” коробку через нагрузку

        telemetry.SpeedKmh = currentWheelTorque * 0.01f;

        // можно расширить:
        // engine.externalLoad = load;
    }

    public float GetWheelTorque()
    {
        return currentWheelTorque;
    }
}
