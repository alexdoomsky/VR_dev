using UnityEngine;

[System.Serializable]
public class ExerciseResult
{
    [Tooltip("Максимальное количество касаний для этой оценки")]
    [Min(0)]
    public int maxHits;

    [Tooltip("Префаб страницы планшета")]
    public GameObject resultPagePrefab;
}
