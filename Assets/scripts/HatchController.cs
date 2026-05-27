using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit;

public class HatchController : MonoBehaviour
{
    [Header("XR")]
    [SerializeField] private XRGrabInteractable _grabInteractable;

    [Header("Angles")]
    [SerializeField] private float _closedAngle = 0f;

    [SerializeField] private float _openAngle = -60f;

    [Header("Thresholds")]
    [SerializeField] private float _openSnapThreshold = -50f;

    [SerializeField] private float _closeSnapThreshold = -10f;

    [Header("Speeds")]
    [SerializeField] private float _autoCloseSpeed = 2f;

    [SerializeField] private float _snapSpeed = 8f;

    private bool _isGrabbed;

    private void OnEnable()
    {
        if (_grabInteractable != null)
        {
            _grabInteractable.selectEntered.AddListener(OnGrab);
            _grabInteractable.selectExited.AddListener(OnRelease);
        }
    }

    private void OnDisable()
    {
        if (_grabInteractable != null)
        {
            _grabInteractable.selectEntered.RemoveListener(OnGrab);
            _grabInteractable.selectExited.RemoveListener(OnRelease);
        }
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        _isGrabbed = true;
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        _isGrabbed = false;
    }

    private void Update()
    {
        // Пока игрок держит люк —
        // ничего не делаем
        if (_isGrabbed)
            return;

        float currentAngle = NormalizeAngle(
            transform.localEulerAngles.z
        );

        float targetAngle = currentAngle;
        float speed = _autoCloseSpeed;

        // Доводчик открытия
        if (currentAngle <= _openSnapThreshold)
        {
            targetAngle = _openAngle;
            speed = _snapSpeed;
        }

        // Доводчик закрытия
        else if (currentAngle >= _closeSnapThreshold)
        {
            targetAngle = _closedAngle;
            speed = _snapSpeed;
        }

        // Автозакрытие из среднего положения
        else
        {
            targetAngle = _closedAngle;
            speed = _autoCloseSpeed;
        }

        float newAngle = Mathf.Lerp(
            currentAngle,
            targetAngle,
            Time.deltaTime * speed
        );

        transform.localRotation = Quaternion.Euler(
            0f,
            0f,
            newAngle
        );
    }

    private float NormalizeAngle(float angle)
    {
        if (angle > 180f)
            angle -= 360f;

        return angle;
    }
}
