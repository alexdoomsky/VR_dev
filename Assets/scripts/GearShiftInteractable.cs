using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class GearShiftInteractable : XRBaseInteractable
{
    [Header("References")]
    [SerializeField] private Transform handle;
    [SerializeField] private Transform trackingTransform;

    [Header("Rotation Limits")]

    [Tooltip("Максимальный наклон вправо/влево (ось X контроллера -> вращение Z ручки)")]
    [SerializeField] private float maxSideAngle = 18f;

    [Tooltip("Максимальный наклон вперед/назад (ось Z контроллера -> вращение X ручки)")]
    [SerializeField] private float maxForwardAngle = 20f;

    [Header("Tracking")]

    [Tooltip("Сколько градусов соответствует одному метру движения руки.")]
    [SerializeField] private float degreesPerMeter = 350f;

    [SerializeField] private bool invertSide;
    [SerializeField] private bool invertForward;

    public float SideNormalized => currentSideAngle / maxSideAngle;
    public float ForwardNormalized => currentForwardAngle / maxForwardAngle;

    private bool isGrabbed;

    private Vector3 grabStartLocalHand;

    private float grabStartSideAngle;
    private float grabStartForwardAngle;

    private float currentSideAngle;
    private float currentForwardAngle;

    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);

        if (trackingTransform == null)
            return;

        isGrabbed = true;

        grabStartLocalHand = transform.InverseTransformPoint(trackingTransform.position);

        grabStartSideAngle = currentSideAngle;
        grabStartForwardAngle = currentForwardAngle;
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
        Vector3 currentLocalHand = transform.InverseTransformPoint(trackingTransform.position);

        Vector3 delta = currentLocalHand - grabStartLocalHand;

        float side = delta.x * degreesPerMeter;
        float forward = delta.z * degreesPerMeter;

        if (invertSide)
            side = -side;

        if (invertForward)
            forward = -forward;

        currentSideAngle = Mathf.Clamp(
            grabStartSideAngle + side,
            -maxSideAngle,
            maxSideAngle);

        currentForwardAngle = Mathf.Clamp(
            grabStartForwardAngle + forward,
            -maxForwardAngle,
            maxForwardAngle);

        handle.localRotation = Quaternion.Euler(
            currentForwardAngle,
            0f,
            currentSideAngle);
    }
}
