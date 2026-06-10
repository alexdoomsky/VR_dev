using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public struct HandAnimationData
{
    public readonly List<Finger> Fingers;
    public readonly List<float> TargetValues;

    public HandAnimationData(List<Finger> fingers, List<float> targetValues)
    {
        Fingers = fingers;
        TargetValues = targetValues;
    }
}

public class Finger
{
    public FingerType Type;
    public float BlendTargetValue = 0.0f;
    public float BlendCurrentValue = 0.0f;

    public Finger(FingerType type)
    {
        Type = type;
    }
}

public enum FingerType { Thumb, Index, Middle, Ring, Pinky }

public class HandAnimationController : MonoBehaviour
{
    [SerializeField] private XRBaseInputInteractor[] _interactors;
    [SerializeField] private GameObject _handModel;
    [SerializeField] private float _animationSpeed = 10.0f;
    [SerializeField] private Animator _animator;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference _gripAction;
    [SerializeField] private InputActionReference _wipeAction;  // Протирка триплекса
    [SerializeField] private InputActionReference _okAction;    // Жест "ОК"

    // Кулак: все 5 пальцев сжимаются (grip)
    private HandAnimationData _grabAnimation;
    // Указание: все пальцы кроме указательного сжимаются (hover рядом с интерактивным объектом)
    private HandAnimationData _hoverAnimation;
    // Протирка триплекса: все пальцы кроме большого сжимаются (кнопка _wipeAction)
    private HandAnimationData _wipeAnimation;
    // Жест "ОК": большой и указательный ~0.5, средний 0.2, безымянный 0.1, мизинец вытянут
    private HandAnimationData _okAnimation;


    // Счётчик интерактивных объектов в зоне ховера — нужен, если рука одновременно
    // выходит из одного объекта и входит в другой, чтобы не сбросить анимацию раньше времени.
    private int _hoverCount = 0;

    private void Awake()
    {
        InitializeHandAnimations();
    }

    private void InitializeHandAnimations()
    {
        // Grab / Fist — grip axis двигает все 5 пальцев
        _grabAnimation = new HandAnimationData(
            fingers: new List<Finger>
            {
                new Finger(FingerType.Thumb),
                                               new Finger(FingerType.Index),
                                               new Finger(FingerType.Middle),
                                               new Finger(FingerType.Ring),
                                               new Finger(FingerType.Pinky)
            },
            targetValues: new List<float> { 1.0f, 1.0f, 1.0f, 1.0f, 1.0f }
        );

        // Hover / Point — все пальцы кроме указательного (Index остаётся вытянутым)
        _hoverAnimation = new HandAnimationData(
            fingers: new List<Finger>
            {
                new Finger(FingerType.Thumb),
                                                new Finger(FingerType.Middle),
                                                new Finger(FingerType.Ring),
                                                new Finger(FingerType.Pinky)
            },
            targetValues: new List<float> { 0.8f, 0.8f, 0.8f, 0.8f }
        );

        // Wipe / Протирка — все пальцы кроме большого (Thumb остаётся вытянутым)
        _wipeAnimation = new HandAnimationData(
            fingers: new List<Finger>
            {
                new Finger(FingerType.Index),
                                               new Finger(FingerType.Middle),
                                               new Finger(FingerType.Ring),
                                               new Finger(FingerType.Pinky)
            },
            targetValues: new List<float> { 0.9f, 0.9f, 0.9f, 0.9f }
        );

        // OK — большой и указательный образуют кольцо, остальные расслаблены, мизинец вытянут
        _okAnimation = new HandAnimationData(
            fingers: new List<Finger>
            {
                new Finger(FingerType.Thumb),
                                             new Finger(FingerType.Index),
                                             new Finger(FingerType.Middle),
                                             new Finger(FingerType.Ring)
                                             // Pinky не включён — он остаётся вытянутым (значение 0)
            },
            targetValues: new List<float> { 0.5f, 0.5f, 0.2f, 0.1f }
        );
    }

    private void OnEnable()
    {
        foreach (var interactor in _interactors)
        {
            interactor.selectEntered.AddListener(OnSelect);
            interactor.selectExited.AddListener(OnDeselect);
            interactor.hoverEntered.AddListener(OnHoverEntered);
            interactor.hoverExited.AddListener(OnHoverExited);
        }
    }

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

