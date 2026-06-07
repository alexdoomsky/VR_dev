using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class StarterButton : MonoBehaviour
{
    private XRSimpleInteractable interactable;

    [Header("References")]
    [SerializeField] private TankEngine engine;

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
        Debug.Log("starter pressed (XR)");

        if (engine == null)
        {
            Debug.LogError("StarterButton: TankEngine reference is missing");
            return;
        }

        engine.StartEngine();
    }

    [ContextMenu("PRESS STARTER (DEBUG)")]
    private void DebugPress()
    {
        Debug.Log("starter pressed (context menu)");

        if (engine == null)
        {
            Debug.LogError("StarterButton: TankEngine reference is missing");
            return;
        }

        engine.StartEngine();
    }
}
