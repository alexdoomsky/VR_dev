using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Универсальная XR-кнопка танка.
/// Просто сообщает о своем нажатии через TankEventBus.
/// </summary>
[RequireComponent(typeof(XRSimpleInteractable))]
public class TankButtonInteractable : MonoBehaviour
{
    [Header("Button")]

    [SerializeField]
    private TankButton buttonType;

    private XRSimpleInteractable interactable;

    private void Awake()
    {
        interactable = GetComponent<XRSimpleInteractable>();
    }

    private void OnEnable()
    {
        interactable.selectEntered.AddListener(OnPressed);
    }

    private void OnDisable()
    {
        interactable.selectEntered.RemoveListener(OnPressed);
    }

    private void OnPressed(SelectEnterEventArgs args)
    {
        TankEventBus.RaiseButtonPressed(buttonType);
    }

    #if UNITY_EDITOR

    [ContextMenu("Press Button")]
    private void DebugPress()
    {
        TankEventBus.RaiseButtonPressed(buttonType);
    }

    #endif
}
