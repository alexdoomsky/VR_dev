using System;
using UnityEngine;
public enum GunState { Empty, Loading, Ready }
public enum ControlMode { PC, VR }

public interface IAimable
{
    float Yaw { get; }
    float Pitch { get; }
    void AddAimDelta(float yaw, float pitch);
}

public interface IGun
{
    GunState State { get; }
    int SelectedShell { get; }
    float LoadProgress { get; }
    event Action<int> Fired;
    void SelectShell(int index);
    void StartLoading();
    bool TryFire();
}

public interface IDamageable
{
    void TakeDamage(float amount);
}

// —тратеги€ запуска режима: что подготовить и какую сцену грузить
public interface IControlProfile
{
    ControlMode Mode { get; }
    string SceneName { get; }
    System.Collections.IEnumerator Prepare(Action<bool> done);
}

public interface IAimTarget
{
    void AimAt(Vector3 worldDirection);   // целитьс€ по направлению
    void AimAtPoint(Vector3 worldPoint);  // целитьс€ в точку мира
}
public interface ICameraView
{
    string ViewName { get; }
    Camera ViewCamera { get; }
    void OnEnter();   // вид стал активным
    void OnExit();    // вид выключен
}

public interface ILookView : ICameraView
{
    void ApplyLook(Quaternion worldRotation);
    float SensitivityScale { get; }
    void Zoom(float scroll);
}
public interface ILobbyEvents
{
    void OnLobbyCreated(bool ok);
    void OnLobbyJoined(bool ok, string error);
    void OnLobbyMemberChanged();
    void OnLobbyChatMsg(string from, string msg);
    void OnGameStart(ulong hostSteamId);
    void OnPartnerLeft();
}