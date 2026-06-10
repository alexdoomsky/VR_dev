using UnityEngine;
using UnityEngine.InputSystem;

// Управляет визуальной анимацией педали сцепления и передаёт её состояние в телеметрию
public class ClutchPedal : MonoBehaviour
{
    [Header("Input")]

    // Ссылка на действие Input System (например триггер контроллера)
    [SerializeField] private InputActionReference triggerAction;

    [Header("Reference")]

    // Ссылка на общий контейнер данных танка
    [SerializeField] private TankTelemetry telemetry;

    [Header("Settings")]

    // Максимальный угол поворота педали при полном нажатии
    [SerializeField] private float maxAngle = -25f;

    // Скорость сглаживания движения педали
    [SerializeField] private float smoothSpeed = 10f;

    // Текущий угол педали
    private float currentAngle;

    // Вызывается Unity каждый кадр
    private void Update()
    {
        // Проверка что все необходимые ссылки назначены
        if (telemetry == null || triggerAction == null)
            return;

        // action - объект InputAction
        //
        // ReadValue<float>()
        // Считывает текущее значение действия как число float
        // Обычно возвращает значение от 0 до 1
        float input = triggerAction.action.ReadValue<float>();

        // Сохраняет текущее положение сцепления в телеметрии
        telemetry.ClutchInput = input;

        // Mathf.Lerp(a, b, t)
        // Линейно интерполирует между двумя значениями
        //
        // При input = 0 вернётся 0
        // При input = 1 вернётся maxAngle
        //
        // Используется для перевода процента нажатия в угол педали
        float targetAngle = Mathf.Lerp(0f, maxAngle, input);

        // Плавно приближает текущий угол к целевому
        //
        // Time.deltaTime
        // Время между текущим и предыдущим кадром
        //
        // smoothSpeed определяет скорость движения педали
        currentAngle = Mathf.Lerp(
            currentAngle,
            targetAngle,
            Time.deltaTime * smoothSpeed
        );

        // Quaternion - специальный тип для хранения поворотов
        //
        // Quaternion.Euler(x,y,z)
        // Создаёт поворот из углов Эйлера
        //
        // Здесь педаль вращается только вокруг оси Z
        transform.localRotation = Quaternion.Euler(0f, 0f, currentAngle);
		// Создаёт поворот объекта по заданным углам
        // transform - Transform текущего объекта
        //
        // localRotation - локальный поворот относительно родителя
    }
}