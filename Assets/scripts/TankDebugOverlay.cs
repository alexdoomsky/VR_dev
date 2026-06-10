using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

// Отладочный оверлей с телеметрией танка и FPS
//DEBUG
//VISUAL
public class TankDebugOverlay : MonoBehaviour
{
    [Header("References")]

    // Общая телеметрия танка
    [SerializeField] private TankTelemetry telemetry;

    // Текстовое поле TextMeshPro
    [SerializeField] private TextMeshProUGUI output;

    [Header("Input")]

    // Ссылка на левый триггер контроллера
    [SerializeField] private InputActionReference leftTrigger;

    // Ссылка на правый триггер контроллера
    [SerializeField] private InputActionReference rightTrigger;

    [Header("Performance")]

    // Интервал обновления текста
    [SerializeField] private float updateInterval = 0.1f;

    // FPS считается хорошим выше этого значения
    [SerializeField] private float goodFpsThreshold = 72f;

    // FPS считается плохим ниже этого значения
    [SerializeField] private float badFpsThreshold = 50f;

    // Усреднённое время кадра
    private float deltaTime;

    // Время кадра в миллисекундах
    private float milliseconds;

    // Количество кадров в секунду
    private int fps;

    // Значение левого триггера
    private float leftTriggerValue;

    // Значение правого триггера
    private float rightTriggerValue;

    // Кэшированный объект ожидания для корутины
    private WaitForSeconds waitTime;

    // Вызывается при создании объекта
    private void Awake()
    {
        // Если ссылка не назначена вручную
        if (output == null)

            // GetComponent<T>()
            // Получает компонент указанного типа
            output = GetComponent<TextMeshProUGUI>();
    }

    // Вызывается при включении объекта
    private void OnEnable()
    {
        // ?. проверяет что объект не null

        leftTrigger?.action.Enable();
        rightTrigger?.action.Enable();
    }

    // Вызывается при выключении объекта
    private void OnDisable()
    {
        leftTrigger?.action.Disable();
        rightTrigger?.action.Disable();
    }

    // Вызывается Unity один раз после Awake
    private void Start()
    {
        // WaitForSeconds заставляет корутину ждать указанное время
        waitTime = new WaitForSeconds(updateInterval);

        // Запуск корутины
        StartCoroutine(UpdateOverlay());
    }

    // Вызывается каждый кадр
    private void Update()
    {
        CalculateFPS();

        if (leftTrigger != null)

            // ReadValue<float>()
            // Считывает текущее значение Input Action
            leftTriggerValue =
            leftTrigger.action.ReadValue<float>();

        if (rightTrigger != null)
            rightTriggerValue =
            rightTrigger.action.ReadValue<float>();
    }

    // Вычисляет FPS и время кадра
    private void CalculateFPS()
    {
        // Time.unscaledDeltaTime
        // Время кадра без учёта Time.timeScale

        deltaTime +=
        (Time.unscaledDeltaTime - deltaTime)
        * 0.1f;

        milliseconds =
        deltaTime * 1000f;

        if (deltaTime > 0f)

            // RoundToInt()
            // Округляет число до int
            fps =
            Mathf.RoundToInt(
                1f / deltaTime
            );
    }

    // Корутина обновления текста
    private IEnumerator UpdateOverlay()
    {
        // Бесконечный цикл
        while (true)
        {
            RefreshText();

            // yield return приостанавливает выполнение корутины
            yield return waitTime;
        }
    }

    // Формирует текст отладочной панели
    private void RefreshText()
    {
        if (output == null)
            return;

        // Изменение цвета по FPS
        if (fps >= goodFpsThreshold)

            // Color.green
            // Предопределённый зелёный цвет Unity
            output.color = Color.green;

        else if (fps >= badFpsThreshold)
            output.color = Color.yellow;

        else
            output.color = Color.red;

        // StringBuilder эффективнее обычной конкатенации строк
        StringBuilder sb =
        new StringBuilder(1024);

        // AppendLine()
        // Добавляет строку и символ переноса
        sb.AppendLine("========== PERFORMANCE ==========");
        sb.AppendLine($"FPS        : {fps}");
        sb.AppendLine($"Frame Time : {milliseconds:F1} ms");
		// :Fn - количество знаков после запятой
        sb.AppendLine();
        sb.AppendLine("============= INPUT =============");
        sb.AppendLine($"Left Trigger  : {leftTriggerValue:F2}");
        sb.AppendLine($"Right Trigger : {rightTriggerValue:F2}");

        if (telemetry == null)
        {
            sb.AppendLine();
            sb.AppendLine("TankTelemetry NOT ASSIGNED");

            // ToString()
            // Преобразует StringBuilder в строку
            output.text = sb.ToString();

            return;
        }

        // Формирование разделов телеметрии

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

        // Вывод готового текста на экран
        output.text = sb.ToString();
    }
}