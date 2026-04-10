using System.Collections.Generic;
using UnityEngine;

public class EngineStartManager : MonoBehaviour
{
    // правильный порядок (ИМЕНА ОБЪЕКТОВ В СЦЕНЕ)
    private List<string> correctSequence = new List<string>
    {
        "FuelButton",
        "AirButton",
        "IgnitionButton"
    };

    private int currentIndex = 0;
    private bool engineStarted = false;

    public void RegisterButtonPress(string buttonName)
    {
        if (engineStarted)
        {
            Debug.Log("Engine already started, stop pressing buttons like a maniac");
            return;
        }

        Debug.Log($"Pressed: {buttonName}");

        // проверка правильности
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

            // сброс
            currentIndex = 0;
        }
    }
}
