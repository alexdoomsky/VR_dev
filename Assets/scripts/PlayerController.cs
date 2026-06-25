using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;
//GIVEN

// PlayerController — компонент управления VR-игроком
// MonoBehaviour = базовый Unity класс, даёт lifecycle методы:
// Start / Update / OnEnable / OnDisable и доступ к сцене
public class PlayerController : MonoBehaviour
{
    // InputActionReference — ссылка на Action из Input System (не сам input, а ссылка на его описание)
    // Это wrapper над InputAction, который хранится в InputActionAsset
    // Используется для декуплинга input от кода (можно переназначать в инспекторе)
    public InputActionReference LeftTriggerValue;

    // Аналогично левому триггеру:
    // хранит ссылку на action правого триггера контроллера
    public InputActionReference RightTriggerValue;

    [Header("Body components")]

    // Transform — базовый Unity тип для позиции/вращения/масштаба объекта
    // Левый объект руки игрока (в VR обычно привязан к XR controller pose)
    public Transform LeftHand;

    // Transform правой руки игрока
    public Transform RightHand;

    // Transform головы игрока (обычно HMD / XR Camera)
    public Transform Head;

    // private float — локальная переменная класса
    // "_" prefix = соглашение: "internal state variable"
    private float _leftValue;

    // текущее значение правого триггера
    private float _rightValue;

    // таймер накопления времени между логами
    // используется вместо Debug.Log каждый кадр (что дорого по CPU + GC allocations)
    private float _logTimer = 0f;

    // const = compile-time constant
    // значение фиксировано и не изменяется во время выполнения
    private const float LOG_INTERVAL = 0.1f; // 100ms между логами

    // Start()
    // Unity lifecycle method:
    // вызывается ОДИН раз перед первым Update
    // гарантирует что все Awake() уже выполнены
    // OnEnable()
    // вызывается каждый раз когда GameObject или компонент становится активным
    // важно: может вызываться МНОГО РАЗ за жизнь объекта
    private void OnEnable()
    {
        // action — это InputAction внутри InputActionReference
        // Enable() переводит InputAction в активное состояние:
        // - начинает слушать input system
        // - начинает получать значения из устройства
        LeftTriggerValue.action.Enable();

        // то же самое для правого триггера
        RightTriggerValue.action.Enable();
    }

    // OnDisable()
    // вызывается когда объект выключается или удаляется
    // важно: всегда должен “undo” OnEnable, иначе будут утечки input подписок
    private void OnDisable()
    {
        // Disable():
        // - отключает polling input
        // - освобождает обработку событий
        LeftTriggerValue.action.Disable();
        RightTriggerValue.action.Disable();
    }

    // Update()
    // вызывается КАЖДЫЙ КАДР (frame-based loop)
    // частота зависит от FPS (не фиксированное время)
    private void Update()
    {
        // ReadValue<T>()
        // Generic method Input System:
        // T = float → значит ожидается float значение action (0..1)
        //
        // Что происходит внутри:
        // - Input System берёт текущее состояние устройства
        // - преобразует его в нормализованное значение action
        // - возвращает его как float
        _leftValue = LeftTriggerValue.action.ReadValue<float>();

        _rightValue = RightTriggerValue.action.ReadValue<float>();

        // ========================= DEBUG SECTION =========================
        // ниже закомментирован debug-код, который:
        // - накапливает время
        // - печатает значения input не каждый кадр, а с интервалом

        /*
        _logTimer += Time.deltaTime;

        // Time.deltaTime:
        // время между текущим и прошлым кадром (в секундах)
        // зависит от FPS (например 0.016 при 60 FPS)

        if (_logTimer >= LOG_INTERVAL)
        {
            // сброс таймера
            _logTimer = 0f;

            // string interpolation:
            // $"..." — синтаксический сахар C#
            // внутри можно вставлять выражения {value}
            //
            // F2:
            // формат числа float с 2 знаками после запятой
            // пример: 0.456 → 0.46
            string logText = $"LT: {_leftValue:F2} | RT: {_rightValue:F2}";

            // PlayerLogger.Message(string)
            // предположительно:
            // - принимает строку
            // - выводит в консоль / UI / файл
            // - может иметь buffering или filtering
            PlayerLogger.Message(logText);
        }
        */
    }
}
