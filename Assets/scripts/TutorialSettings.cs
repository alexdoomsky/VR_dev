using UnityEngine;

[CreateAssetMenu(
    fileName = "Tutorial Settings",
    menuName = "Tank Trainer/Tutorial Settings",
    order = 2)]
    public class TutorialSettings : ScriptableObject
    {
        [Header("Tutorial")]

        [Tooltip("Tutorial shown when scene starts")]
        public TutorialData defaultTutorial;

        [Tooltip("Can player skip tutorial")]
        public bool allowSkip = true;

        [Tooltip("Start tutorial automatically")]
        public bool startAutomatically = true;
    }
