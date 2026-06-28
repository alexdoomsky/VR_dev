using UnityEngine;

[CreateAssetMenu(
    fileName = "Tutorial Step",
    menuName = "Tank Trainer/Tutorial Step")]
    public class TutorialStep : ScriptableObject
    {
        [Header("Identification")]
        public string stepName;

        [TextArea(2, 6)]
        public string developerDescription;

        [Header("Tablet")]
        public GameObject pagePrefab;

        [Header("Success")]

        public TutorialEventType successEvent;

        public TankButton successButton = TankButton.None;

        public int successIntParameter;

        [Header("Failure")]

        [Tooltip("Reset when incorrect button is pressed")]
        public bool resetOnWrongButton;

        [Min(0)]
        public int resetStepIndex;

        [Tooltip("Restart this step if engine stalls")]
        public bool resetOnEngineStall;

        [Header("Behaviour")]

        public bool autoAdvance = true;

        [Min(0)]
        public float autoAdvanceDelay = 0f;

        public bool skippable = true;
    }
