using System.Collections;
using UnityEngine;

public class EngineAudioController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TankTelemetry telemetry;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource startupSource;
    [SerializeField] private AudioSource idleSource;
    [SerializeField] private AudioSource shutdownSource;

    [Header("Settings")]
    [SerializeField] private float startupDelay = 0.2f;
    [SerializeField] private float idleVolume = 0.6f;

    private EngineState lastState;
    private Coroutine startupRoutine;

    private void Update()
    {
        if (telemetry == null)
            return;

        var state = telemetry.EngineState;

        if (state != lastState)
        {
            HandleStateChange(state);
            lastState = state;
        }

        // динамика idle можно позже привязать к RPM
        if (state == EngineState.Running && idleSource != null)
        {
            idleSource.volume = idleVolume;
        }
    }

    private void HandleStateChange(EngineState state)
    {
        switch (state)
        {
            case EngineState.Starting:
                StartStartup();
                break;

            case EngineState.Running:
                StartIdle();
                break;

            case EngineState.Stalled:
                PlayShutdown();
                break;

            case EngineState.Off:
                StopAllAudio();
                break;
        }
    }

    private void StartStartup()
    {
        if (startupRoutine != null)
            StopCoroutine(startupRoutine);

        startupRoutine = StartCoroutine(StartupSequence());
    }

    private IEnumerator StartupSequence()
    {
        if (startupSource != null)
        {
            startupSource.Play();

            yield return new WaitForSeconds(
                startupSource.clip != null
                ? startupSource.clip.length + startupDelay
                : startupDelay
            );
        }

        // если за время старта двигатель не умер
        if (telemetry.EngineState == EngineState.Starting)
        {
            telemetry.EngineState = EngineState.Running;
        }
    }

    private void StartIdle()
    {
        if (idleSource == null)
            return;

        if (!idleSource.isPlaying)
        {
            idleSource.loop = true;
            idleSource.volume = idleVolume;
            idleSource.Play();
        }
    }

    private void PlayShutdown()
    {
        if (idleSource != null)
            idleSource.Stop();

        if (shutdownSource != null)
            shutdownSource.Play();
    }

    private void StopAllAudio()
    {
        if (idleSource != null)
            idleSource.Stop();

        if (startupSource != null)
            startupSource.Stop();
    }
}
