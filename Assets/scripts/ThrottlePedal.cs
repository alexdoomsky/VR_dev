using UnityEngine;
using UnityEngine.InputSystem;

public class ThrottlePedal : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionReference triggerAction;

    [Header("Reference")]
    [SerializeField] private TankTelemetry telemetry;

    [Header("Settings")]
    [SerializeField] private float maxAngle = -25f;
    [SerializeField] private float smoothSpeed = 10f;
    [SerializeField] private bool invertInput;

    private float currentAngle;

    private void Update()
    {
        if (telemetry == null || triggerAction == null)
            return;

        float input = triggerAction.action.ReadValue<float>();

        if (invertInput)
            input = 1f - input;

        telemetry.ThrottleInput = input;

        float targetAngle = Mathf.Lerp(0f, maxAngle, input);

        currentAngle = Mathf.Lerp(
            currentAngle,
            targetAngle,
            Time.deltaTime * smoothSpeed
        );

        transform.localRotation = Quaternion.Euler(0f, 0f, currentAngle);
    }
}
