using UnityEngine;

// Управляет движением и поворотом танка
public class TankMovement : MonoBehaviour
{
    [Header("References")]

    // Общая телеметрия танка
    [SerializeField] private TankTelemetry telemetry;

    [Header("Movement")]

    // Скорость разгона
    [SerializeField] private float acceleration = 8f;

    // Скорость замедления
    [SerializeField] private float deceleration = 3f;

    [Header("Turning")]

    // Максимальная скорость поворота
    [SerializeField] private float turnRate = 50f;

    // Текущая скорость танка
    private float currentSpeedKmh;

    // Вызывается Unity каждый кадр
    private void Update()
    {
        if (telemetry == null)
            return;

        UpdateSpeed();
        UpdateTurning();
    }

    // Обновляет скорость и перемещение танка
    private void UpdateSpeed()
    {
        // Желаемая скорость от коробки передач
        float targetSpeed = telemetry.TargetSpeedKmh;

        // Тернарный оператор
        // Если нужно разгоняться -> acceleration
        // Если нужно тормозить -> deceleration
        float speedChangeRate =
        Mathf.Abs(targetSpeed) > Mathf.Abs(currentSpeedKmh)
        ? acceleration
        : deceleration;

        // Mathf.MoveTowards()
        // Двигает значение к цели с фиксированной скоростью
        currentSpeedKmh = Mathf.MoveTowards(
            currentSpeedKmh,
            targetSpeed,

            // Time.deltaTime
            // Время между кадрами
            speedChangeRate * Time.deltaTime * 10f
        );

        // Сохраняем скорость в телеметрию
        telemetry.SpeedKmh = currentSpeedKmh;

        // Перевод км/ч в м/с
        //
        // 1 м/с = 3.6 км/ч
        telemetry.SpeedMs =
        currentSpeedKmh / 3.6f;

        // transform.forward
        // Направление вперёд относительно объекта
        //
        // transform.position
        // Текущая позиция объекта
        transform.position +=
        transform.forward *
        telemetry.SpeedMs *
        Time.deltaTime;
    }

    // Обновляет поворот танка
    private void UpdateTurning()
    {
        // Разница между левым и правым тормозом
        //
        // Если правый тормозит сильнее:
        // значение положительное
        //
        // Если левый тормозит сильнее:
        // значение отрицательное
        float steering =
        telemetry.RightBrakeInput -
        telemetry.LeftBrakeInput;

        // Mathf.Abs()
        // Возвращает модуль числа
        //
        // Mathf.Clamp01()
        // Ограничивает диапазоном 0..1
        float speedFactor =
        Mathf.Clamp01(
            Mathf.Abs(currentSpeedKmh) / 20f
        );

        // Итоговая скорость поворота
        float rotation =
        steering *
        turnRate *
        speedFactor;

        // transform.Rotate()
        // Поворачивает объект на указанные углы
        transform.Rotate(
            0f,

            // Поворот вокруг оси Y
            rotation * Time.deltaTime,

            0f
        );

        // Сохраняем скорость поворота
        telemetry.TurnRate = rotation;
    }
}