    private void OnSelect(SelectEnterEventArgs args)
    {
        Debug.Log("[HAND_ANIMATION_CONTROLLER] grabbed!");
        // HideHands(true);
    }

    private void OnDeselect(SelectExitEventArgs args)
    {
        // HideHands(false);
    }

    private void OnHoverEntered(HoverEnterEventArgs args)
    {
        if (args.interactableObject is UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable)
            return;

        _hoverCount++;
    }

    private void OnHoverExited(HoverExitEventArgs args)
    {
        if (args.interactableObject is UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable)
            return;

        _hoverCount = Mathf.Max(0, _hoverCount - 1);
    }

    private void HideHands(bool state)
    {
        _handModel.SetActive(!state);
    }

    private void Update()
    {
        // Считать input и выставить целевые значения
        CheckGrip();
        CheckHover();
        CheckWipe();
        CheckOk();

        // Плавно двигать текущие значения к целевым
        SmoothFinger(_hoverAnimation);
        SmoothFinger(_wipeAnimation);
        SmoothFinger(_okAnimation);
        SmoothFinger(_grabAnimation);

        // Смёрджить все анимации через Mathf.Max и отправить в Animator
        ApplyFinalFingerValues();
    }

    private void CheckGrip()
    {
        if (_gripAction == null) return;
        float gripValue = _gripAction.action.ReadValue<float>();
        SetFingerTargetValues(_grabAnimation, gripValue);
    }

    private void CheckHover()
    {
        float hoverValue = _hoverCount > 0 ? 1f : 0f;
        SetFingerTargetValues(_hoverAnimation, hoverValue);
    }

    private void CheckWipe()
    {
        if (_wipeAction == null) return;
        float wipeValue = _wipeAction.action.ReadValue<float>();
        SetFingerTargetValues(_wipeAnimation, wipeValue);
    }

    private void CheckOk()
    {
        if (_okAction == null) return;
        float okValue = _okAction.action.ReadValue<float>();
        SetFingerTargetValues(_okAnimation, okValue);
    }


    private void SetFingerTargetValues(HandAnimationData handAnimation, float value)
    {
        for (int i = 0; i < handAnimation.Fingers.Count; i++)
            handAnimation.Fingers[i].BlendTargetValue = handAnimation.TargetValues[i] * value;
    }

    private void SmoothFinger(HandAnimationData handAnimation)
    {
        for (int i = 0; i < handAnimation.Fingers.Count; i++)
        {
            float step = _animationSpeed * Time.deltaTime;
            handAnimation.Fingers[i].BlendCurrentValue = Mathf.MoveTowards(
                handAnimation.Fingers[i].BlendCurrentValue,
                handAnimation.Fingers[i].BlendTargetValue,
                step
            );
        }
    }

    /// <summary>
    /// Для каждого пальца берём максимальное значение из всех активных анимаций
    /// и отправляем в Animator. Это гарантирует, что неактивная анимация (значение 0)
    /// не затирает активную — проблема оригинальной цепочки AnimateFingers().
    /// </summary>
    private void ApplyFinalFingerValues()
    {
        var finalValues = new Dictionary<FingerType, float>
        {
            { FingerType.Thumb,  0f },
            { FingerType.Index,  0f },
            { FingerType.Middle, 0f },
            { FingerType.Ring,   0f },
            { FingerType.Pinky,  0f }
        };

        MergeFingerValues(_hoverAnimation, finalValues);
        MergeFingerValues(_wipeAnimation,  finalValues);
        MergeFingerValues(_okAnimation,    finalValues);
        MergeFingerValues(_grabAnimation,  finalValues);

        foreach (var kvp in finalValues)
            _animator.SetFloat(kvp.Key.ToString(), kvp.Value);
    }

    private void MergeFingerValues(HandAnimationData handAnimation, Dictionary<FingerType, float> finalValues)
    {
        for (int i = 0; i < handAnimation.Fingers.Count; i++)
        {
            FingerType ft = handAnimation.Fingers[i].Type;
            finalValues[ft] = Mathf.Max(finalValues[ft], handAnimation.Fingers[i].BlendCurrentValue);
        }
    }
}
