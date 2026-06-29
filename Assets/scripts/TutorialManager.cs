using System.Collections;
using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TutorialData tutorialData;
    [SerializeField] private TabletController tablet;

    [Header("Checkpoint bindings")]
    [SerializeField] private TutorialCheckpointBinding[] checkpointBindings;

    [Header("Settings")]
    [SerializeField] private bool startAutomatically = true;

    public bool TutorialCompleted { get; private set; }

    private int currentStepIndex = -1;
    private Coroutine advanceRoutine;

    private TutorialStep CurrentStep =>
    tutorialData != null
    ? tutorialData.GetStep(currentStepIndex)
    : null;

    private void Awake()
    {
        foreach (var binding in checkpointBindings)
        {
            if (binding != null && binding.checkpoint != null)
                binding.checkpoint.Initialize(this);
        }
    }

    private void Start()
    {
        if (startAutomatically)
            StartTutorial();
    }

    private void OnEnable()
    {
        TankEventBus.OnButtonPressed += OnButtonPressed;

        TankEventBus.EngineStarted += OnEngineStarted;
        TankEventBus.EngineStopped += OnEngineStopped;
        TankEventBus.EngineStalled += OnEngineStalled;

        TankEventBus.GearChanged += OnGearChanged;
        TankEventBus.ClutchPressed += OnClutchPressed;
        TankEventBus.ThrottleChanged += OnThrottleChanged;
    }

    private void OnDisable()
    {
        TankEventBus.OnButtonPressed -= OnButtonPressed;

        TankEventBus.EngineStarted -= OnEngineStarted;
        TankEventBus.EngineStopped -= OnEngineStopped;
        TankEventBus.EngineStalled -= OnEngineStalled;

        TankEventBus.GearChanged -= OnGearChanged;
        TankEventBus.ClutchPressed -= OnClutchPressed;
        TankEventBus.ThrottleChanged -= OnThrottleChanged;
    }

    public void StartTutorial()
    {
        TutorialCompleted = false;
        currentStepIndex = 0;
        ShowCurrentStep();
    }

    public void SkipTutorial()
    {
        FinishTutorial();
    }

    private void FinishTutorial()
    {
        Debug.Log("Tutorial finished");

        TutorialCompleted = true;

        tablet.ClearPage();
        tablet.ShowExerciseList();

        enabled = false;
    }

    private void NextStep()
    {
        currentStepIndex++;

        if (currentStepIndex >= tutorialData.StepCount)
        {
            FinishTutorial();
            return;
        }

        ShowCurrentStep();
    }

    private void ResetToStep(int index)
    {
        currentStepIndex = Mathf.Clamp(index, 0, tutorialData.StepCount - 1);

        ShowCurrentStep();
    }

    private void ShowCurrentStep()
    {
        if (advanceRoutine != null)
        {
            StopCoroutine(advanceRoutine);
            advanceRoutine = null;
        }

        TutorialStep step = CurrentStep;

        tablet.ClearPage();

        if (step == null)
            return;

        if (step.pagePrefab != null)
            tablet.ShowPage(step.pagePrefab);
    }

    private void CompleteCurrentStep()
    {
        if (advanceRoutine != null)
            StopCoroutine(advanceRoutine);

        if (CurrentStep.autoAdvance)
            advanceRoutine = StartCoroutine(AdvanceRoutine());
        else
            NextStep();
    }

    private IEnumerator AdvanceRoutine()
    {
        yield return new WaitForSeconds(CurrentStep.autoAdvanceDelay);

        NextStep();
    }

    //=========================================================
    // CHECKPOINTS
    //=========================================================

    public void NotifyCheckpoint(TutorialCheckpoint checkpoint)
    {
        Debug.Log($"NotifyCheckpoint: {checkpoint.name}");

        if (CurrentStep == null)
        {
            Debug.Log("CurrentStep == null");
            return;
        }

        Debug.Log($"Current step: {CurrentStep.stepName}");

        foreach (var binding in checkpointBindings)
        {
            if (binding == null)
                continue;

            Debug.Log(
                $"Compare step={binding.step?.stepName}, checkpoint={binding.checkpoint?.name}");

            if (binding.step != CurrentStep)
                continue;

            if (binding.checkpoint != checkpoint)
                continue;

            Debug.Log("Checkpoint matched");

            CompleteCurrentStep();
            return;
        }

        Debug.Log("No binding found");
    }

    //=========================================================
    // BUTTONS
    //=========================================================

    private void OnButtonPressed(TankButton button)
    {
        if (CurrentStep == null)
            return;

        if (CurrentStep.successEvent != TutorialEventType.ButtonPressed)
            return;

        if (button == CurrentStep.successButton)
        {
            CompleteCurrentStep();
            return;
        }

        if (CurrentStep.resetOnWrongButton)
            ResetToStep(CurrentStep.resetStepIndex);
    }

    //=========================================================
    // ENGINE
    //=========================================================

    private void OnEngineStarted()
    {
        CheckSimpleEvent(TutorialEventType.EngineStarted);
    }

    private void OnEngineStopped()
    {
        CheckSimpleEvent(TutorialEventType.EngineStopped);
    }

    private void OnEngineStalled()
    {
        if (CurrentStep == null)
            return;

        if (CurrentStep.resetOnEngineStall)
        {
            ResetToStep(currentStepIndex);
            return;
        }

        CheckSimpleEvent(TutorialEventType.EngineStalled);
    }

    //=========================================================
    // GEAR
    //=========================================================

    private void OnGearChanged(int gear)
    {
        if (CurrentStep == null)
            return;

        if (CurrentStep.successEvent != TutorialEventType.GearChanged)
            return;

        if (gear == CurrentStep.successIntParameter)
            CompleteCurrentStep();
    }

    //=========================================================
    // CLUTCH
    //=========================================================

    private void OnClutchPressed()
    {
        CheckSimpleEvent(TutorialEventType.ClutchPressed);
    }

    //=========================================================
    // THROTTLE
    //=========================================================

    private void OnThrottleChanged(float value)
    {
        if (CurrentStep == null)
            return;

        if (CurrentStep.successEvent != TutorialEventType.ThrottlePressed)
            return;

        if (value >= 0.25f)
            CompleteCurrentStep();
    }

    //=========================================================

    private void CheckSimpleEvent(TutorialEventType type)
    {
        if (CurrentStep == null)
            return;

        if (CurrentStep.successEvent == type)
            CompleteCurrentStep();
    }
}
