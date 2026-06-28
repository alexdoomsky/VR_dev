using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Проверяет правильную последовательность запуска двигателя.
/// После успешного прохождения разрешает запуск двигателя.
/// </summary>
public class EngineStartSequence : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TankTelemetry telemetry;

    // Правильная последовательность кнопок
    private readonly List<TankButton> correctSequence = new()
    {
        TankButton.Fuel,
        TankButton.Air,
        TankButton.Ignition
    };

    // Индекс ожидаемой кнопки
    private int currentIndex;

    public bool SequenceCompleted =>
    telemetry != null &&
    telemetry.CanStartEngine;

    private void OnEnable()
    {
        TankEventBus.OnButtonPressed += OnButtonPressed;
    }

    private void OnDisable()
    {
        TankEventBus.OnButtonPressed -= OnButtonPressed;
    }

    private void OnButtonPressed(TankButton button)
    {
        if (telemetry == null)
            return;

        // После прохождения последовательности больше не реагируем
        // (стартер обрабатывается TankEngine)
        if (telemetry.CanStartEngine)
            return;

        Debug.Log($"Pressed: {button}");

        if (button != correctSequence[currentIndex])
        {
            Debug.Log("Wrong engine start sequence.");

            ResetSequence();
            return;
        }

        switch (button)
        {
            case TankButton.Fuel:
                telemetry.FuelEnabled = true;
                break;

            case TankButton.Air:
                telemetry.AirEnabled = true;
                break;

            case TankButton.Ignition:
                telemetry.IgnitionEnabled = true;
                break;
        }

        currentIndex++;

        if (currentIndex >= correctSequence.Count)
        {
            telemetry.CanStartEngine = true;

            Debug.Log("Engine start sequence completed.");
        }
    }

    /// <summary>
    /// Сбрасывает последовательность запуска.
    /// </summary>
    public void ResetSequence()
    {
        currentIndex = 0;

        telemetry.FuelEnabled = false;
        telemetry.AirEnabled = false;
        telemetry.IgnitionEnabled = false;

        telemetry.CanStartEngine = false;
    }
}
