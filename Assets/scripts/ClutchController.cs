using UnityEngine;
using UnityEngine.InputSystem;

// Управляет сцеплением через Input System
public class ClutchController : MonoBehaviour
{
    [Header("Input")]

    // Ссылка на Input Action из новой Input System
	// Ссылка на действие из Unity Input System
	// Позволяет читать нажатия кнопок, триггеров и осей
    [SerializeField] private InputActionReference clutchAction;

    [Header("Smoothing")]

    // Скорость сглаживания изменения значения сцепления
    [SerializeField] private float responseSpeed = 8f;

    [Header("Debug inversion")]

    // Инвертирует входное значение (0->1, 1->0)
    [SerializeField] private bool invertInput = false;

    [Header("Output")]

    // Показывает значение в Inspector и ограничивает его диапазоном 0..1
    [Range(0f, 1f)]

    // Текущее положение сцепления
    // 0 = отпущено (двигатель связан с коробкой)
    // 1 = выжато (связь разорвана)
    public float ClutchValue;

    // Сырое значение напрямую из устройства ввода
    private float rawValue;

    // Вызывается Unity при включении объекта
    private void OnEnable()
    {
        // ?. - null conditional operator
        // Выполняет Enable() только если clutchAction не равен null

        // action - объект InputAction

        // Enable() начинает получать данные от Input System
        clutchAction?.action.Enable();
    }

    // Вызывается Unity при выключении объекта
    private void OnDisable()
    {
        // Disable() отключает получение ввода
        clutchAction?.action.Disable();
    }

    // Вызывается Unity каждый кадр
    private void Update()
    {
        // Проверка что ссылка на Input Action назначена
        if (clutchAction == null) return;

        // ReadValue<float>()
        // Считывает текущее значение действия как float
        // Обычно диапазон от 0 до 1 для триггера или оси
        rawValue = clutchAction.action.ReadValue<float>();

        if (invertInput)

            // Инвертирует значение:
            // 0 становится 1
            // 0.25 становится 0.75
            // 1 становится 0
            rawValue = 1f - rawValue;

        // Mathf.Lerp(a, b, t)
        // Линейно интерполирует между a и b
        // Используется для плавного изменения значения

        // Time.deltaTime
        // Время между текущим и предыдущим кадром

        // responseSpeed задаёт скорость реакции сцепления
        ClutchValue = Mathf.Lerp(
            ClutchValue,
            rawValue,
            Time.deltaTime * responseSpeed
        );
    }

    /// <summary>
    /// Насколько сцепление передаёт момент
    /// 1 = полностью сцеплено
    /// 0 = полностью выжато
    /// </summary>
    public float GetCoupling()
    {
        // Возвращает обратное значение сцепления
        // Используется коробкой передач и двигателем

        return 1f - ClutchValue;
    }

    /// <summary>
    /// Можно использовать для КПП / двигателя
    /// </summary>
    public bool IsDisengaged(float threshold = 0.9f)
    {
        // threshold = параметр по умолчанию
        // Если аргумент не передан, используется 0.9

        // Возвращает true если сцепление считается выжатым
        return ClutchValue >= threshold;
    }
}