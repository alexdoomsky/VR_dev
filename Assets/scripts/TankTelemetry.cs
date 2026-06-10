using UnityEngine;
//DATA

// Перечисление состояний двигателя танка
// enum = тип данных с фиксированным набором значений
// Используется вместо "магических чисел" (0,1,2,3)
public enum EngineState
{
    Off,        // двигатель выключен
    Starting,   // идёт запуск
    Running,    // двигатель работает
    Stalled     // двигатель заглох
}

// Тип поверхности, по которой движется танк
// влияет на сцепление, трение и поведение гусениц
public enum SurfaceType
{
    Unknown,
    Asphalt,
    Dirt,
    Mud,
    Wood
}

// Основной класс телеметрии танка
// MonoBehaviour = базовый класс Unity для компонентов сцены
// Этот скрипт НЕ управляет танком напрямую, а хранит и обновляет состояние (data container)
public class TankTelemetry : MonoBehaviour
{
    // Ссылка на компонент переключения передач
    // GearShiftDetector — внешний скрипт, который генерирует событие смены передачи
    [SerializeField] private GearShiftDetector gearShift;

    [Header("Engine")]

    // Текущее состояние двигателя (Off/Starting/Running/Stalled)
    // public → доступно другим скриптам напрямую (плохая инкапсуляция, но упрощает дебаг)
    public EngineState EngineState = EngineState.Off;

    [Tooltip("Current engine RPM")]
    // RPM = revolutions per minute (обороты двигателя)
    public float EngineRPM;

    [Tooltip("Current engine torque")]
    // Torque = крутящий момент двигателя
    public float EngineTorque;

    [Tooltip("Engine running flag")]
    // true = двигатель запущен и работает
    public bool EngineRunning;

    [Tooltip("Engine stalled flag")]
    // true = двигатель заглох (не может работать без перезапуска)
    public bool EngineStalled;

    [Header("Transmission")]

    [Tooltip("-1 = Reverse, 0 = Neutral, 1..5 = Forward gears")]
    // текущая передача коробки
    public int CurrentGear;

    [Tooltip("0 = released, 1 = fully pressed")]
    [Range(0f, 1f)]
    // положение сцепления (0 = отпущено, 1 = выжато)
    public float ClutchInput;

    // степень "сцепления" двигателя и трансмиссии
    // влияет на передачу крутящего момента
    public float ClutchCoupling;

    // нагрузка на двигатель (условная величина)
    public float EngineLoad;

    // нагрузка на коробку передач
    public float GearLoad;

    [Header("Controls")]

    [Tooltip("0 = released, 1 = full throttle")]
    [Range(0f, 1f)]
    // газ / ускорение
    public float ThrottleInput;

    [Tooltip("0 = released, 1 = full brake")]
    [Range(0f, 1f)]
    // левый тормоз (гусеница)
    public float LeftBrakeInput;

    [Tooltip("0 = released, 1 = full brake")]
    [Range(0f, 1f)]
    // правый тормоз (гусеница)
    public float RightBrakeInput;

    [Header("Movement")]

    [Tooltip("Vehicle speed in km/h")]
    // целевая скорость (может отличаться от фактической)
    public float TargetSpeedKmh;

    // фактическая скорость в km/h
    public float SpeedKmh;

    [Tooltip("Linear velocity magnitude in m/s")]
    // скорость в метрах в секунду (физическая единица Unity)
    public float SpeedMs;

    [Tooltip("Angular velocity around Y axis")]
    // скорость поворота вокруг вертикальной оси (yaw)
    public float TurnRate;

    [Header("Tracks")]

    // сила, создаваемая левой гусеницей
    public float LeftTrackForce;

    // сила правой гусеницы
    public float RightTrackForce;

    // скорость вращения левой гусеницы
    public float LeftTrackSpeed;

    // скорость правой гусеницы
    public float RightTrackSpeed;

    [Tooltip("0 = no slip")]
    // проскальзывание гусеницы (0 = идеальное сцепление)
    public float LeftTrackSlip;

    [Tooltip("0 = no slip")]
    public float RightTrackSlip;

    // касается ли левая гусеница земли
    public bool LeftTrackGrounded;

    // касается ли правая гусеница земли
    public bool RightTrackGrounded;

    [Header("Environment")]

    // тип поверхности под танком (влияет на физику)
    public SurfaceType CurrentSurface = SurfaceType.Unknown;

    [Header("Vehicle Systems")]

    [Range(0f, 100f)]
    // уровень топлива (0–100%)
    public float FuelLevel = 100f;

    // давление масла в системе двигателя
    public float OilPressure;

    // температура двигателя
    public float EngineTemperature;

    [Tooltip("0..1 water level (cooling system)")]
    public float WaterLevel = 1f;

    [Header("Diagnostics")]

    // включено ли зажигание
    public bool IgnitionEnabled;

    // подача топлива активна ли
    public bool FuelEnabled;

    // подача воздуха активна ли
    public bool AirEnabled;

    // можно ли запустить двигатель (условное состояние)
    public bool CanStartEngine;

    // находится ли водитель в танке
    public bool DriverPresent;

    // движется ли танк (обобщённый флаг)
    public bool IsMoving;

    // находится ли в нейтрали
    public bool IsNeutral;

    // включена ли задняя передача
    public bool IsReverse;

    // Update вызывается каждый кадр Unity
    private void Update()
    {
        // нейтраль = передача 0
        IsNeutral = CurrentGear == 0;

        // задний ход = отрицательная передача
        IsReverse = CurrentGear < 0;

        // движение считается активным, если скорость выше порога шума
        IsMoving = SpeedKmh > 0.1f;
    }

    // Awake вызывается при создании объекта до Start
    // здесь подписка на события других компонентов
    private void Awake()
    {
        // если есть компонент переключения передач
        if (gearShift != null)

            // подписка на событие смены передачи
            // += добавляет метод как обработчик события
            gearShift.OnGearChanged += OnGearChanged;
    }

    // OnDestroy вызывается при удалении объекта или выходе из сцены
    // важно для очистки подписок (иначе будет утечка событий)
    private void OnDestroy()
    {
        if (gearShift != null)

            // отписка от события (очень важно для Unity event system)
            gearShift.OnGearChanged -= OnGearChanged;
    }

    // callback-метод события смены передачи
    // вызывается GearShiftDetector'ом
    // gear — новая передача (int)
    private void OnGearChanged(int gear)
    {
        // обновляем текущую передачу в телеметрии
        CurrentGear = gear;
    }
}