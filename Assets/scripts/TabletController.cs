using UnityEngine;

/// <summary>
/// Управляет содержимым планшета.
/// Сам планшет не знает, что именно отображает:
/// обучение, упражнения, результаты и т.д.
/// Он просто показывает переданный prefab.
/// </summary>
public class TabletController : MonoBehaviour
{
    [Header("References")]

    [SerializeField] private Transform pageRoot;

    [Header("Exercise")]

    [Tooltip("Страница со списком упражнений")]
    [SerializeField] private GameObject exerciseListPrefab;

    [Header("Debug")]

    [SerializeField] private GameObject currentPage;

    /// <summary>
    /// Показывает любую страницу.
    /// </summary>
    public GameObject ShowPage(GameObject prefab)
    {
        ClearPage();

        if (prefab == null)
            return null;

        currentPage = Instantiate(prefab, pageRoot);

        return currentPage;
    }

    /// <summary>
    /// Показывает описание упражнения.
    /// </summary>
    public void ShowExercise(ExerciseData data)
    {
        if (data == null)
            return;

        ShowPage(data.ExercisePagePrefab);
    }

    /// <summary>
    /// Показывает страницу результата упражнения.
    /// </summary>
    public void ShowExerciseResult(ExerciseInstance exercise)
    {
        if (exercise == null)
            return;

        GameObject resultPrefab =
        exercise.Data.GetResultPrefab(exercise.GetHitCount());

        ShowPage(resultPrefab);
    }

    /// <summary>
    /// Показывает список упражнений.
    /// </summary>
    public void ShowExerciseList()
    {
        ShowPage(exerciseListPrefab);
    }

    /// <summary>
    /// Закрывает текущую страницу.
    /// </summary>
    public void ClearPage()
    {
        if (currentPage == null)
            return;

        Destroy(currentPage);
        currentPage = null;
    }

    /// <summary>
    /// Есть ли открытая страница.
    /// </summary>
    public bool HasPage()
    {
        return currentPage != null;
    }

    /// <summary>
    /// Возвращает текущую страницу.
    /// </summary>
    public GameObject GetCurrentPage()
    {
        return currentPage;
    }
}
