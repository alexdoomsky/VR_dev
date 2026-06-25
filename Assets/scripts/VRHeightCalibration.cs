using UnityEngine;
using UnityEngine.InputSystem;

public class VRHeightCalibration : MonoBehaviour
{
    public enum CalibrationState
    {
        WaitingForSit,
        WaitingForStand,
        Calibrated
    }

    [Header("XR References")]
    [SerializeField] private Transform xrOrigin;
    [SerializeField] private Transform head;

    [Header("Tank Positions")]
    [SerializeField] private Transform seatAnchor;
    [SerializeField] private Transform hatchAnchor;

    [Header("Input")]
    [SerializeField] private InputActionReference calibrateSitAction;
    [SerializeField] private InputActionReference calibrateStandAction;

    [Header("UI")]
    [SerializeField] private GameObject sitCalibrationUI;
    [SerializeField] private GameObject standCalibrationUI;
    [SerializeField] private GameObject calibrationCompleteUI;

    [Header("Calibration")]
    [SerializeField] private float minimumHeightDifference = 0.25f;

    [Header("Movement")]
    [SerializeField] private float smoothSpeed = 5f;

    [Tooltip("Зона игнорирования около сидячего положения")]
    [SerializeField] private float sitDeadZone = 0.05f;

    [Tooltip("Зона игнорирования около стоячего положения")]
    [SerializeField] private float standDeadZone = 0.95f;

    [Header("Completion UI")]
    [SerializeField] private float completeMessageDuration = 3f;

    private CalibrationState state = CalibrationState.WaitingForSit;

    private float sitHeight;
    private float standHeight;

    private float completeTimer;

    private void OnEnable()
    {
        calibrateSitAction?.action.Enable();
        calibrateStandAction?.action.Enable();
    }

    private void OnDisable()
    {
        calibrateSitAction?.action.Disable();
        calibrateStandAction?.action.Disable();
    }

    private void Start()
    {
        UpdateUI();
    }

    private void Update()
    {
        HandleCalibrationInput();

        if (state == CalibrationState.Calibrated)
        {
            UpdatePlayerPosition();
            UpdateCompletionUI();
        }
    }

    private void HandleCalibrationInput()
    {
        switch (state)
        {
            case CalibrationState.WaitingForSit:

                if (calibrateSitAction != null &&
                    calibrateSitAction.action.WasPressedThisFrame())
                {
                    CaptureSitPosition();
                }

                break;

            case CalibrationState.WaitingForStand:

                if (calibrateStandAction != null &&
                    calibrateStandAction.action.WasPressedThisFrame())
                {
                    CaptureStandPosition();
                }

                break;

            case CalibrationState.Calibrated:
                break;
        }
    }

    private void CaptureSitPosition()
    {
        sitHeight = head.localPosition.y;

        AlignForward();

        state = CalibrationState.WaitingForStand;

        UpdateUI();

        Debug.Log($"Sit calibration complete. Height: {sitHeight:F2}");
    }

    private void CaptureStandPosition()
    {
        standHeight = head.localPosition.y;

        float difference = standHeight - sitHeight;

        if (difference < minimumHeightDifference)
        {
            Debug.LogWarning(
                $"Stand height too close to sit height. Difference = {difference:F2}");

            return;
        }

        state = CalibrationState.Calibrated;

        completeTimer = completeMessageDuration;

        UpdateUI();

        Debug.Log(
            $"Calibration finished. Sit={sitHeight:F2} Stand={standHeight:F2}");
    }

    private void AlignForward()
    {
        if (xrOrigin == null || head == null)
            return;

        Vector3 forward = head.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.001f)
            return;

        float yaw = Quaternion.LookRotation(forward).eulerAngles.y;

        xrOrigin.Rotate(
            Vector3.up,
            -yaw,
            Space.World
        );
    }

    private void UpdatePlayerPosition()
    {
        if (seatAnchor == null ||
            hatchAnchor == null ||
            head == null ||
            xrOrigin == null)
        {
            return;
        }

        float currentHeight = head.localPosition.y;

        float t = Mathf.InverseLerp(
            sitHeight,
            standHeight,
            currentHeight
        );

        t = Mathf.Clamp01(t);

        if (t < sitDeadZone)
            t = 0f;

        if (t > standDeadZone)
            t = 1f;

        float targetWorldY = Mathf.Lerp(
            seatAnchor.position.y,
            hatchAnchor.position.y,
            t
        );

        float desiredOriginY =
        targetWorldY - head.position.y;

        Vector3 originPos = xrOrigin.position;

        originPos.y = Mathf.Lerp(
            originPos.y,
            desiredOriginY,
            Time.deltaTime * smoothSpeed
        );

        xrOrigin.position = originPos;
    }

    private void UpdateCompletionUI()
    {
        if (calibrationCompleteUI == null)
            return;

        if (completeTimer <= 0f)
            return;

        completeTimer -= Time.deltaTime;

        if (completeTimer <= 0f)
        {
            calibrationCompleteUI.SetActive(false);
        }
    }

    private void UpdateUI()
    {
        if (sitCalibrationUI != null)
        {
            sitCalibrationUI.SetActive(
                state == CalibrationState.WaitingForSit);
        }

        if (standCalibrationUI != null)
        {
            standCalibrationUI.SetActive(
                state == CalibrationState.WaitingForStand);
        }

        if (calibrationCompleteUI != null)
        {
            calibrationCompleteUI.SetActive(
                state == CalibrationState.Calibrated);
        }
    }

    public bool IsCalibrated()
    {
        return state == CalibrationState.Calibrated;
    }

    public float GetNormalizedHeight()
    {
        if (state != CalibrationState.Calibrated)
            return 0f;

        return Mathf.Clamp01(
            Mathf.InverseLerp(
                sitHeight,
                standHeight,
                head.localPosition.y
            )
        );
    }
}
