using UnityEngine;

public class ExerciseManager : MonoBehaviour
{
    public static ExerciseManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private TabletController tablet;

    [SerializeField] private ExerciseBinding[] exerciseBindings;

    [Header("Debug")]
    [SerializeField] private ExerciseInstance currentExercise;

    [SerializeField] private ExerciseGate startGate;

    [SerializeField] private ExerciseGate finishGate;

    [SerializeField] private ExerciseZone currentZone;

    [SerializeField] private ObstacleCounter currentCounter;

    public ExerciseInstance CurrentExercise => currentExercise;

    private void Awake()
    {
        Instance = this;

        foreach (ExerciseBinding binding in exerciseBindings)
        {
            if (binding == null)
                continue;

            if (binding.gateA != null)
                binding.gateA.Initialize(this);

            if (binding.gateB != null)
                binding.gateB.Initialize(this);

            if (binding.zone != null)
                binding.zone.Initialize(this);
        }
    }
    public void NotifyTutorialSkipped()
    {
        Debug.Log("Tutorial skipped -> enabling exercise flow");

        tablet.ShowExerciseList();

        ClearCurrentExercise();
    }
    public void NotifyGate(ExerciseGate gate)
    {
        if (gate == null)
            return;

        // =====================================================
        // Начало упражнения
        // =====================================================

        if (currentExercise == null)
        {
            foreach (ExerciseBinding binding in exerciseBindings)
            {
                if (binding == null)
                    continue;

                bool fromA = binding.gateA == gate;
                bool fromB = binding.gateB == gate;

                if (!fromA && !fromB)
                    continue;

                currentExercise = new ExerciseInstance(binding.exercise);

                currentZone = binding.zone;
                currentCounter = binding.counter;

                startGate = gate;
                finishGate = fromA
                ? binding.gateB
                : binding.gateA;

                currentExercise.Reset();

                currentCounter?.ResetCounter();
                ResetObstacles(binding.exercise.obstacles);

                tablet.ShowExercise(binding.exercise);

                Debug.Log($"Exercise started: {binding.exercise.exerciseName}");

                return;
            }

            return;
        }

        // =====================================================
        // Финиш упражнения
        // =====================================================

        if (gate != finishGate)
            return;

        int hits = currentCounter != null ? currentCounter.HitCount : 0;
        currentExercise.Finish(hits);

        tablet.ShowExerciseResult(currentExercise);

        Debug.Log($"Exercise finished: {currentExercise.Data.exerciseName}, hits: {hits}");

        ClearCurrentExercise();
    }

    public void NotifyZoneExit(ExerciseZone zone)
    {
        if (currentExercise == null)
            return;

        if (zone != currentZone)
            return;

        Debug.Log("Exercise cancelled");

        tablet.ShowExerciseList();

        ClearCurrentExercise();
    }

    private void ResetObstacles(ObstacleBehaviour[] obstacles)
    {
        if (obstacles == null)
            return;

        foreach (ObstacleBehaviour obstacle in obstacles)
        {
            if (obstacle != null)
                obstacle.ResetObstacle();
        }
    }

    private void ClearCurrentExercise()
    {
        startGate = null;
        finishGate = null;
        currentZone = null;
        currentCounter = null;
        currentExercise = null;
    }

    public bool IsRunning(ExerciseInstance exercise)
    {
        return currentExercise == exercise;
    }
}
