using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ExerciseGate : MonoBehaviour
{
    [SerializeField] private string requiredTag = "Tank";

    private ExerciseManager manager;

    public void Initialize(ExerciseManager exerciseManager)
    {
        manager = exerciseManager;
    }

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(requiredTag))
            return;

        if (manager == null)
            return;

        manager.NotifyGate(this);
    }
}
