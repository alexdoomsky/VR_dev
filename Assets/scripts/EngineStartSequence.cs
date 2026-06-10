using System.Collections.Generic;
using UnityEngine;

// Проверяет правильную последовательность запуска двигателя
//DEPRECATED
public class EngineStartSequence : MonoBehaviour
{
    [SerializeField]

    // Ссылка на общую телеметрию танка
    private TankTelemetry telemetry;

    // readonly означает что ссылка на объект не может быть изменена после создания
    //
    // List<string> - список строк
    //
    // new() создаёт новый объект списка
    private readonly List<string> correctSequence = new()
    {
        "FuelButton",
        "AirButton",
        "IgnitionButton"
    };

    // Индекс текущего ожидаемого шага запуска
    private int currentIndex;

    // Свойство только для чтения (нет set)
    //
    // => это сокращённая запись return
    //
    // Возвращает true если последовательность завершена
    public bool SequenceCompleted =>
    telemetry != null &&
    telemetry.CanStartEngine;

    // Регистрирует нажатие кнопки запуска
    public void RegisterButtonPress(string buttonName)
    {
        // Проверка что телеметрия назначена
        if (telemetry == null)
        {
            // Вывод ошибки в Console Unity
            Debug.LogError("TankTelemetry not assigned");

            return;
        }

        // Вывод сообщения в Console Unity
        //
        // $ позволяет использовать интерполяцию строк
        Debug.Log($"Pressed: {buttonName}");

        // Проверяем соответствует ли нажатая кнопка ожидаемой
        //
        // currentIndex указывает какой шаг сейчас должен быть выполнен
        if (buttonName != correctSequence[currentIndex])
        {
            Debug.Log("WRONG ORDER");

            // Сброс последовательности при ошибке
            ResetSequence();

            return;
        }

        // switch выбирает действие по значению строки
        switch (buttonName)
        {
            case "FuelButton":

                // Разрешаем подачу топлива
                telemetry.FuelEnabled = true;
                break;

            case "AirButton":

                // Разрешаем подачу воздуха
                telemetry.AirEnabled = true;
                break;

            case "IgnitionButton":

                // Включаем зажигание
                telemetry.IgnitionEnabled = true;
                break;
        }

        // Переходим к следующему шагу последовательности
        currentIndex++;

        // Count возвращает количество элементов списка
        if (currentIndex >= correctSequence.Count)
        {
            // Разрешаем запуск двигателя
            telemetry.CanStartEngine = true;

            Debug.Log("START SEQUENCE COMPLETED");
        }
    }

    // Сбрасывает последовательность запуска в начальное состояние
    private void ResetSequence()
    {
        // Возвращаемся к первому шагу
        currentIndex = 0;

        // Отключаем все системы запуска
        telemetry.FuelEnabled = false;
        telemetry.AirEnabled = false;
        telemetry.IgnitionEnabled = false;

        // Запуск двигателя снова запрещён
        telemetry.CanStartEngine = false;
    }
}