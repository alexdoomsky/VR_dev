using System;
using System.Collections;

public static class GameSettings
{
    public static ControlMode Mode { get; set; } = ControlMode.PC;
    public static bool IsVR => Mode == ControlMode.VR;
}

public class PcProfile : IControlProfile
{
    public ControlMode Mode => ControlMode.PC;
    public string SceneName { get; }
    public PcProfile(string scene) => SceneName = scene;

    public IEnumerator Prepare(Action<bool> done)
    {
        XRManager.StopXR();   // шлем отключаем, дальше только монитор
        done(true);
        yield break;
    }
}

public class VrProfile : IControlProfile
{
    public ControlMode Mode => ControlMode.VR;
    public string SceneName { get; }
    public VrProfile(string scene) => SceneName = scene;

    public IEnumerator Prepare(Action<bool> done)
    {
        if (XRManager.IsRunning) { done(true); yield break; }   // уже запущен автостартом
        yield return XRManager.StartXR(done);
    }
}