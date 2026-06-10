using UnityEngine;
using UnityEngine.InputSystem;

// Управляет визуальной анимацией педали газа и передаёт её состояние в телеметрию
public class ThrottlePedal : MonoBehaviour
{
    [Header("Input")]

    // Ссылка на действие Input System (обычно триггер контроллера)
    [SerializeField] private InputActionReference triggerAction;

    [Header("Reference")]

    // Ссылка на общий контейнер данных танка
    [SerializeField] private TankTelemetry telemetry;

    [Header("Settings")]

    // Максимальный угол поворота педали при полном нажатии
    [SerializeField] private float maxAngle = -25f;

    // Скорость сглаживания движения педали
    [SerializeField] private float smoothSpeed = 10f;

    // Инвертирует входное значение при необходимости
    [SerializeField] private bool invertInput;

    // Текущий угол педали
    private float currentAngle;

    // Вызывается Unity каждый кадр
    private void Update()
    {
        // Проверка что все необходимые ссылки назначены
        if (telemetry == null || triggerAction == null)
            return;

        // ReadValue<float>()
        // Считывает текущее значение действия как число float
        // Обычно возвращает значение от 0 до 1
        float input = triggerAction.action.ReadValue<float>();

        if (invertInput)

            // Инвертирует значение:
            // 0 -> 1
            // 1 -> 0
            input = 1f - input;

        // Передаёт текущее положение педали газа в телеметрию
        telemetry.ThrottleInput = input;

        // Mathf.Lerp(a,b,t)
        // Линейно интерполирует между двумя значениями
        //
        // input = 0 -> угол 0°
        // input = 1 -> угол maxAngle
        float targetAngle = Mathf.Lerp(0f, maxAngle, input);

        // Плавно приближает текущий угол к целевому
        currentAngle = Mathf.Lerp(
            currentAngle,
            targetAngle,

            // Time.deltaTime
            // Время между текущим и предыдущим кадром
            Time.deltaTime * smoothSpeed
        );

        // Quaternion.Euler(x,y,z)
        // Создаёт поворот из углов Эйлера
        //
        // Здесь вращаем педаль только по оси Z
        transform.localRotation = Quaternion.Euler(0f, 0f, currentAngle);

        // transform.localRotation
        // Локальный поворот объекта относительно родителя
    }
}