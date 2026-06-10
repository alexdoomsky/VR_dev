using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
//VISUAL
//GIVEN-UPDATED

// struct = value type (тип-значение)
// В отличие от class хранится обычно в стеке и копируется целиком.
//
// Хранит набор пальцев и их целевые значения для конкретной анимации.
public struct HandAnimationData
{
    // readonly запрещает заменять ссылку после создания структуры.
    // Сам объект List менять можно, но присвоить новый List нельзя.
    public readonly List<Finger> Fingers;

    // Целевые коэффициенты сгибания пальцев.
    public readonly List<float> TargetValues;

    // Конструктор структуры.
    // Вызывается при создании через new HandAnimationData(...)
    public HandAnimationData(List<Finger> fingers, List<float> targetValues)
    {
        Fingers = fingers;
        TargetValues = targetValues;
    }
}

// Класс отдельного пальца.
public class Finger
{
    // Тип пальца.
    public FingerType Type;

    // Значение, к которому должен стремиться палец.
    public float BlendTargetValue = 0.0f;

    // Текущее сглаженное значение.
    public float BlendCurrentValue = 0.0f;

    // Конструктор класса.
    public Finger(FingerType type)
    {
        Type = type;
    }
}

// enum = перечисление фиксированных значений.
//
// Используется вместо строк:
// лучше писать FingerType.Thumb
// чем "Thumb"
public enum FingerType
{
    Thumb,
    Index,
    Middle,
    Ring,
    Pinky
}

// Контроллер анимации руки.
//
// Отвечает за:
// - чтение input
// - отслеживание hover/select событий XR
// - расчёт целевых поз пальцев
// - плавную интерполяцию
// - передачу значений в Animator
public class HandAnimationController : MonoBehaviour
{
    [SerializeField] private XRBaseInputInteractor[] _interactors;

    // XRBaseInputInteractor
    // базовый класс XR контроллеров взаимодействия.
    //
    // Через него приходят события:
    // selectEntered
    // selectExited
    // hoverEntered
    // hoverExited

    [SerializeField] private GameObject _handModel;

    // Скорость сглаживания анимации.
    [SerializeField] private float _animationSpeed = 10.0f;

    // Unity Animator.
    // Управляет параметрами анимационного графа.
    [SerializeField] private Animator _animator;

    [Header("Input Actions")]

    // Сила сжатия кисти.
    [SerializeField] private InputActionReference _gripAction;

    // Отдельное действие для протирки триплекса.
    [SerializeField] private InputActionReference _wipeAction;

    // Отдельное действие для жеста ОК.
    [SerializeField] private InputActionReference _okAction;

    // ===== Наборы анимаций =====

    // Кулак.
    private HandAnimationData _grabAnimation;

    // Указание пальцем.
    private HandAnimationData _hoverAnimation;

    // Протирка.
    private HandAnimationData _wipeAnimation;

    // Жест ОК.
    private HandAnimationData _okAnimation;

    // Количество объектов под курсором руки.
    //
    // Почему int а не bool?
    //
    // Потому что одновременно можно навести руку
    // сразу на несколько объектов.
    //
    // Если использовать bool:
    // вошли в объект А
    // вошли в объект Б
    // вышли из объекта А
    // bool станет false
    //
    // хотя объект Б всё ещё наведен.
    private int _hoverCount = 0;

    // Awake вызывается раньше Start.
    private void Awake()
    {
        InitializeHandAnimations();
    }

    // Создание всех конфигураций анимаций.
    private void InitializeHandAnimations()
    {
        // Все пять пальцев участвуют.
        _grabAnimation = new HandAnimationData(
            fingers: new List<Finger>
            {
                new Finger(FingerType.Thumb),
                                               new Finger(FingerType.Index),
                                               new Finger(FingerType.Middle),
                                               new Finger(FingerType.Ring),
                                               new Finger(FingerType.Pinky)
            },
            targetValues: new List<float>
            {
                1.0f,1.0f,1.0f,1.0f,1.0f
            }
        );

        // Указательный остаётся прямым.
        _hoverAnimation = new HandAnimationData(
            fingers: new List<Finger>
            {
                new Finger(FingerType.Thumb),
                                                new Finger(FingerType.Middle),
                                                new Finger(FingerType.Ring),
                                                new Finger(FingerType.Pinky)
            },
            targetValues: new List<float>
            {
                0.8f,0.8f,0.8f,0.8f
            }
        );

        // Большой палец прямой.
        _wipeAnimation = new HandAnimationData(
            fingers: new List<Finger>
            {
                new Finger(FingerType.Index),
                                               new Finger(FingerType.Middle),
                                               new Finger(FingerType.Ring),
                                               new Finger(FingerType.Pinky)
            },
            targetValues: new List<float>
            {
                0.9f,0.9f,0.9f,0.9f
            }
        );

        // Жест ОК.
        _okAnimation = new HandAnimationData(
            fingers: new List<Finger>
            {
                new Finger(FingerType.Thumb),
                                             new Finger(FingerType.Index),
                                             new Finger(FingerType.Middle),
                                             new Finger(FingerType.Ring)
            },
            targetValues: new List<float>
            {
                0.5f,0.5f,0.2f,0.1f
            }
        );
    }

    // Подписка на XR события.
    private void OnEnable()
    {
        foreach (var interactor in _interactors)
        {
            // AddListener регистрирует обработчик события.

            interactor.selectEntered.AddListener(OnSelect);
            interactor.selectExited.AddListener(OnDeselect);

            interactor.hoverEntered.AddListener(OnHoverEntered);
            interactor.hoverExited.AddListener(OnHoverExited);
        }
    }

