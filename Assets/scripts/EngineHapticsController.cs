using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using System.Collections;

public class EngineHapticsController : MonoBehaviour
{
    [Header("Engine")]
    [SerializeField] private EngineStartManager _engineManager;

    [Header("Controllers")]
    [SerializeField] private XRBaseInputInteractor _leftHand;

    [SerializeField] private XRBaseInputInteractor _rightHand;

    [Header("Idle Vibration")]
    [SerializeField] private float _amplitude = 0.08f;

    [SerializeField] private float _duration = 0.08f;

    [SerializeField] private float _interval = 0.25f;

    private Coroutine _engineRoutine;

    private bool _wasStarted;

    private void Update()
    {
        if (_engineManager == null)
            return;

        // двигатель только что запустился
        if (_engineManager.EngineStarted && !_wasStarted)
        {
            _wasStarted = true;

            _engineRoutine = StartCoroutine(
                EngineIdleHaptics()
            );
        }

        //  engine stop
    }

    private IEnumerator EngineIdleHaptics()
    {
        while (true)
        {
            SendHaptics();

            yield return new WaitForSeconds(_interval);
        }
    }

    private void SendHaptics()
    {
        if (_leftHand != null)
        {
            _leftHand.SendHapticImpulse(
                _amplitude,
                _duration
            );
        }

        if (_rightHand != null)
        {
            _rightHand.SendHapticImpulse(
                _amplitude,
                _duration
            );
        }
    }
}
