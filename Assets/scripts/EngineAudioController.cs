using System.Collections;
using UnityEngine;

// Управляет звуками запуска, работы и остановки двигателя
public class EngineAudioController : MonoBehaviour
{
    [Header("References")]

    // Ссылка на телеметрию танка
    [SerializeField] private TankTelemetry telemetry;

    [Header("Audio Sources")]

    // Источник звука запуска двигателя
    [SerializeField] private AudioSource startupSource;

    // Источник звука работающего двигателя
    [SerializeField] private AudioSource idleSource;

    // Источник звука остановки двигателя
    [SerializeField] private AudioSource shutdownSource;

    [Header("Settings")]

    // Дополнительная задержка после запуска
    [SerializeField] private float startupDelay = 0.2f;

    // Громкость работающего двигателя
    [SerializeField] private float idleVolume = 0.6f;

    // Предыдущее состояние двигателя
    private EngineState lastState;

    // Ссылка на запущенную корутину запуска
    private Coroutine startupRoutine;

    // Вызывается Unity каждый кадр
    private void Update()
    {
        // Проверка наличия телеметрии
        if (telemetry == null)
            return;

        // var автоматически выводит тип переменной
        // Здесь тип будет EngineState
        var state = telemetry.EngineState;

        // Проверка смены состояния двигателя
        if (state != lastState)
        {
            HandleStateChange(state);

            lastState = state;
        }

        // Если двигатель работает
        if (state == EngineState.Running && idleSource != null)
        {
            // volume - громкость AudioSource от 0 до 1
            idleSource.volume = idleVolume;
        }
    }

    // Выполняет действия при смене состояния двигателя
    private void HandleStateChange(EngineState state)
    {
        // switch выбирает ветку выполнения по значению переменной
        switch (state)
        {
            case EngineState.Starting:

                // Начало запуска двигателя
                StartStartup();
                break;

            case EngineState.Running:

                // Переход к работе на холостом ходу
                StartIdle();
                break;

            case EngineState.Stalled:

                // Воспроизведение остановки двигателя
                PlayShutdown();
                break;

            case EngineState.Off:

                // Полное отключение звуков
                StopAllAudio();
                break;
        }
    }

    // Запускает корутину старта двигателя
    private void StartStartup()
    {
        if (startupRoutine != null)

            // Останавливает ранее запущенную корутину
            StopCoroutine(startupRoutine);

        // StartCoroutine запускает выполнение IEnumerator во времени
        startupRoutine = StartCoroutine(StartupSequence());
    }

    // Корутина последовательности запуска двигателя
    private IEnumerator StartupSequence()
    {
        if (startupSource != null)
        {
            // Play() запускает воспроизведение AudioSource
            startupSource.Play();

            // WaitForSeconds создаёт задержку
            // yield return приостанавливает корутину на указанное время
            yield return new WaitForSeconds(
                startupSource.clip != null
                ? startupSource.clip.length + startupDelay
                : startupDelay
            );

            // ?: тернарный оператор
            // Если clip существует:
            // длина звука + задержка
            // иначе только задержка
        }

        // Проверка что двигатель всё ещё запускается
        if (telemetry.EngineState == EngineState.Starting)
        {
            // Перевод двигателя в рабочее состояние
            telemetry.EngineState = EngineState.Running;
        }
    }

    // Запускает звук работающего двигателя
    private void StartIdle()
    {
        if (idleSource == null)
            return;

        // isPlaying показывает воспроизводится ли звук
        if (!idleSource.isPlaying)
        {
            // loop заставляет звук повторяться бесконечно
            idleSource.loop = true;

            idleSource.volume = idleVolume;

            idleSource.Play();
        }
    }

    // Воспроизводит звук остановки двигателя
    private void PlayShutdown()
    {
        if (idleSource != null)

            // Stop() останавливает воспроизведение
            idleSource.Stop();

        if (shutdownSource != null)

            // Воспроизводит звук остановки
            shutdownSource.Play();
    }

    // Полностью отключает все звуки двигателя
    private void StopAllAudio()
    {
        if (idleSource != null)
            idleSource.Stop();

        if (startupSource != null)
            startupSource.Stop();
    }
}