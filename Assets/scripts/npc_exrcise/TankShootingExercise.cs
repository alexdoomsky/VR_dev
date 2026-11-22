
using UnityEngine;

public class TankShootingExercise : MonoBehaviour
{
    private enum ExerciseState
    {
        Inactive,
        Preparing,
        Running,
        Finished
    }

    [Header("Exercise")]
    [SerializeField] private ExerciseData exerciseData;
    [SerializeField] private TabletController tablet;

    [Tooltip("Страница, отображаемая при досрочном выходе из упражнения")]
    [SerializeField] private GameObject earlyExitPagePrefab;

    [Header("Triggers")]
    [SerializeField] private ExerciseTriggerRelay exerciseTrigger;
    [SerializeField] private ExerciseTriggerRelay startTrigger;

    [Header("NPC")]
    [SerializeField] private SplineTankMovement[] npcTanks;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float sharedStartDelay = 2f;

    [Header("Debug")]
    [SerializeField] private ExerciseState state = ExerciseState.Inactive;
    [SerializeField] private int destroyedCount;
    [SerializeField] private int escapedCount;

    private ExerciseInstance exerciseInstance;

    private void Awake()
    {
        if (exerciseTrigger != null)
            exerciseTrigger.Initialize(this);

        if (startTrigger != null)
            startTrigger.Initialize(this);

        if (npcTanks == null || npcTanks.Length == 0)
            npcTanks = GetComponentsInChildren<SplineTankMovement>(true);

        foreach (SplineTankMovement npc in npcTanks)
        {
            if (npc == null)
                continue;

            npc.OnDestroyed += HandleNPCDestroyed;
            npc.OnEscaped += HandleNPCEscaped;
        }
    }

    // Въезд в общую зону: только показываем описание.
    public void NotifyExerciseZoneEntered()
    {
        if (state != ExerciseState.Inactive)
            return;

        if (exerciseData == null)
        {
            Debug.LogError(
                "[TankShootingExercise] ExerciseData не назначен.",
                this
            );
            return;
        }

        state = ExerciseState.Preparing;

        destroyedCount = 0;
        escapedCount = 0;
        exerciseInstance = null;

        tablet?.ShowExercise(exerciseData);

        Debug.Log(
            "[TankShootingExercise] Показано описание упражнения. " +
            "Ожидание въезда на огневую позицию."
        );
    }

    // Выезд из общей зоны.
    public void NotifyExerciseZoneExited()
    {
        if (state == ExerciseState.Preparing)
        {
            CancelPreparation();
            return;
        }

        if (state == ExerciseState.Running)
        {
            Debug.Log(
                "[TankShootingExercise] Игрок покинул зону во время выполнения."
            );

            FinishExercise(true);
        }
    }

    // Въезд на огневую позицию: фактический запуск.
    public void NotifyStartZoneEntered()
    {
        if (state != ExerciseState.Preparing)
            return;

        if (!HasNPCs())
        {
            Debug.LogError(
                "[TankShootingExercise] Не назначены NPC. " +
                "Невозможно запустить упражнение.",
                this
            );
            return;
        }

        state = ExerciseState.Running;

        exerciseInstance = new ExerciseInstance(exerciseData);

        destroyedCount = 0;
        escapedCount = 0;

        Debug.Log("[TankShootingExercise] Запуск движения NPC.");

        foreach (SplineTankMovement npc in npcTanks)
        {
            if (npc == null)
                continue;

            npc.StartMoving(sharedStartDelay);
        }

        CheckCompletion();
    }

    // Отмена подготовки: упражнение ещё не запускалось.
    private void CancelPreparation()
    {
        if (state != ExerciseState.Preparing)
            return;

        state = ExerciseState.Inactive;
        exerciseInstance = null;

        tablet?.ShowExerciseList();

        Debug.Log(
            "[TankShootingExercise] Подготовка отменена. " +
            "Игрок покинул зону до начала упражнения."
        );
    }

    private void HandleNPCDestroyed(SplineTankMovement npc)
    {
        if (state != ExerciseState.Running)
            return;

        destroyedCount++;

        Debug.Log(
            $"[TankShootingExercise] NPC уничтожен. " +
            $"Уничтожено: {destroyedCount}, сбежало: {escapedCount}"
        );

        CheckCompletion();
    }

    private void HandleNPCEscaped(SplineTankMovement npc)
    {
        if (state != ExerciseState.Running)
            return;

        escapedCount++;

        Debug.Log(
            $"[TankShootingExercise] NPC сбежал. " +
            $"Уничтожено: {destroyedCount}, сбежало: {escapedCount}"
        );

        CheckCompletion();
    }

    private void CheckCompletion()
    {
        if (state != ExerciseState.Running)
            return;

        if (!HasNPCs())
            return;

        foreach (SplineTankMovement npc in npcTanks)
        {
            if (npc == null)
                continue;

            if (!npc.IsDestroyed && !npc.IsEscaped)
                return;
        }

        // Все NPC уничтожены или сбежали.
        FinishExercise(false);
    }

    private void FinishExercise(bool earlyExit)
    {
        if (state != ExerciseState.Running)
            return;

        // Сначала меняем состояние, чтобы события OnEscaped
        // не запустили повторную проверку завершения.
        state = ExerciseState.Finished;

        // Все оставшиеся живые NPC считаются сбежавшими.
        foreach (SplineTankMovement npc in npcTanks)
        {
            if (npc == null)
                continue;

            if (!npc.IsDestroyed && !npc.IsEscaped)
            {
                npc.MarkEscaped();
                escapedCount++;
            }
        }

        if (exerciseInstance != null)
        {
            exerciseInstance.Finish(destroyedCount);

            if (earlyExit && earlyExitPagePrefab != null)
            {
                tablet?.ShowPage(earlyExitPagePrefab);
            }
            else
            {
                if (earlyExit)
                {
                    Debug.LogWarning(
                        "[TankShootingExercise] Не назначен Early Exit Page Prefab. " +
                        "Показываем стандартный результат.",
                        this
                    );
                }

                tablet?.ShowExerciseResult(exerciseInstance);
            }
        }

        Debug.Log(
            $"[TankShootingExercise] Упражнение завершено. " +
            $"Досрочно: {earlyExit}, " +
            $"уничтожено: {destroyedCount}, " +
            $"сбежало: {escapedCount}"
        );
    }

    private bool HasNPCs()
    {
        if (npcTanks == null || npcTanks.Length == 0)
            return false;

        foreach (SplineTankMovement npc in npcTanks)
        {
            if (npc != null)
                return true;
        }

        return false;
    }

    private void OnDestroy()
    {
        if (npcTanks == null)
            return;

        foreach (SplineTankMovement npc in npcTanks)
        {
            if (npc == null)
                continue;

            npc.OnDestroyed -= HandleNPCDestroyed;
            npc.OnEscaped -= HandleNPCEscaped;
        }
    }

    private void OnValidate()
    {
        sharedStartDelay = Mathf.Max(0f, sharedStartDelay);
    }
}
