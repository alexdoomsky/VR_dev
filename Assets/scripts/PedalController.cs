using UnityEngine;
using UnityEngine.InputSystem;

public class PedalController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionReference _triggerAction;

    [Header("Pedal Settings")]
    [SerializeField] private float _maxAngle = -25f;

    [SerializeField] private float _smoothSpeed = 10f;

    [SerializeField] private bool _invertInput = false;

    private float _currentAngle;

    private void Update()
    {
        if (_triggerAction == null)
            return;

        // Считываем значение триггера
        float inputValue = _triggerAction.action.ReadValue<float>();

        // Инверсия если понадобится
        if (_invertInput)
            inputValue = 1f - inputValue;

        // Вычисляем целевой угол
        float targetAngle = Mathf.Lerp(0f, _maxAngle, inputValue);

        // Плавное движение
        _currentAngle = Mathf.Lerp(
            _currentAngle,
            targetAngle,
            Time.deltaTime * _smoothSpeed
        );

        // Применяем поворот
        transform.localRotation = Quaternion.Euler(
            0f,
            0f,
            _currentAngle
        );
    }
}
