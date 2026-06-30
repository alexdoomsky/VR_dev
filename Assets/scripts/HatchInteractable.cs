using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Game.Vehicles.Tank
{
    /// <summary>
    /// Custom XR interactable that simulates a tank hatch hinging around a single axis.
    ///
    /// Design constraints this class honours:
    /// - Never derives from XRGrabInteractable, never uses a Joint/ConfigurableJoint/
    ///   SpringJoint, and never uses a Rigidbody to drive the rotation.
    /// - Never computes an angle from the hand position (no Mathf.Atan2, no controller
    ///   world axes feeding the angle). Movement is driven purely by projecting the
    ///   hand's per-frame translation delta onto the hinge's tangent direction.
    /// - The delta is accumulated frame-to-frame (current hand position minus the
    ///   *previous frame's* hand position), not a snapshot-from-grab delta. The
    ///   reference point advances every frame - even while currentAngle is clamped at
    ///   a limit - so reversing direction responds immediately instead of first
    ///   "unwinding" an error that built up while pinned against the limit.
    /// - Internal state is a single float, currentAngle; no rotation or Transform state
    ///   is ever stored or read back for logic. The visual transform is a one-way
    ///   projection of that state.
    /// - Uses only ProcessInteractable/OnSelectEntered/OnSelectExited; no Update().
    /// - Does not implement auto-snap; that is left to a separate component, by design.
    /// </summary>
    [DisallowMultipleComponent]
    public class HatchInteractable : XRBaseInteractable
    {
        // ----------------------------------------------------------------------------
        // Inspector configuration
        // ----------------------------------------------------------------------------

        [Header("Visual")]
        [SerializeField]
        [Tooltip("Visual handle whose local rotation is derived from currentAngle. Never read back into logic.")]
        private Transform handle;

        [Header("Hinge Geometry")]
        [SerializeField]
        [Tooltip("Pivot point the hatch rotates around.")]
        private Transform hinge;

        [SerializeField]
        [Tooltip("Local axis of the hinge that the hatch rotates around (e.g. Vector3.right or Vector3.forward).")]
        private Vector3 rotationAxis = Vector3.right;

        [Header("Hand Tracking")]
        [SerializeField]
        [Tooltip("Fixed reference frame (e.g. the vehicle hull) all hand and hinge positions are measured against, so that vehicle motion never bleeds into the hand delta.")]
        private Transform trackingTransform;

        [Header("Sensitivity")]
        [SerializeField]
        [Tooltip("Degrees of hatch rotation per meter of tangential hand movement.")]
        private float degreesPerMeter = 250f;

        [Header("Limits")]
        [SerializeField]
        private float minAngle = 0f;

        [SerializeField]
        private float maxAngle = 90f;

        [Header("Speed Sensitivity Curve")]
        [SerializeField]
        [Tooltip("Maps normalized hand speed (0..1, see speedNormalizationReference) to a sensitivity multiplier applied on top of degreesPerMeter. Edit this curve to make the hatch feel heavier or lighter at different hand speeds without touching the interaction math. A flat curve at 1 reproduces the original behaviour.")]
        private AnimationCurve sensitivityCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);

        [SerializeField]
        [Tooltip("Hand speed, in meters/second, that maps to x = 1 on sensitivityCurve.")]
        private float speedNormalizationReference = 1.5f;

        // ----------------------------------------------------------------------------
        // Constants (named to avoid magic numbers)
        // ----------------------------------------------------------------------------

        private const float k_DefaultSensitivity = 1f;
        private const float k_MinDeltaTime = 0.0001f;
        private const float k_MinSpeedDenominator = 0.0001f;

        // ----------------------------------------------------------------------------
        // Runtime state
        // ----------------------------------------------------------------------------

        private IXRSelectInteractor selectingInteractor;

        /// <summary>Hand position, in trackingTransform local space, captured the previous frame (or at grab).</summary>
        private Vector3 previousHandPosition;

        /// <summary>Hand position, in trackingTransform local space, for the current frame.</summary>
        private Vector3 currentHandPosition;

        /// <summary>This frame's accumulated hand displacement: currentHandPosition - previousHandPosition.</summary>
        private Vector3 handDelta;

        // ----------------------------------------------------------------------------
        // Public API
        // ----------------------------------------------------------------------------

        /// <summary>
        /// The only persisted spatial state of the hatch. No rotation, quaternion or
        /// Transform is ever stored elsewhere; the visual is a pure projection of this.
        /// </summary>
        public float CurrentAngle { get; private set; }

        public bool IsGrabbed { get; private set; }

        // ----------------------------------------------------------------------------
        // Unity lifecycle
        // ----------------------------------------------------------------------------

        protected override void Awake()
        {
            base.Awake();

            if (hinge == null)
                Debug.LogWarning("HatchInteractable: hinge is not assigned.", this);

            if (trackingTransform == null)
                Debug.LogWarning("HatchInteractable: trackingTransform is not assigned.", this);
        }

        // ----------------------------------------------------------------------------
        // XRI selection events
        // ----------------------------------------------------------------------------

        protected override void OnSelectEntered(SelectEnterEventArgs args)
        {
            base.OnSelectEntered(args);

            // Only a single hand drives the hatch at a time; ignore additional grabs.
            if (IsGrabbed)
                return;

            selectingInteractor = args.interactorObject;
            IsGrabbed = true;

            previousHandPosition = GetHandPositionInTrackingSpace(selectingInteractor);
        }

        protected override void OnSelectExited(SelectExitEventArgs args)
        {
            base.OnSelectExited(args);

            if (args.interactorObject != selectingInteractor)
                return;

            IsGrabbed = false;
            selectingInteractor = null;
            handDelta = Vector3.zero;
        }

        public override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            base.ProcessInteractable(updatePhase);

            if (updatePhase != XRInteractionUpdateOrder.UpdatePhase.Dynamic)
                return;

            if (IsGrabbed)
            {
                UpdateHandDelta();

                var tangent = CalculateTangent();
                var movement = CalculateMovement(tangent);
                var angleDelta = CalculateAngleDelta(movement);

                UpdateAngle(angleDelta);
            }

            // Visual is recomputed every frame regardless of grab state, so an external
            // auto-snap component (or any future system) that changes currentAngle keeps
            // the handle in sync without this class needing to know about it.
            ApplyVisual();
        }

        // ----------------------------------------------------------------------------
        // Core pipeline (one responsibility per method)
        // ----------------------------------------------------------------------------

        /// <summary>
        /// Computes this frame's accumulated hand displacement: the current hand position
        /// minus the position recorded last frame, never an absolute position used on its
        /// own. previousHandPosition is advanced unconditionally here, even while
        /// currentAngle ends up clamped later in the pipeline, so that a later reversal of
        /// direction is felt immediately rather than first cancelling out a stale error.
        /// </summary>
        private void UpdateHandDelta()
        {
            if (selectingInteractor == null)
            {
                handDelta = Vector3.zero;
                return;
            }

            currentHandPosition = GetHandPositionInTrackingSpace(selectingInteractor);
            handDelta = currentHandPosition - previousHandPosition;

            previousHandPosition = currentHandPosition;
        }

        /// <summary>
        /// Builds the tangent direction of the hinge's arc at the hand's current radius.
        /// Any component of hand movement along the radius (towards/away from the hinge)
        /// is implicitly excluded later by the dot product against this tangent.
        /// </summary>
        private Vector3 CalculateTangent()
        {
            var radius = CalculateRadius();
            var axis = GetTrackingLocalRotationAxis();

            return Vector3.Cross(axis, radius).normalized;
        }

        /// <summary>Vector from the hinge pivot to the hand, in trackingTransform local space.</summary>
        private Vector3 CalculateRadius()
        {
            return currentHandPosition - GetHingePositionInTrackingSpace();
        }

        /// <summary>Tangential component of this frame's hand delta; radial movement contributes nothing.</summary>
        private float CalculateMovement(Vector3 tangent)
        {
            return Vector3.Dot(handDelta, tangent);
        }

        /// <summary>
        /// Converts tangential movement (meters) into an angle change (degrees), scaled by
        /// degreesPerMeter and modulated by sensitivityCurve. The curve is keyed by
        /// normalized hand speed, so editing it changes how the hatch *feels* at different
        /// hand speeds without touching any of the geometry above.
        /// </summary>
        private float CalculateAngleDelta(float movement)
        {
            return movement * degreesPerMeter * EvaluateSpeedSensitivity(movement);
        }

        /// <summary>
        /// delta движения -> нормализованная скорость руки -> AnimationCurve.Evaluate() -> коэффициент чувствительности.
        /// </summary>
        private float EvaluateSpeedSensitivity(float movement)
        {
            if (sensitivityCurve == null)
                return k_DefaultSensitivity;

            var deltaTime = Mathf.Max(Time.deltaTime, k_MinDeltaTime);
            var handSpeed = Mathf.Abs(movement) / deltaTime;
            var normalizedSpeed = Mathf.Clamp01(handSpeed / Mathf.Max(speedNormalizationReference, k_MinSpeedDenominator));

            return sensitivityCurve.Evaluate(normalizedSpeed);
        }

        /// <summary>Applies the angle change and clamps to the configured travel limits.</summary>
        private void UpdateAngle(float angleDelta)
        {
            CurrentAngle = Mathf.Clamp(CurrentAngle + angleDelta, minAngle, maxAngle);
        }

        /// <summary>
        /// Pure projection from currentAngle to visual rotation. Never writes back into
        /// currentAngle and is never read by any logic method above.
        /// </summary>
        private void ApplyVisual()
        {
            if (handle == null)
                return;

            handle.localRotation = Quaternion.Euler(rotationAxis.normalized * CurrentAngle);
        }

        // ----------------------------------------------------------------------------
        // Internal helpers
        // ----------------------------------------------------------------------------

        /// <summary>
        /// Hand attach position in trackingTransform local space. Used only to produce a
        /// delta against previousHandPosition - never read as an absolute coordinate that
        /// feeds the angle directly.
        /// </summary>
        private Vector3 GetHandPositionInTrackingSpace(IXRSelectInteractor interactor)
        {
            if (trackingTransform == null || interactor == null)
                return Vector3.zero;

            var attachTransform = interactor.GetAttachTransform(this);

            if (attachTransform == null)
                return Vector3.zero;

            return trackingTransform.InverseTransformPoint(attachTransform.position);
        }

        private Vector3 GetHingePositionInTrackingSpace()
        {
            if (trackingTransform == null || hinge == null)
                return Vector3.zero;

            return trackingTransform.InverseTransformPoint(hinge.position);
        }

        /// <summary>
        /// rotationAxis is local to the hinge; this brings it into the same trackingTransform
        /// local space as the radius and hand delta vectors so the cross/dot products below
        /// are taken consistently in one frame of reference, immune to the vehicle moving.
        /// </summary>
        private Vector3 GetTrackingLocalRotationAxis()
        {
            if (trackingTransform == null || hinge == null)
                return rotationAxis.normalized;

            var worldAxis = hinge.TransformDirection(rotationAxis.normalized);
            return trackingTransform.InverseTransformDirection(worldAxis);
        }
    }
}
