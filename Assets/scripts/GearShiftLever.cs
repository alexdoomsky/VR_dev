using System;
using UnityEngine;

// Определяет текущую передачу по положению рычага КПП
public class GearShiftDetector : MonoBehaviour
{
    [System.Serializable]

    // Структура описывает одну позицию передачи
    public struct GearSlot
    {
        // Название передачи для удобства в Inspector
        public string label;

        // Номер передачи
        public int gear;

        // Vector2 хранит два значения:
        // x = угол вперёд/назад
        // y = угол влево/вправо
        //
        // Здесь используется для хранения эталонного положения рычага
        public Vector2 angles;
    }

    [Header("Gear layout")]

    // Массив всех положений передач
    [SerializeField] private GearSlot[] slots;

    [Header("Detection")]

    // Максимальное расстояние до точки,
    // при котором передача считается выбранной
    [SerializeField] private float snapThreshold = 10f;

    [Header("Smoothing")]

    // Пока не используется
    [SerializeField] private float updateSpeed = 20f;

    // Текущая передача
    private int currentGear = 0;

    // Исходный поворот рычага
    private Quaternion baseRotation;

    // Свойство только для чтения
    //
    // => сокращённая форма return
    public int CurrentGear => currentGear;

    // Событие вызывается при смене передачи
    //
    // Action<int> означает:
    // событие передаёт один параметр типа int
    public event Action<int> OnGearChanged;

    // Вызывается Unity при создании объекта
    void Awake()
    {
        // localRotation - локальный поворот объекта
        baseRotation = transform.localRotation;
    }

    // Вызывается Unity каждый кадр
    void Update()
    {
        // Получение текущих углов рычага
        Vector2 angles = GetLocalAnglesFromRotation();

        // Поиск ближайшей передачи
        //
        // out позволяет вернуть дополнительное значение из метода
        int nearest = FindNearest(angles, out float dist);

        if (nearest < 0)
            return;

        // Проверяем что рычаг достаточно близко к позиции передачи
        if (dist < snapThreshold)
        {
            int gear = slots[nearest].gear;

            // Если передача изменилась
            if (gear != currentGear)
            {
                currentGear = gear;

                // ?. проверяет что есть подписчики
                //
                // Invoke() вызывает событие
                OnGearChanged?.Invoke(currentGear);
                TankEventBus.RaiseGearChanged(currentGear);
            }
        }
    }

    // --------------------------------------------------
    // ROTATION → ANGLES
    // --------------------------------------------------

    // Преобразует поворот рычага в углы X и Z
    Vector2 GetLocalAnglesFromRotation()
    {
        // Quaternion.Inverse()
        // Создаёт обратный поворот
        //
        // Позволяет получить вращение относительно исходной позиции
        Quaternion localRot =
        Quaternion.Inverse(baseRotation) *
        transform.localRotation;

        // ToAngleAxis()
        //
        // Преобразует Quaternion в:
        // угол вращения
        // ось вращения
        //
        // out означает что значения будут записаны методом
        localRot.ToAngleAxis(
            out float angle,
            out Vector3 axis
        );

        // Приведение угла к диапазону -180..180
        if (angle > 180f)
            angle -= 360f;

        // eulerAngles возвращает углы Эйлера
        Vector3 euler = localRot.eulerAngles;

        euler.x = NormalizeAngle(euler.x);
        euler.z = NormalizeAngle(euler.z);

        // Возвращаем только X и Z
        return new Vector2(
            euler.x,
            euler.z
        );
    }

    // Преобразует угол из диапазона 0..360 в -180..180
    float NormalizeAngle(float a)
    {
        if (a > 180f)
            a -= 360f;

        return a;
    }

    // --------------------------------------------------
    // SEARCH NEAREST SLOT
    // --------------------------------------------------

    // Ищет ближайшую позицию передачи
    int FindNearest(Vector2 v, out float bestDist)
    {
        // Индекс лучшего совпадения
        int best = -1;

        // float.MaxValue
        // Максимально возможное значение float
        bestDist = float.MaxValue;

        // Проход по всем передачам
        for (int i = 0; i < slots.Length; i++)
        {
            // Vector2.Distance()
            // Вычисляет расстояние между двумя точками
            float d =
            Vector2.Distance(
                v,
                slots[i].angles
            );

            if (d < bestDist)
            {
                bestDist = d;
                best = i;
            }
        }

        return best;
    }
}
