using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class SingleAxisLeverInteractable : XRBaseInteractable
{
    public enum RotationAxis
    {
        X,
        Y,
        Z
    }

    [Header("References")]
    [SerializeField] private Transform handle;

    [Tooltip("Трансформ, положение которого будет использоваться для управления рычагом.\nОбычно сюда указывается объект Hand или пустой объект внутри него.")]
    [SerializeField] private Transform trackingTransform;

    [Header("Lever")]
    [SerializeField] private RotationAxis axis = RotationAxis.Z;

    [SerializeField] private float minAngle = -45f;
    [SerializeField] private float maxAngle = 45f;

    [Header("Tracking")]
    [SerializeField] private bool invert;

    private bool isGrabbed;

    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);
        isGrabbed = true;
    }

    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        base.OnSelectExited(args);
        isGrabbed = false;
    }

    public override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
    {
        base.ProcessInteractable(updatePhase);

        if (updatePhase != XRInteractionUpdateOrder.UpdatePhase.Dynamic)
            return;

        if (!isGrabbed)
            return;

        if (trackingTransform == null)
            return;

        UpdateLever();
    }

    private void UpdateLever()
    {
        Vector3 local = transform.InverseTransformPoint(trackingTransform.position);

        float angle;

        switch (axis)
        {
            case RotationAxis.X:
                angle = Mathf.Atan2(local.y, local.z) * Mathf.Rad2Deg;
                break;

            case RotationAxis.Y:
                angle = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
                break;

            default:
                angle = Mathf.Atan2(local.x, local.y) * Mathf.Rad2Deg;
                break;
        }

        if (invert)
            angle = -angle;

        angle = Mathf.Clamp(angle, minAngle, maxAngle);

        switch (axis)
        {
            case RotationAxis.X:
                handle.localRotation = Quaternion.Euler(angle, 0f, 0f);
                break;

            case RotationAxis.Y:
                handle.localRotation = Quaternion.Euler(0f, angle, 0f);
                break;

            case RotationAxis.Z:
                handle.localRotation = Quaternion.Euler(0f, 0f, angle);
                break;
        }
    }
}
