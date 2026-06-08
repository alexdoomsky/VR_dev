using UnityEngine;

public class BrakeLeverInput : MonoBehaviour
{
    [SerializeField] private TankTelemetry telemetry;

    [Header("Lever")]
    [SerializeField] private bool isLeftLever = true;

    [Header("Z Rotation")]
    [SerializeField] private float releasedAngle = 30f;
    [SerializeField] private float fullBrakeAngle = -10f;

    [Header("Debug")]
    [SerializeField]
    [Range(0f, 1f)]
    private float currentValue;

    private void Start()
    {
        Vector3 angles = transform.localEulerAngles;
        angles.z = releasedAngle;
        transform.localEulerAngles = angles;

        WriteBrakeValue(0f);
    }

    private void Update()
    {
        float zAngle = NormalizeAngle(transform.localEulerAngles.z);

        currentValue = Mathf.InverseLerp(
            releasedAngle,
            fullBrakeAngle,
            zAngle
        );

        currentValue = Mathf.Clamp01(currentValue);

        WriteBrakeValue(currentValue);
    }

    private void WriteBrakeValue(float value)
    {
        if (isLeftLever)
            telemetry.LeftBrakeInput = value;
        else
            telemetry.RightBrakeInput = value;
    }

    private float NormalizeAngle(float angle)
    {
        if (angle > 180f)
            angle -= 360f;

        return angle;
    }
}
