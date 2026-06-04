using System.Collections;
using UnityEngine;

public class EngineAudioController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EngineStartManager _engineManager;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource _startupSource;

    [SerializeField] private AudioSource _idleSource;

    [SerializeField] private AudioSource _shutdownSource;

    [Header("Settings")]
    [SerializeField] private float _startupDelay = 0.2f;

    [SerializeField] private float _idleVolume = 0.6f;

    private bool _wasStarted;

    private void Update()
    {
        if (_engineManager == null)
            return;

        // запуск двигателя
        if (_engineManager.EngineStarted && !_wasStarted)
        {
            _wasStarted = true;

            StartCoroutine(StartEngineSequence());
        }
    }

    private IEnumerator StartEngineSequence()
    {
        // проигрываем запуск
        if (_startupSource != null)
        {
            _startupSource.Play();

            yield return new WaitForSeconds(
                _startupSource.clip.length + _startupDelay
            );
        }

        // запускаем idle loop
        if (_idleSource != null)
        {
            _idleSource.volume = _idleVolume;

            _idleSource.loop = true;

            _idleSource.Play();
        }
    }

    public void StopEngine()
    {
        // стоп idle
        if (_idleSource != null)
        {
            _idleSource.Stop();
        }

        // звук остановки двигателя
        if (_shutdownSource != null)
        {
            _shutdownSource.Play();
        }

        _wasStarted = false;
    }
}
