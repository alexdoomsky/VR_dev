using UnityEngine;

// Считывает положение тормозного рычага и записывает силу торможения в телеметрию
public class BrakeLeverInput : MonoBehaviour
{
    // Ссылка на общий контейнер данных танка
    [SerializeField] private TankTelemetry telemetry;

    [Header("Lever")]
    // Определяет какой рычаг управляется: левый или правый
    [SerializeField] private bool isLeftLever = true;

    [Header("Z Rotation")]
    // Угол отпущенного тормоза
    [SerializeField] private float releasedAngle = 30f;

    // Угол полностью затянутого тормоза
    [SerializeField] private float fullBrakeAngle = -10f;

    [Header("Debug")]
    [SerializeField]

    // Ограничивает значение в инспекторе диапазоном 0..1
    [Range(0f, 1f)]
    private float currentValue;

    // Вызывается Unity каждый кадр
    private void Update()
    {
        // transform - компонент Transform текущего объекта
        // localEulerAngles - локальные углы объекта относительно родителя
        // .z - угол вокруг оси Z
        float zAngle = NormalizeAngle(transform.localEulerAngles.z);

        // Mathf.InverseLerp:
        // переводит значение из произвольного диапазона в диапазон 0..1
        // releasedAngle -> 0
        // fullBrakeAngle -> 1
        currentValue = Mathf.InverseLerp(
            releasedAngle,
            fullBrakeAngle,
            zAngle
        );

        // Mathf.Clamp01:
        // ограничивает значение диапазоном от 0 до 1
        currentValue = Mathf.Clamp01(currentValue);

        if (isLeftLever)

            // Передаёт степень торможения левого борта
            telemetry.LeftBrakeInput = currentValue;
        else

            // Передаёт степень торможения правого борта
            telemetry.RightBrakeInput = currentValue;
    }

    // Преобразует угол Unity из диапазона 0..360 в -180..180
    private float NormalizeAngle(float angle)
    {
        if (angle > 180f)

            // Например 350° превращается в -10°
            angle -= 360f;

        return angle;
    }
}