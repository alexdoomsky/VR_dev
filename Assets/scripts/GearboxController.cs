using UnityEngine;

// Рассчитывает целевую скорость танка и нагрузку на двигатель в зависимости от передачи
public class TankTransmission : MonoBehaviour
{
    [Header("References")]

    // Ссылка на телеметрию танка
    [SerializeField] private TankTelemetry telemetry;

    [Header("Vehicle Speed")]

    // Максимальная скорость заднего хода
    [SerializeField] private float reverseMaxSpeed = 8f;

    // Массив максимальных скоростей для передач
    //
    // [0] = 1 передача
    // [1] = 2 передача
    // [2] = 3 передача
    // ...
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

    // Порог, после которого сцепление считается выжатым
    [SerializeField] private float clutchEngageThreshold = 0.2f;

    // Вызывается Unity каждый кадр
    private void Update()
    {
        // Проверка наличия телеметрии
        if (telemetry == null)
            return;

        UpdateTransmission();
    }

    // Обновляет параметры трансмиссии
    private void UpdateTransmission()
    {
        // Текущая передача
        int gear = telemetry.CurrentGear;

        // Положение педали газа (0..1)
        float throttle = telemetry.ThrottleInput;

        // Положение сцепления (0..1)
        float clutch = telemetry.ClutchInput;

        // Если двигатель не работает
        if (!telemetry.EngineRunning)
        {
            telemetry.TargetSpeedKmh = 0f;
            telemetry.GearLoad = 0f;

            return;
        }

        // ! означает логическое НЕ
        //
        // true -> false
        // false -> true

        // Если сцепление выжато
        if (clutch > clutchEngageThreshold)
        {
            telemetry.TargetSpeedKmh = 0f;
            telemetry.GearLoad = 0f;

            return;
        }

        // Нейтральная передача
        if (gear == 0)
        {
            telemetry.TargetSpeedKmh = 0f;
            telemetry.GearLoad = 0f;

            return;
        }

        // Получение максимальной скорости текущей передачи
        float maxSpeed = GetGearMaxSpeed(gear);

        // throttle от 0 до 1
        //
        // Например:
        // 50 км/ч * 0.5 = 25 км/ч
        telemetry.TargetSpeedKmh =
        maxSpeed * throttle;

        // Вычисление разницы между желаемой и текущей скоростью
        float speedDifference =
        Mathf.Abs(
            telemetry.TargetSpeedKmh -
            telemetry.SpeedKmh
        );

        // Mathf.Abs()
        // Возвращает модуль числа
        //
        // -10 -> 10
        // 10 -> 10

        // Нагрузка на двигатель
        telemetry.GearLoad =
        speedDifference * 0.5f;
    }

    // Возвращает максимальную скорость для указанной передачи
    private float GetGearMaxSpeed(int gear)
    {
        // gear < 0 означает заднюю передачу
        if (gear < 0)

            // Отрицательная скорость для движения назад
            return -reverseMaxSpeed;

        // Передачи начинаются с 1
        // Индексы массива начинаются с 0
        int index = gear - 1;

        // Проверка выхода за границы массива
        if (index < 0 || index >= gearMaxSpeeds.Length)
            return 0f;

        // || означает логическое ИЛИ
        //
        // Условие истинно если хотя бы одна часть истинна

        // Возвращает максимальную скорость передачи
        return gearMaxSpeeds[index];
    }
}