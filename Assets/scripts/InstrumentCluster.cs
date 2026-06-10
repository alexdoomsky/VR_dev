using UnityEngine;

// Передаёт данные телеметрии на стрелочные приборы
//VISUAL
//SLOP
public class InstrumentCluster : MonoBehaviour
{
    [Header("Telemetry")]

    // Ссылка на телеметрию танка
    [SerializeField] private TankTelemetry telemetry;

    [Header("Needles")]

    // Стрелка тахометра (обороты двигателя)
    [SerializeField] private GaugeNeedle2D rpmNeedle;

    // Стрелка спидометра
    [SerializeField] private GaugeNeedle2D speedNeedle;

    // Стрелка температуры двигателя
    [SerializeField] private GaugeNeedle2D tempNeedle;

    // Стрелка уровня воды
    [SerializeField] private GaugeNeedle2D waterNeedle;

    [Header("Limits")]

    // Максимальные обороты двигателя
    [SerializeField] private float maxRPM = 2500f;

    // Максимальная скорость танка
    [SerializeField] private float maxSpeed = 60f;

    // Минимальная температура шкалы
    [SerializeField] private float tempMin = 20f;

    // Максимальная температура шкалы
    [SerializeField] private float tempMax = 105f;

    // Вызывается Unity каждый кадр
    private void Update()
    {
        // Проверка наличия телеметрии
        if (telemetry == null)
            return;

        // --------------------------------------------------
        // RPM
        // --------------------------------------------------

        // Перевод оборотов в диапазон 0..1
        //
        // Пример:
        // 1250 / 2500 = 0.5
        rpmNeedle.value =
        Mathf.Clamp01(
            telemetry.EngineRPM / maxRPM
        );

        // --------------------------------------------------
        // Speed
        // --------------------------------------------------

        // Перевод скорости в диапазон 0..1
        speedNeedle.value =
        Mathf.Clamp01(
            telemetry.SpeedKmh / maxSpeed
        );

        // --------------------------------------------------
        // Temperature
        // --------------------------------------------------

        // InverseLerp переводит значение температуры
        // в процент между tempMin и tempMax
        //
        // tempMin -> 0
        // tempMax -> 1
        tempNeedle.value =
        Mathf.InverseLerp(
            tempMin,
            tempMax,
            telemetry.EngineTemperature
        );

        // --------------------------------------------------
        // Water
        // --------------------------------------------------

        // Ограничение уровня воды диапазоном 0..1
        waterNeedle.value =
        Mathf.Clamp01(
            telemetry.WaterLevel
        );
    }
}