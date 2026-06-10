using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit;

// Управляет открытием, закрытием и автодоводкой люка
//WIP
public class HatchController : MonoBehaviour
{
    [Header("XR")]

    // XR Grab Interactable позволяет хватать объект в VR
    [SerializeField] private XRGrabInteractable _grabInteractable;

    [Header("Angles")]

    // Угол полностью закрытого люка
    [SerializeField] private float _closedAngle = 0f;

    // Угол полностью открытого люка
    [SerializeField] private float _openAngle = -60f;

    [Header("Thresholds")]

    // Если игрок отпустил люк после этого угла,
    // люк автоматически откроется до конца
    [SerializeField] private float _openSnapThreshold = -50f;

    // Если игрок отпустил люк до этого угла,
    // люк автоматически закроется
    [SerializeField] private float _closeSnapThreshold = -10f;

    [Header("Speeds")]

    // Скорость обычного автозакрытия
    [SerializeField] private float _autoCloseSpeed = 2f;

    // Скорость доводчика
    [SerializeField] private float _snapSpeed = 8f;

    // Флаг удержания люка рукой
    private bool _isGrabbed;

    // Вызывается при включении объекта
    private void OnEnable()
    {
        if (_grabInteractable != null)
        {
            // selectEntered
            // Событие начала захвата объекта

            // AddListener()
            // Подписывает метод на событие
            _grabInteractable.selectEntered.AddListener(OnGrab);

            // selectExited
            // Событие отпускания объекта
            _grabInteractable.selectExited.AddListener(OnRelease);
        }
    }

    // Вызывается при выключении объекта
    private void OnDisable()
    {
        if (_grabInteractable != null)
        {
            // RemoveListener()
            // Удаляет подписку на событие
            _grabInteractable.selectEntered.RemoveListener(OnGrab);

            _grabInteractable.selectExited.RemoveListener(OnRelease);
        }
    }

    // Вызывается когда игрок берёт люк рукой
    private void OnGrab(SelectEnterEventArgs args)
    {
        // Помечаем что люк сейчас удерживается
        _isGrabbed = true;
    }

    // Вызывается когда игрок отпускает люк
    private void OnRelease(SelectExitEventArgs args)
    {
        // Помечаем что люк больше не удерживается
        _isGrabbed = false;
    }

    // Вызывается Unity каждый кадр
    private void Update()
    {
        // Пока игрок держит люк,
        // автоматическое управление отключено
        if (_isGrabbed)
            return;

        // Получаем текущий угол люка
        float currentAngle = NormalizeAngle(
            transform.localEulerAngles.z
        );

        // Целевой угол
        float targetAngle = currentAngle;

        // Скорость движения к цели
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

        // Промежуточное положение
        else
        {
            // Медленно закрываем люк
            targetAngle = _closedAngle;
            speed = _autoCloseSpeed;
        }

        // Mathf.Lerp()
        // Плавно интерполирует значение между двумя числами
        float newAngle = Mathf.Lerp(
            currentAngle,
            targetAngle,

            // Time.deltaTime
            // Время между кадрами
            Time.deltaTime * speed
        );

        // Quaternion.Euler()
        // Создаёт поворот из углов Эйлера
        transform.localRotation = Quaternion.Euler(
            0f,
            0f,
            newAngle
        );
    }

    // Переводит угол из диапазона 0..360 в -180..180
    private float NormalizeAngle(float angle)
    {
        if (angle > 180f)
            angle -= 360f;

        return angle;
    }
}