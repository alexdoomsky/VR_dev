using UnityEngine;

public class TutorialStartUI : MonoBehaviour
{
    [SerializeField] private TutorialManager tutorialManager;
    [SerializeField] private ExerciseManager exerciseManager;
    [SerializeField] private GameObject root;

    public void OnStartTutorial()
    {
        root.SetActive(false);
        tutorialManager.StartTutorial();
    }

    public void OnSkipTutorial()
    {
        root.SetActive(false);
        tutorialManager.SkipTutorial();
        exerciseManager.NotifyTutorialSkipped();
    }
}
