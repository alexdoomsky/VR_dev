using System;
using System.Collections;
using UnityEngine;

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

    readonly bool skip;

    public VrProfile(string scene, bool skipCheck = false)
    {
        SceneName = scene;
        skip = skipCheck;
    }

    public IEnumerator Prepare(Action<bool> done)
    {
        if (skip)
        {
            Debug.LogWarning("[VrProfile] Проверка VR пропущена (skipVrCheck включён).");
            done(true);
            yield break;
        }

        if (XRManager.IsRunning) { done(true); yield break; }
        yield return XRManager.StartXR(done);
    }
}