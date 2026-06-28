using UnityEngine;

public class ExerciseManager : MonoBehaviour
{
    public static ExerciseManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private TabletController tablet;

    public ExerciseInstance CurrentExercise { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    public void StartExercise(ExerciseInstance exercise)
    {
        if (CurrentExercise != null)
            return;

        CurrentExercise = exercise;

        tablet.ShowExercise(exercise.Data);
    }

    public void FinishExercise()
    {
        if (CurrentExercise == null)
            return;

        CurrentExercise.Finish();

        tablet.ShowExerciseResult(CurrentExercise);

        CurrentExercise = null;
    }

    public void LeaveExercise()
    {
        if (CurrentExercise == null)
            return;

        tablet.ShowExerciseList();

        CurrentExercise = null;
    }

    public bool IsRunning(ExerciseInstance exercise)
    {
        return CurrentExercise == exercise;
    }
}
