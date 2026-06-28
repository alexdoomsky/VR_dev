using System;

public static class TankEventBus
{
    //--------------------------
    // Engine
    //--------------------------

    public static event Action EngineStarted;
    public static event Action EngineStopped;
    public static event Action EngineStalled;

    //--------------------------
    // Controls
    //--------------------------

    public static event Action<TankButton> OnButtonPressed;

    public static event Action ClutchPressed;

    public static event Action<float> ThrottleChanged;

    //--------------------------
    // Transmission
    //--------------------------

    public static event Action<int> GearChanged;

    //--------------------------
    // Tutorial
    //--------------------------

    public static event Action<string> CheckpointReached;

    //------------------------------------------------

    public static void RaiseEngineStarted()
    {
        EngineStarted?.Invoke();
    }

    public static void RaiseEngineStopped()
    {
        EngineStopped?.Invoke();
    }

    public static void RaiseEngineStalled()
    {
        EngineStalled?.Invoke();
    }

    public static void RaiseButtonPressed(TankButton button)
    {
        OnButtonPressed?.Invoke(button);
    }

    public static void RaiseClutchPressed()
    {
        ClutchPressed?.Invoke();
    }

    public static void RaiseThrottleChanged(float value)
    {
        ThrottleChanged?.Invoke(value);
    }

    public static void RaiseGearChanged(int gear)
    {
        GearChanged?.Invoke(gear);
    }

    public static void RaiseCheckpointReached(string checkpoint)
    {
        CheckpointReached?.Invoke(checkpoint);
    }
}
