using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Game.Vehicles.Tank
{
    /// <summary>
    /// Vertical channel the lever currently occupies. New channels can be appended
    /// for custom shift schemes; no runtime code branches on a specific gate value.
    /// </summary>
    public enum GearGate
    {
        Left,
        Center,
        Right
    }

    /// <summary>
    /// Gearbox state. Additional gears can be appended for custom schemes (4/5/6-speed)
    /// without touching the resolution logic, since gears are resolved through the
    /// serialized <see cref="gearleverinteractable.GateGearMapping"/> table.
    /// </summary>
    public enum Gear
    {
        Reverse,
        Neutral,
        First,
        Second,
        Third,
        Fourth,
        Fifth
    }

    /// <summary>
    /// Custom XR interactable that simulates the mechanical gear lever of a T-62 tank gearbox.
    ///
    /// Design constraints this class honours:
    /// - Never derives from XRGrabInteractable and never uses a physics Joint.
    /// - Never computes an angle from the hand position (no Mathf.Atan2, no absolute
    ///   hand-to-pivot mapping). All movement is driven by the translation delta between
    ///   the hand's current position and the position captured at the moment of grab.
    /// - Internal state is a single normalized Vector2 (<see cref="leverPosition"/>); no
    ///   angles or rotations are stored anywhere. The visual transform is a one-way
    ///   projection of that state and never feeds back into the logic.
    /// - Uses only ProcessInteractable/OnSelectEntered/OnSelectExited; no Update().
    /// </summary>
    [DisallowMultipleComponent]
    public class gearleverinteractable : XRBaseInteractable
    {
        /// <summary>
        /// Data-driven description of how a single gate resolves to a gear, given the
        /// lever's vertical position inside that gate. Edited from the Inspector so new
        /// shift schemes (more gears, a different gate layout) require no code changes.
        /// </summary>
        [Serializable]
        public struct GateGearMapping
        {
            public GearGate gate;
            public Gear topGear;
            public Gear bottomGear;
            [Tooltip("If true, the vertical center of this gate resolves to centerGear instead of top/bottom.")]
            public bool hasNeutralAtCenter;
            public Gear centerGear;
            [Tooltip("Extension point for a gate-release lockout (e.g. reverse). Currently always permissive.")]
            public bool bottomRequiresGateRelease;
        }

        // ----------------------------------------------------------------------------
        // Inspector configuration
        // ----------------------------------------------------------------------------

        [Header("Visual")]
        [SerializeField]
        [Tooltip("Visual handle whose local rotation is derived from leverPosition. Never read back into logic.")]
        private Transform handle;

        [Header("Hand Tracking")]
        [SerializeField]
        [Tooltip("Fixed reference frame (e.g. the lever base) the grabbing hand's position is measured against. Only ever used to produce a delta, never an absolute angle.")]
        private Transform trackingTransform;

        [Header("Sensitivity")]
        [SerializeField]
        [Tooltip("Linear sensitivity converting hand displacement (meters) into an equivalent angular displacement (degrees) before normalization. A scaling factor, not an angle computed from position.")]
        private float degreesPerMeter = 90f;

        [SerializeField]
        [Tooltip("Physical travel limit, in degrees, used to normalize vertical (in-gate) movement to the -1..1 range.")]
        private float maxForwardAngle = 20f;

        [SerializeField]
        [Tooltip("Physical travel limit, in degrees, used to normalize horizontal (gate-to-gate) movement to the -1..1 range.")]
        private float maxSideAngle = 15f;

        [Header("Gate System")]
        [SerializeField]
        [Tooltip("Vertical distance from center, normalized, within which the lever is considered neutral and gate switching is permitted.")]
        private float neutralWindow = 0.15f;

        [SerializeField]
        [Tooltip("Normalized horizontal distance between two adjacent gates' rest positions.")]
        private float gateSpacing = 1f;

        [SerializeField]
        [Tooltip("How far past the current gate's rest position the hand must move, while near neutral, before the lever switches to the neighbouring gate.")]
        private float gateSwitchThreshold = 0.6f;

        [Header("Dead Zones")]
        [SerializeField]
        [Tooltip("Hand displacement (meters) along the forward/back axis ignored before it affects the vertical position.")]
        private float forwardDeadZone = 0.005f;

        [SerializeField]
        [Tooltip("Hand displacement (meters) along the left/right axis ignored before it affects the horizontal position.")]
        private float sideDeadZone = 0.005f;

        [Header("Gear Resolution")]
        [SerializeField]
        [Tooltip("Per-gate mapping table. Populated with the default T-62 layout by Reset(); edit freely to support other schemes.")]
        private List<GateGearMapping> gateGearMappings = new List<GateGearMapping>();

        // ----------------------------------------------------------------------------
        // Constants (named to avoid magic numbers)
        // ----------------------------------------------------------------------------

        private const float k_NormalizedMin = -1f;
        private const float k_NormalizedMax = 1f;
        private const float k_MinAngleDenominator = 0.0001f;
        private const float k_MinRangeDenominator = 0.0001f;

        // ----------------------------------------------------------------------------
        // Runtime state
        // ----------------------------------------------------------------------------

        /// <summary>
        /// The only persisted spatial state of the lever. X is the horizontal (gate) offset,
        /// Y is the vertical position inside the current gate. No angles, no rotations.
        /// </summary>
        private Vector2 leverPosition;

        private bool isGrabbed;
        private IXRSelectInteractor selectingInteractor;

        /// <summary>Hand position, in trackingTransform local space, captured at the moment of grab.</summary>
        private Vector3 grabHandPosition;

        /// <summary>leverPosition captured at the moment of grab.</summary>
        private Vector2 grabLeverPosition;

        /// <summary>Normalized hand displacement since grab, computed once per frame by UpdateHandDelta().</summary>
        private Vector2 handDelta;

        private static readonly GearGate[] s_GateOrder = (GearGate[])Enum.GetValues(typeof(GearGate));
        private int minGateOffset;
        private int maxGateOffset;

        // ----------------------------------------------------------------------------
        // Public API
        // ----------------------------------------------------------------------------

        public Gear CurrentGear { get; private set; } = Gear.Neutral;
        public GearGate CurrentGate { get; private set; } = GearGate.Center;
        public Vector2 LeverPosition => leverPosition;

        /// <summary>Raised whenever the resolved gear changes. Extension point for sounds, haptics, UI, etc.</summary>
        public event Action<Gear> GearChanged;

        // ----------------------------------------------------------------------------
        // Unity / editor lifecycle
        // ----------------------------------------------------------------------------

        protected override void Reset()
        {
            base.Reset();
            gateGearMappings = BuildDefaultGateGearMappings();
        }

        protected override void Awake()
        {
            base.Awake();

            if (gateGearMappings == null || gateGearMappings.Count == 0)
                gateGearMappings = BuildDefaultGateGearMappings();

            CacheGateOffsetBounds();
        }

        private static List<GateGearMapping> BuildDefaultGateGearMappings()
        {
            return new List<GateGearMapping>
            {
                new GateGearMapping
                {
                    gate = GearGate.Left,
                    topGear = Gear.First,
                    bottomGear = Gear.Second,
                    hasNeutralAtCenter = false
                },
                new GateGearMapping
                {
                    gate = GearGate.Center,
                    topGear = Gear.Third,
                    bottomGear = Gear.Fourth,
                    hasNeutralAtCenter = true,
                    centerGear = Gear.Neutral
                },
                new GateGearMapping
                {
                    gate = GearGate.Right,
                    topGear = Gear.Fifth,
                    bottomGear = Gear.Reverse,
                    hasNeutralAtCenter = false,
                    bottomRequiresGateRelease = true
                }
            };
        }

        private void CacheGateOffsetBounds()
        {
            minGateOffset = int.MaxValue;
            maxGateOffset = int.MinValue;

            foreach (var gate in s_GateOrder)
            {
                var offset = GetGateOffset(gate);
                minGateOffset = Mathf.Min(minGateOffset, offset);
                maxGateOffset = Mathf.Max(maxGateOffset, offset);
            }
        }

        // ----------------------------------------------------------------------------
        // XRI selection events
        // ----------------------------------------------------------------------------

        protected override void OnSelectEntered(SelectEnterEventArgs args)
        {
            base.OnSelectEntered(args);

            // Only a single hand drives the lever at a time; ignore additional grabs.
            if (isGrabbed)
                return;

            selectingInteractor = args.interactorObject;
            isGrabbed = true;

            grabHandPosition = GetInteractorLocalPosition(selectingInteractor);
            grabLeverPosition = leverPosition;
        }

        protected override void OnSelectExited(SelectExitEventArgs args)
        {
            base.OnSelectExited(args);

            if (args.interactorObject != selectingInteractor)
                return;

            isGrabbed = false;
            selectingInteractor = null;
            handDelta = Vector2.zero;
        }

        public override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            base.ProcessInteractable(updatePhase);

            if (updatePhase != XRInteractionUpdateOrder.UpdatePhase.Dynamic)
                return;

            if (isGrabbed)
            {
                UpdateHandDelta();
                UpdateGate();
                UpdateLeverPosition();
                UpdateCurrentGear();
            }

            // Visual is recomputed every frame regardless of grab state so that future
            // additions (return spring, snap-to-gear) keep the handle in sync for free.
            UpdateVisual();
        }

        // ----------------------------------------------------------------------------
        // Core pipeline (one responsibility per method)
        // ----------------------------------------------------------------------------

        /// <summary>
        /// Computes the normalized hand displacement since grab. This is pure translation:
        /// current local hand position minus the position captured at grab time. No angle,
        /// no absolute position, is ever derived from this beyond the subtraction itself.
        /// </summary>
        private void UpdateHandDelta()
        {
            if (selectingInteractor == null)
            {
                handDelta = Vector2.zero;
                return;
            }

            var currentHandPosition = GetInteractorLocalPosition(selectingInteractor);
            var rawDelta = currentHandPosition - grabHandPosition;

            var sideMeters = ApplyDeadZone(rawDelta.x, sideDeadZone);
            var forwardMeters = ApplyDeadZone(rawDelta.z, forwardDeadZone);

            var sideAngleEquivalent = sideMeters * degreesPerMeter;
            var forwardAngleEquivalent = forwardMeters * degreesPerMeter;

            handDelta = new Vector2(
                sideAngleEquivalent / Mathf.Max(maxSideAngle, k_MinAngleDenominator),
                forwardAngleEquivalent / Mathf.Max(maxForwardAngle, k_MinAngleDenominator));
        }

        /// <summary>
        /// Decides whether a gate switch is allowed and performs it. Reads the lever's
        /// vertical position from *before* this frame's update, which is intentional:
        /// the gate must be settled before UpdateLeverPosition() computes this frame's
        /// authoritative position.
        /// </summary>
        private void UpdateGate()
        {
            if (!IsNearNeutral(leverPosition.y))
                return;

            var restX = GetGateRestPosition(CurrentGate);
            var desiredX = grabLeverPosition.x + handDelta.x;
            var offsetFromRest = desiredX - restX;

            if (Mathf.Abs(offsetFromRest) <= gateSwitchThreshold)
                return;

            var direction = offsetFromRest > 0f ? 1 : -1;
            var adjacentGate = GetAdjacentGate(CurrentGate, direction);

            if (adjacentGate.HasValue)
                CurrentGate = adjacentGate.Value;
        }

        /// <summary>
        /// Produces this frame's authoritative leverPosition. Horizontal movement is only
        /// applied while near neutral; otherwise X is locked to the current gate's channel.
        /// The lever always follows grabLeverPosition + delta - it never chases the hand
        /// incrementally frame to frame.
        /// </summary>
        private void UpdateLeverPosition()
        {
            float newX;

            if (IsNearNeutral(leverPosition.y))
            {
                var desiredX = grabLeverPosition.x + handDelta.x;
                newX = Mathf.Clamp(desiredX, minGateOffset * gateSpacing, maxGateOffset * gateSpacing);
            }
            else
            {
                newX = GetGateRestPosition(CurrentGate);
            }

            var newY = Mathf.Clamp(grabLeverPosition.y + handDelta.y, k_NormalizedMin, k_NormalizedMax);

            leverPosition = new Vector2(newX, newY);
        }

        /// <summary>
        /// Resolves CurrentGear purely from CurrentGate + leverPosition.y via the serialized
        /// mapping table. No branch in this method is specific to any gate or gear value.
        /// </summary>
        private void UpdateCurrentGear()
        {
            var mapping = GetMappingForGate(CurrentGate);
            var resolvedGear = ResolveGearFromGate(mapping, leverPosition.y);

            if (resolvedGear == CurrentGear)
                return;

            var previousGear = CurrentGear;
            CurrentGear = resolvedGear;

            OnGearChanged(previousGear, CurrentGear);
            GearChanged?.Invoke(CurrentGear);
        }

        /// <summary>
        /// Pure projection from logic state to visual rotation. Never writes back into
        /// leverPosition, CurrentGate or CurrentGear.
        /// </summary>
        private void UpdateVisual()
        {
            if (handle == null)
                return;

            var horizontalRange = Mathf.Max(GetMaxAbsoluteGateOffset() * gateSpacing, k_MinRangeDenominator);
            var normalizedX = Mathf.Clamp(leverPosition.x / horizontalRange, k_NormalizedMin, k_NormalizedMax);
            var normalizedY = Mathf.Clamp(leverPosition.y, k_NormalizedMin, k_NormalizedMax);

            var sideAngle = normalizedX * maxSideAngle;
            var forwardAngle = normalizedY * maxForwardAngle;

            handle.localRotation = Quaternion.Euler(forwardAngle, 0f, sideAngle);
        }

        // ----------------------------------------------------------------------------
        // Extension hooks (override in a derived class; base class needs no changes)
        // ----------------------------------------------------------------------------

        /// <summary>Called after CurrentGear changes. Override for sounds, haptics, etc.</summary>
        protected virtual void OnGearChanged(Gear previousGear, Gear newGear)
        {
        }

        /// <summary>
        /// Extension point for a reverse-gate release / lockout button. Returning true
        /// (the default) means no lockout is enforced; override to gate bottomGear entries
        /// marked bottomRequiresGateRelease behind a physical release input.
        /// </summary>
        protected virtual bool IsGateReleaseHeld()
        {
            return true;
        }

        /// <summary>
        /// Resolves a single gate's mapping entry to a gear for the given vertical position.
        /// Virtual so a derived class can implement custom resolution (e.g. snap zones)
        /// without modifying this class.
        /// </summary>
        protected virtual Gear ResolveGearFromGate(GateGearMapping mapping, float verticalPosition)
        {
            if (mapping.hasNeutralAtCenter && IsNearNeutral(verticalPosition))
                return mapping.centerGear;

            if (verticalPosition >= 0f)
                return mapping.topGear;

            if (mapping.bottomRequiresGateRelease && !IsGateReleaseHeld())
                return mapping.hasNeutralAtCenter ? mapping.centerGear : CurrentGear;

            return mapping.bottomGear;
        }

        /// <summary>Rest (locked) horizontal position of a gate, in normalized units. Virtual for custom layouts.</summary>
        protected virtual float GetGateRestPosition(GearGate gate)
        {
            return GetGateOffset(gate) * gateSpacing;
        }

        /// <summary>Returns the neighbouring gate one step away in the given direction, if any. Virtual for custom layouts.</summary>
        protected virtual GearGate? GetAdjacentGate(GearGate gate, int direction)
        {
            var targetOffset = GetGateOffset(gate) + direction;

            foreach (var candidate in s_GateOrder)
            {
                if (GetGateOffset(candidate) == targetOffset)
                    return candidate;
            }

            return null;
        }

        // ----------------------------------------------------------------------------
        // Internal helpers
        // ----------------------------------------------------------------------------

        private bool IsNearNeutral(float verticalPosition)
        {
            return Mathf.Abs(verticalPosition) < neutralWindow;
        }

        private Vector3 GetInteractorLocalPosition(IXRSelectInteractor interactor)
        {
            if (trackingTransform == null)
                return Vector3.zero;

            var attachTransform = interactor.GetAttachTransform(this);

            if (attachTransform == null)
                return Vector3.zero;

            // Relative-to-grab tracking only; this value is never used directly as an
            // absolute position or fed into an angle calculation - only ever subtracted
            // against the position captured at grab time.
            return trackingTransform.InverseTransformPoint(attachTransform.position);
        }

        private GateGearMapping GetMappingForGate(GearGate gate)
        {
            for (var i = 0; i < gateGearMappings.Count; i++)
            {
                if (gateGearMappings[i].gate == gate)
                    return gateGearMappings[i];
            }

            Debug.LogWarning($"gearleverinteractable: no GateGearMapping configured for gate '{gate}'.", this);
            return default;
        }

        /// <summary>
        /// Ordinal offset of a gate relative to GearGate.Center, derived from declaration
        /// order. This is the one place gate identity is referenced, and only to establish
        /// ordering - not to special-case behaviour.
        /// </summary>
        private static int GetGateOffset(GearGate gate)
        {
            var gateIndex = Array.IndexOf(s_GateOrder, gate);
            var centerIndex = Array.IndexOf(s_GateOrder, GearGate.Center);
            return gateIndex - centerIndex;
        }

        private int GetMaxAbsoluteGateOffset()
        {
            return Mathf.Max(maxGateOffset, Mathf.Abs(minGateOffset));
        }

        private static float ApplyDeadZone(float value, float deadZone)
        {
            var magnitude = Mathf.Abs(value);

            if (magnitude <= deadZone)
                return 0f;

            return Mathf.Sign(value) * (magnitude - deadZone);
        }
    }
}
