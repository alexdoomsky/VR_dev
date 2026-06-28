using UnityEngine;

[CreateAssetMenu(
    fileName = "Tutorial",
    menuName = "Tank Trainer/Tutorial")]
    public class TutorialData : ScriptableObject
    {
        public string tutorialName;

        [TextArea(3, 8)]
        public string description;

        public TutorialStep[] steps;

        public int StepCount
        {
            get
            {
                if (steps == null)
                    return 0;

                return steps.Length;
            }
        }

        public TutorialStep GetStep(int index)
        {
            if (steps == null)
                return null;

            if (index < 0 || index >= steps.Length)
                return null;

            return steps[index];
        }
    }
