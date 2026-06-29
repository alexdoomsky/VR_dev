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

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"Checkpoint {name}: entered by {other.name}, tag={other.tag}");

        if (!other.CompareTag(requiredTag))
        {
            Debug.Log("Wrong tag");
            return;
        }

        if (manager == null)
        {
            Debug.LogError("TutorialManager is NULL");
            return;
        }

        Debug.Log("Notify tutorial");

        manager.NotifyCheckpoint(this);
    }
}
