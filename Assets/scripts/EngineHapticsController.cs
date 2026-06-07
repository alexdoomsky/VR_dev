using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using System.Collections;

public class EngineHapticsController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TankTelemetry telemetry;

    [Header("Controllers")]
    [SerializeField] private XRBaseInputInteractor leftHand;
    [SerializeField] private XRBaseInputInteractor rightHand;

    [Header("Idle Haptics")]
    [SerializeField] private float amplitude = 0.08f;
    [SerializeField] private float duration = 0.08f;
    [SerializeField] private float interval = 0.25f;

    [Header("Running modulation")]
    [SerializeField] private float rpmToAmplitude = 0.00005f;

    private Coroutine hapticsRoutine;
    private EngineState lastState;

    private void Update()
    {
        if (telemetry == null)
            return;

        EngineState state = telemetry.EngineState;

        if (state != lastState)
        {
            HandleStateChange(state);
            lastState = state;
        }
    }

    private void HandleStateChange(EngineState state)
    {
        switch (state)
        {
            case EngineState.Running:
                StartHaptics();
                break;

            case EngineState.Starting:
                StopHaptics();
                break;

            case EngineState.Stalled:
                StopHaptics();
                break;

            case EngineState.Off:
                StopHaptics();
                break;
        }
    }

    private void StartHaptics()
    {
        if (hapticsRoutine != null)
            StopCoroutine(hapticsRoutine);

        hapticsRoutine = StartCoroutine(HapticsLoop());
    }

    private void StopHaptics()
    {
        if (hapticsRoutine != null)
        {
            StopCoroutine(hapticsRoutine);
            hapticsRoutine = null;
        }
    }

    private IEnumerator HapticsLoop()
    {
        while (telemetry != null &&
            telemetry.EngineState == EngineState.Running)
        {
            float dynamicAmplitude =
            amplitude +
            telemetry.EngineRPM * rpmToAmplitude;

            SendHaptics(dynamicAmplitude, duration);

            yield return new WaitForSeconds(interval);
        }

        hapticsRoutine = null;
    }

    private void SendHaptics(float amp, float dur)
    {
        if (leftHand != null)
            leftHand.SendHapticImpulse(amp, dur);

        if (rightHand != null)
            rightHand.SendHapticImpulse(amp, dur);
    }
}
