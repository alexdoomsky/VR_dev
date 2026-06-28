using UnityEngine;

public class ExerciseInstance : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private ExerciseData data;

    [Header("References")]
    [SerializeField] private ObstacleCounter obstacleCounter;

    public ExerciseData Data => data;

    private TutorialCheckpoint startGate;
    private TutorialCheckpoint finishGate;

    private bool started;
    private bool completed;

    public bool Started => started;
    public bool Completed => completed;

    /// <summary>
    /// Вызывается любой из двух ворот.
    /// </summary>
    public void OnGateTriggered(TutorialCheckpoint gate)
    {
        if (completed)
            return;

        // Первое пересечение любой воротины
        if (!started)
        {
            started = true;

            startGate = gate;

            ExerciseManager.Instance.StartExercise(this);

            return;
        }

        // Повторный въезд в стартовую воротину
        if (gate == startGate)
            return;

        finishGate = gate;

        ExerciseManager.Instance.FinishExercise();
    }

    /// <summary>
    /// Пользователь покинул область упражнения.
    /// </summary>
    public void OnBoundsExited()
    {
        if (!started)
            return;

        if (completed)
            return;

        ExerciseManager.Instance.LeaveExercise();
    }

    /// <summary>
    /// Вызывается менеджером после успешного завершения.
    /// </summary>
    public void Finish()
    {
        completed = true;

        Debug.Log($"Exercise \"{data.ExerciseName}\" completed. Hits: {GetHitCount()}");
    }

    /// <summary>
    /// Полный сброс упражнения.
    /// </summary>
    public void ResetExercise()
    {
        started = false;
        completed = false;

        startGate = null;
        finishGate = null;

        if (obstacleCounter != null)
            obstacleCounter.ResetCounter();
    }

    public int GetHitCount()
    {
        if (obstacleCounter == null)
            return 0;

        return obstacleCounter.HitCount;
    }
}
