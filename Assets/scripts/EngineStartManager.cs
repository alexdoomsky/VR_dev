using System.Collections.Generic;
using UnityEngine;

public class EngineStartManager : MonoBehaviour
{
    // правильный порядок
    private List<string> correctSequence = new List<string>
    {
        "FuelButton",
        "AirButton",
        "IgnitionButton"
    };

    private int currentIndex = 0;

    [Header("Engine State")]
    [SerializeField] private bool engineStarted = false;

    public bool EngineStarted => engineStarted;

    public void RegisterButtonPress(string buttonName)
    {
        if (engineStarted)
        {
            Debug.Log("Engine already started, stop pressing buttons like a maniac");
            return;
        }

        Debug.Log($"Pressed: {buttonName}");

        if (buttonName == correctSequence[currentIndex])
        {
            currentIndex++;

            if (currentIndex >= correctSequence.Count)
            {
                engineStarted = true;

                Debug.Log("ENGINE STARTED");
            }
        }
        else
        {
            Debug.Log("WRONG ORDER - engine not started");

            currentIndex = 0;
        }
    }
}
