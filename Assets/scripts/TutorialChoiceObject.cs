using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
[RequireComponent(typeof(XRGrabInteractable))]
public class TutorialChoiceObject : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject uiRoot;

    private XRGrabInteractable grabInteractable;

    private TutorialChoiceZone currentZone;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
    }

    private void OnEnable()
    {
        grabInteractable.selectExited.AddListener(OnReleased);
    }

    private void OnDisable()
    {
        grabInteractable.selectExited.RemoveListener(OnReleased);
    }

    private void OnTriggerEnter(Collider other)
    {
        TutorialChoiceZone zone = other.GetComponent<TutorialChoiceZone>();

        if (zone != null)
            currentZone = zone;
    }

    private void OnTriggerExit(Collider other)
    {
        TutorialChoiceZone zone = other.GetComponent<TutorialChoiceZone>();

        if (zone == currentZone)
            currentZone = null;
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        if (currentZone == null)
            return;

        currentZone.ExecuteChoice();

        uiRoot.SetActive(false);

        Destroy(gameObject);
    }
}
