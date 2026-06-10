using UnityEngine;

// Управляет поворотом стрелки прибора по входному значению от 0 до 1
//VISUAL
//SLOP
public class GaugeNeedle2D : MonoBehaviour
{
    // enum - перечисление фиксированного набора значений
    public enum GaugeMode
    {
        // Интерполяция между заданными точками
        Linear,

        // Движение по дуге между двумя углами
        Circular
    }

    [Header("Mode")]

    // Режим работы стрелки
    [SerializeField] private GaugeMode mode;

    [Header("Input")]

    // Значение прибора в диапазоне 0..1
    [Range(0f, 1f)]
    public float value;

    [Header("Smoothing")]

    // Скорость сглаживания движения стрелки
    [SerializeField] private float smooth = 10f;

    [Header("Base rotation (keeps model orientation)")]

    // Исходный поворот модели
    private Vector3 baseRot;

    // Текущий сглаженный поворот
    private Vector3 currentRot;

    // Serializable позволяет редактировать структуру в Inspector
    [System.Serializable]

    // struct - пользовательский тип данных
    // хранит несколько связанных значений
    public struct Keyframe2D
    {
        // Позиция точки на шкале (0..1)
        [Range(0f, 1f)]
        public float t;

        // Угол по оси X
        public float x;

        // Угол по оси Z
        public float z;
    }

    [Header("Linear Mapping (ONLY for Linear mode)")]

    // Массив контрольных точек для линейного режима
    public Keyframe2D[] points;

    [Header("Circular Mapping (ONLY for RPM)")]

    // Минимальный и максимальный угол по оси X
    public float minAngleX = -50f;
    public float maxAngleX = 228f;

    // Минимальный и максимальный угол по оси Z
    public float minAngleZ = -75f;
    public float maxAngleZ = -110f;

    // Вызывается Unity при создании объекта
    private void Awake()
    {
        // localEulerAngles - локальные углы объекта относительно родителя
        baseRot = transform.localEulerAngles;

        currentRot = baseRot;
    }

    // Вызывается Unity каждый кадр
    private void Update()
    {
        // Получение целевого положения стрелки
        Vector3 target = Evaluate(value);

        // Создание итогового поворота
        Vector3 desired = new Vector3(
            target.x,
            baseRot.y,
            target.z
        );

        // Vector3.Lerp()
        // Плавно интерполирует между двумя векторами
        currentRot = Vector3.Lerp(
            currentRot,
            desired,

            // Time.deltaTime
            // Время между кадрами
            Time.deltaTime * smooth
        );

        // Установка локального поворота объекта
        transform.localEulerAngles = currentRot;
    }

    // Выбирает способ расчёта положения стрелки
    private Vector3 Evaluate(float t)
    {
        // Clamp01 ограничивает значение диапазоном 0..1
        t = Mathf.Clamp01(t);

        if (mode == GaugeMode.Circular)
        {
            return EvaluateCircular(t);
        }

        return EvaluateLinear(t);
    }

    // Вычисляет положение стрелки через массив контрольных точек
    private Vector3 EvaluateLinear(float t)
    {
        // Проверка что массив существует
        // и содержит минимум две точки
        if (points == null || points.Length < 2)

            // Vector3.zero = (0,0,0)
            return Vector3.zero;

        // Проход по всем сегментам шкалы
        for (int i = 0; i < points.Length - 1; i++)
        {
            if (t >= points[i].t && t <= points[i + 1].t)
            {
                // InverseLerp переводит значение в диапазон 0..1
                // относительно двух соседних точек
                float localT = Mathf.InverseLerp(
                    points[i].t,
                    points[i + 1].t,
                    t
                );

                // Интерполяция угла X между двумя точками
                float x = Mathf.Lerp(
                    points[i].x,
                    points[i + 1].x,
                    localT
                );

                // Интерполяция угла Z между двумя точками
                float z = Mathf.Lerp(
                    points[i].z,
                    points[i + 1].z,
                    localT
                );

                return new Vector3(x, 0f, z);
            }
        }

        // Length возвращает размер массива
        Keyframe2D last = points[points.Length - 1];

        return new Vector3(last.x, 0f, last.z);
    }

    // Вычисляет положение стрелки по дуге между двумя углами
    private Vector3 EvaluateCircular(float t)
    {
        // Lerp плавно переводит значение между двумя границами

        float x = Mathf.Lerp(
            minAngleX,
            maxAngleX,
            t
        );

        float z = Mathf.Lerp(
            minAngleZ,
            maxAngleZ,
            t
        );

        return new Vector3(x, 0f, z);
    }
}