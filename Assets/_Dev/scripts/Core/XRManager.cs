using System;
using System.Collections;
using UnityEngine.XR.Management;

public static class XRManager
{
    static XRManagerSettings Manager => XRGeneralSettings.Instance?.Manager;

    public static bool IsRunning => Manager != null && Manager.activeLoader != null;

    public static IEnumerator StartXR(Action<bool> done)
    {
        var m = Manager;
        if (m == null) { done(false); yield break; }

        if (m.activeLoader == null)
            yield return m.InitializeLoader();

        if (m.activeLoader == null) { done(false); yield break; }

        m.StartSubsystems();
        done(true);
    }

    public static void StopXR()
    {
        var m = Manager;
        if (m == null || !m.isInitializationComplete) return;
        m.StopSubsystems();
        m.DeinitializeLoader();
    }
}