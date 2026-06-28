using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TutorialCheckpoint : MonoBehaviour
{
    private TutorialManager manager;

    [SerializeField]
    private string requiredTag = "Tank";

    public void Initialize(TutorialManager tutorialManager)
    {
        manager = tutorialManager;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(requiredTag))
            return;

        manager.NotifyCheckpoint(this);
    }
}
