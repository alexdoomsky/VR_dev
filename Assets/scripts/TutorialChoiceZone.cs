using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TutorialChoiceZone : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool startTutorial = true;

    [Header("References")]
    [SerializeField] private TutorialManager tutorialManager;

    public bool StartTutorial => startTutorial;

    public void ExecuteChoice()
    {
        if (startTutorial)
            tutorialManager.StartTutorial();
        else
            tutorialManager.SkipTutorial();
    }

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }
}
