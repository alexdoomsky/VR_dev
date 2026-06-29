using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Exercise", menuName = "Tank Trainer/Exercise")]
public class ExerciseData : ScriptableObject
{
    [Header("General")]
    public string exerciseName;

    [TextArea(3, 8)]
    public string description;

    [Header("UI")]
    public GameObject exercisePagePrefab;

    [Header("Obstacles")]
    public ObstacleBehaviour[] obstacles;

    [Header("Grades")]
    public ExerciseGrade[] grades;

    /// <summary>
    /// Подбирает префаб результата в зависимости от количества хитов.
    /// Грейды должны быть отсортированы по возрастанию maxHits в инспекторе.
    /// </summary>
    public GameObject GetResultPrefab(int hitCount)
    {
        if (grades == null || grades.Length == 0)
            return null;

        foreach (ExerciseGrade grade in grades)
        {
            if (hitCount <= grade.maxHits)
                return grade.resultPagePrefab;
        }

        return grades[grades.Length - 1].resultPagePrefab;
    }
}
