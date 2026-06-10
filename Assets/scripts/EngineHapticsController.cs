using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using System.Collections;

// Управляет вибрацией VR-контроллеров в зависимости от состояния двигателя
//WIP
public class EngineHapticsController : MonoBehaviour
{
    [Header("References")]

    // Ссылка на телеметрию танка
    [SerializeField] private TankTelemetry telemetry;

    [Header("Controllers")]

    // Левый VR-контроллер
    [SerializeField] private XRBaseInputInteractor leftHand;

    // Правый VR-контроллер
    [SerializeField] private XRBaseInputInteractor rightHand;

    [Header("Idle Haptics")]

    // Базовая сила вибрации
    [SerializeField] private float amplitude = 0.08f;

    // Длительность одного импульса вибрации
    [SerializeField] private float duration = 0.08f;

    // Интервал между импульсами
    [SerializeField] private float interval = 0.25f;

    [Header("Running modulation")]

    // Коэффициент увеличения вибрации от оборотов двигателя
    [SerializeField] private float rpmToAmplitude = 0.00005f;

    // Ссылка на запущенную корутину вибрации
    private Coroutine hapticsRoutine;

    // Предыдущее состояние двигателя
    private EngineState lastState;

    // Вызывается Unity каждый кадр
    private void Update()
    {
        // Проверка наличия телеметрии
		//Чтобы избежать NullReferenceException при обращении к несуществующему объекту.
        if (telemetry == null)
            return;

        // Получение текущего состояния двигателя
        EngineState state = telemetry.EngineState;

        // Реакция только на изменение состояния
        if (state != lastState)
        {
            HandleStateChange(state);

            lastState = state;
        }
    }

    // Обрабатывает смену состояния двигателя
    private void HandleStateChange(EngineState state)
    {
        // switch выбирает действие по значению переменной
        switch (state)
        {
            case EngineState.Running:

                // При работающем двигателе запускаем вибрацию
                StartHaptics();
                break;

            case EngineState.Starting:

                // Во время запуска вибрацию отключаем
                StopHaptics();
                break;

            case EngineState.Stalled:

                // При заглохшем двигателе вибрацию отключаем
                StopHaptics();
                break;

            case EngineState.Off:

                // При выключенном двигателе вибрацию отключаем
                StopHaptics();
                break;
        }
    }

    // Запускает цикл вибрации
    private void StartHaptics()
    {
        if (hapticsRoutine != null)

            // Останавливает предыдущую корутину
            StopCoroutine(hapticsRoutine);

        // StartCoroutine запускает выполнение IEnumerator
        hapticsRoutine = StartCoroutine(HapticsLoop());
    }

    // Останавливает цикл вибрации
    private void StopHaptics()
    {
        if (hapticsRoutine != null)
        {
            // Принудительная остановка корутины
            StopCoroutine(hapticsRoutine);

            hapticsRoutine = null;
        }
    }

    // Корутина циклической вибрации
    private IEnumerator HapticsLoop()
    {
        // Выполняется пока двигатель работает
        while (telemetry != null &&
            telemetry.EngineState == EngineState.Running)
        {
            // Вычисление силы вибрации

            // EngineRPM - текущие обороты двигателя

            // Чем выше обороты,
            // тем сильнее вибрация контроллера
            float dynamicAmplitude =
            amplitude +
            telemetry.EngineRPM * rpmToAmplitude;

            // Отправка вибрации на контроллеры
            SendHaptics(dynamicAmplitude, duration);

            // Пауза между импульсами вибрации
            yield return new WaitForSeconds(interval);
        }

        // Помечаем что корутина завершилась
        hapticsRoutine = null;
    }

    // Отправляет вибрацию на оба контроллера
    private void SendHaptics(float amp, float dur)
    {
        if (leftHand != null)

            // SendHapticImpulse()
            // Отправляет импульс вибрации контроллеру
            leftHand.SendHapticImpulse(amp, dur);

        if (rightHand != null)

            rightHand.SendHapticImpulse(amp, dur);
    }
}