    // Отписка от XR событий.
    //
    // Важно:
    // если не отписаться могут появиться
    // двойные вызовы событий.
    private void OnDisable()
    {
        foreach (var interactor in _interactors)
        {
            interactor.selectEntered.RemoveListener(OnSelect);
            interactor.selectExited.RemoveListener(OnDeselect);

            interactor.hoverEntered.RemoveListener(OnHoverEntered);
            interactor.hoverExited.RemoveListener(OnHoverExited);
        }
    }

    // Вызывается при захвате объекта.
    private void OnSelect(SelectEnterEventArgs args)
    {
        Debug.Log("[HAND_ANIMATION_CONTROLLER] grabbed!");
    }

    // Вызывается при отпускании объекта.
    private void OnDeselect(SelectExitEventArgs args)
    {
    }

    // Вызывается когда рука навелась на объект.
    private void OnHoverEntered(HoverEnterEventArgs args)
    {
        // is = проверка типа объекта.
        //
        // XRGrabInteractable
        // стандартный XR объект который можно брать.
        if (args.interactableObject is UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable)
            return;

        _hoverCount++;
    }

    // Вызывается при выходе курсора руки.
    private void OnHoverExited(HoverExitEventArgs args)
    {
        if (args.interactableObject is UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable)
            return;

        // Mathf.Max возвращает большее из двух значений.
        //
        // Нужен чтобы счётчик не ушёл ниже нуля.
        _hoverCount = Mathf.Max(0, _hoverCount - 1);
    }

    // Скрыть/показать модель руки.
    private void HideHands(bool state)
    {
        // SetActive включает или выключает объект.
        _handModel.SetActive(!state);
    }

    // Главный игровой цикл.
    private void Update()
    {
        // Получаем input.
        CheckGrip();
        CheckHover();
        CheckWipe();
        CheckOk();

        // Сглаживаем значения.
        SmoothFinger(_hoverAnimation);
        SmoothFinger(_wipeAnimation);
        SmoothFinger(_okAnimation);
        SmoothFinger(_grabAnimation);

        // Передаём в Animator.
        ApplyFinalFingerValues();
    }

    private void CheckGrip()
    {
        if (_gripAction == null)
            return;

        // ReadValue<float>()
        // получает текущее значение action.
        float gripValue =
        _gripAction.action.ReadValue<float>();

        SetFingerTargetValues(_grabAnimation, gripValue);
    }

    private void CheckHover()
    {
        // Тернарный оператор.
        //
        // условие ? значение1 : значение2
        float hoverValue =
        _hoverCount > 0 ? 1f : 0f;

        SetFingerTargetValues(_hoverAnimation, hoverValue);
    }

    private void CheckWipe()
    {
        if (_wipeAction == null)
            return;

        float wipeValue =
        _wipeAction.action.ReadValue<float>();

        SetFingerTargetValues(_wipeAnimation, wipeValue);
    }

    private void CheckOk()
    {
        if (_okAction == null)
            return;

        float okValue =
        _okAction.action.ReadValue<float>();

        SetFingerTargetValues(_okAnimation, okValue);
    }

    // Вычисляет целевые значения пальцев.
    private void SetFingerTargetValues(
        HandAnimationData handAnimation,
        float value)
    {
        for (int i = 0; i < handAnimation.Fingers.Count; i++)
        {
            handAnimation.Fingers[i].BlendTargetValue =
            handAnimation.TargetValues[i] * value;
        }
    }

    // Сглаживание движения пальцев.
    private void SmoothFinger(HandAnimationData handAnimation)
    {
        for (int i = 0; i < handAnimation.Fingers.Count; i++)
        {
            float step =
            _animationSpeed * Time.deltaTime;

            // MoveTowards
            //
            // current
            // target
            // maxDelta
            //
            // Двигает значение к цели
            // не превышая step за кадр.
            handAnimation.Fingers[i].BlendCurrentValue =
            Mathf.MoveTowards(
                handAnimation.Fingers[i].BlendCurrentValue,
                handAnimation.Fingers[i].BlendTargetValue,
                step
            );
        }
    }

    // Слияние всех анимаций.
    //
    // Если один палец участвует в нескольких позах,
    // берётся максимальное значение.
    private void ApplyFinalFingerValues()
    {
        // Dictionary<TKey,TValue>
        //
        // коллекция ключ-значение.
        //
        // здесь:
        // FingerType → float
        var finalValues =
        new Dictionary<FingerType, float>
        {
            { FingerType.Thumb, 0f },
            { FingerType.Index, 0f },
            { FingerType.Middle, 0f },
            { FingerType.Ring, 0f },
            { FingerType.Pinky, 0f }
        };

        MergeFingerValues(_hoverAnimation, finalValues);
        MergeFingerValues(_wipeAnimation, finalValues);
        MergeFingerValues(_okAnimation, finalValues);
        MergeFingerValues(_grabAnimation, finalValues);

        // foreach перебирает коллекцию.
        foreach (var kvp in finalValues)
        {
            // kvp.Key = палец
            // kvp.Value = итоговое значение

            _animator.SetFloat(
                kvp.Key.ToString(),
                               kvp.Value
            );
        }
    }

    // Объединяет значения одной анимации
    // с итоговым словарём.
    private void MergeFingerValues(
        HandAnimationData handAnimation,
        Dictionary<FingerType, float> finalValues)
    {
        for (int i = 0; i < handAnimation.Fingers.Count; i++)
        {
            FingerType ft =
            handAnimation.Fingers[i].Type;

            finalValues[ft] =
            Mathf.Max(
                finalValues[ft],
                handAnimation.Fingers[i].BlendCurrentValue
            );
        }
    }
}
