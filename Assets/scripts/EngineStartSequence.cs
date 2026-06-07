using System.Collections.Generic;
using UnityEngine;

public class EngineStartSequence : MonoBehaviour
{
    [SerializeField]
    private TankTelemetry telemetry;

    private readonly List<string> correctSequence = new()
    {
        "FuelButton",
        "AirButton",
        "IgnitionButton"
    };

    private int currentIndex;

    public bool SequenceCompleted =>
    telemetry != null &&
    telemetry.CanStartEngine;

    public void RegisterButtonPress(string buttonName)
    {
        if (telemetry == null)
        {
            Debug.LogError("TankTelemetry not assigned");

            return;
        }

        Debug.Log($"Pressed: {buttonName}");

        if (buttonName != correctSequence[currentIndex])
        {
            Debug.Log("WRONG ORDER");

            ResetSequence();

            return;
        }

        switch (buttonName)
        {
            case "FuelButton":
                telemetry.FuelEnabled = true;
                break;

            case "AirButton":
                telemetry.AirEnabled = true;
                break;

            case "IgnitionButton":
                telemetry.IgnitionEnabled = true;
                break;
        }

        currentIndex++;

        if (currentIndex >= correctSequence.Count)
        {
            telemetry.CanStartEngine = true;

            Debug.Log("START SEQUENCE COMPLETED");
        }
    }

    private void ResetSequence()
    {
        currentIndex = 0;

        telemetry.FuelEnabled = false;
        telemetry.AirEnabled = false;
        telemetry.IgnitionEnabled = false;
        telemetry.CanStartEngine = false;
    }
}
