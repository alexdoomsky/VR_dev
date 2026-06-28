using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "ExerciseData",
    menuName = "Tank Trainer/Exercise")]
    public class ExerciseData : ScriptableObject
    {
        [Header("Information")]
        public string ExerciseName;

        [TextArea(3,8)]
        public string Description;

        [Header("Tablet")]

        [Tooltip("Страница с описанием упражнения")]
        public GameObject ExercisePagePrefab;

        [Tooltip("Страница со списком упражнений после выхода")]
        public GameObject ExerciseListPrefab;

        [Header("Results")]
        public List<ExerciseResult> Results = new();

        /// <summary>
        /// Возвращает нужный префаб результата.
        /// </summary>
        public GameObject GetResultPrefab(int hitCount)
        {
            foreach (ExerciseResult result in Results)
            {
                if (hitCount <= result.maxHits)
                    return result.resultPagePrefab;
            }

            return null;
        }
    }
