using UnityEngine;

// Симулирует работу двигателя танка:
// запуск, работу, остановку, перегрев и расчёт момента
public class TankEngine : MonoBehaviour
{
    [Header("References")]

    // Ссылка на общую телеметрию танка
    [SerializeField] private TankTelemetry telemetry;

    [Header("Engine settings")]

    // Обороты холостого хода
    [SerializeField] private float idleRPM = 700f;

    // Максимальные обороты двигателя
    [SerializeField] private float maxRPM = 2500f;

    // Скорость набора оборотов
    [SerializeField] private float rpmRiseSpeed = 1200f;

    // Скорость падения оборотов
    [SerializeField] private float rpmFallSpeed = 1800f;

    // Инерция двигателя
    // Чем больше значение, тем медленнее двигатель реагирует
    [SerializeField] private float engineInertia = 0.25f;

    [Header("Torque")]

    // Максимальный крутящий момент двигателя
    [SerializeField] private float maxTorque = 2000f;

    [Header("Stall")]

    // Ниже этого значения двигатель может заглохнуть
    [SerializeField] private float stallRPM = 400f;

    [Header("Thermal")]

    // Температура окружающей среды
    [SerializeField] private float ambientTemp = 20f;

    // Начало перегрева
    [SerializeField] private float overheatThreshold = 90f;

    // Критический перегрев
    [SerializeField] private float criticalThreshold = 105f;

    // Нагрев от оборотов
    [SerializeField] private float heatFromRPM = 0.02f;

    // Нагрев от нагрузки
    [SerializeField] private float heatFromLoad = 0.0008f;

    // Базовое охлаждение
    [SerializeField] private float coolingBase = 0.5f;

    // Влияние уровня воды на охлаждение
    [SerializeField] private float coolingWaterEffect = 2.0f;

    // Скорость расхода воды при перегреве
    [SerializeField] private float waterLossRate = 0.02f;

    // Внутренняя переменная SmoothDamp
    private float rpmVelocity;

    // Свойство только для чтения
    // true если двигатель работает
    public bool IsRunning =>
    telemetry.EngineState == EngineState.Running;

    // true если двигатель запускается
    public bool IsStarting =>
    telemetry.EngineState == EngineState.Starting;

    // true если двигатель заглох
    public bool IsDead =>
    telemetry.EngineState == EngineState.Stalled;

    // Запускает двигатель
    public void StartEngine()
    {
        // Не даём запустить уже работающий двигатель
        if (telemetry.EngineState == EngineState.Running ||
            telemetry.EngineState == EngineState.Starting)
            return;

        telemetry.EngineState = EngineState.Starting;

        telemetry.EngineRunning = false;
        telemetry.EngineStalled = false;

        telemetry.EngineRPM = 0f;
    }

    // Полностью выключает двигатель
    public void StopEngine()
    {
        telemetry.EngineState = EngineState.Off;

        telemetry.EngineRunning = false;
        telemetry.EngineStalled = false;

        telemetry.EngineRPM = 0f;
        telemetry.EngineTorque = 0f;
    }

    // Вызывается Unity каждый кадр
    private void Update()
    {
        if (telemetry == null)
            return;

        // Обновление температуры двигателя
        UpdateTemperature();

        // switch выбирает действие по состоянию двигателя
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

    // Симулирует запуск двигателя
    private void SimulateStart()
    {
        telemetry.EngineRPM +=
        rpmRiseSpeed * Time.deltaTime;

        if (telemetry.EngineRPM >= idleRPM)
        {
            telemetry.EngineRPM = idleRPM;

            telemetry.EngineState = EngineState.Running;
            telemetry.EngineRunning = true;
        }

        telemetry.EngineTorque = 0f;
    }

    // Симуляция работающего двигателя
    private void SimulateRunning()
    {
        float throttle = telemetry.ThrottleInput;

        float clutch = telemetry.ClutchInput;

        // Нагрузка уменьшается при выжатом сцеплении
        float loadPenalty =
        CalculateSimpleLoad() * (1f - clutch);

        float targetRPM =
        idleRPM +
        throttle * (maxRPM - idleRPM)
        - loadPenalty;

        float coupling = 1f - clutch;

        float load =
        CalculateSimpleLoad() * coupling;

        targetRPM -= load;

        // Тернарный оператор
        // Аналог if/else в одну строку
        float speed =
        (throttle > 0.1f)
        ? rpmRiseSpeed
        : rpmFallSpeed;

        // SmoothDamp()
        // Плавно изменяет значение с учётом инерции
        telemetry.EngineRPM =
        Mathf.SmoothDamp(
            telemetry.EngineRPM,
            targetRPM,

            // ref позволяет передавать переменную по ссылке
            ref rpmVelocity,

            engineInertia
        );

        // Ограничение диапазона оборотов
        telemetry.EngineRPM =
        Mathf.Clamp(
            telemetry.EngineRPM,
            idleRPM,
            maxRPM
        );

        // Расчёт крутящего момента
        telemetry.EngineTorque =
        (telemetry.EngineRPM / maxRPM) *
        maxTorque *
        telemetry.ThrottleInput *
        coupling;

        CheckStall();
    }

    // Симуляция заглохшего двигателя
    private void SimulateStall()
    {
        telemetry.EngineRPM -=
        rpmFallSpeed * Time.deltaTime;

        if (telemetry.EngineRPM <= 0f)
        {
            telemetry.EngineRPM = 0f;
            telemetry.EngineState = EngineState.Off;
        }

        telemetry.EngineTorque = 0f;
    }

    // Рассчитывает нагрузку на двигатель
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

    // Проверяет заглох ли двигатель
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

    // Обновляет температуру двигателя
    private void UpdateTemperature()
    {
        float rpmFactor =
        telemetry.EngineRPM * heatFromRPM;

        float loadFactor =
        CalculateSimpleLoad() * heatFromLoad;

        float heat =
        rpmFactor + loadFactor;

        float waterEffect =
        coolingBase +
        telemetry.WaterLevel *
        coolingWaterEffect;

        // Расчёт нагрева и охлаждения
        telemetry.EngineTemperature +=
        (heat - waterEffect) *
        Time.deltaTime;

        // Не позволяем температуре опуститься ниже окружающей среды
        if (telemetry.EngineTemperature < ambientTemp)
            telemetry.EngineTemperature = ambientTemp;

        HandleOverheat();
    }

    // Обрабатывает перегрев двигателя
    private void HandleOverheat()
    {
        float temp =
        telemetry.EngineTemperature;

        // Зона перегрева
        if (temp > overheatThreshold)
        {
            telemetry.WaterLevel -=
            waterLossRate * Time.deltaTime;

            telemetry.WaterLevel =
            Mathf.Clamp01(
                telemetry.WaterLevel
            );
        }

        // Критический перегрев или отсутствие воды
        if (temp > criticalThreshold ||
            telemetry.WaterLevel <= 0f)
        {
            telemetry.EngineState =
            EngineState.Stalled;

            telemetry.EngineStalled = true;
            telemetry.EngineRunning = false;
        }
    }
}