using System;
using UnityEngine;

[Serializable]
public class ShellData
{
    public string name = "Ѕ–";
    public Projectile prefab;
    public float speed = 250f, damage = 100f, loadTime = 4f;
}

public class GunController : MonoBehaviour, IGun
{
    [SerializeField] Transform muzzle;
    [SerializeField] Transform recoilBarrel;
    [SerializeField] float spawnOffset = 0.5f;
    [SerializeField] float recoilDistance = 0.3f, recoilTime = 1f;

    [Header("Ёффекты выстрела (партиклы, дочерние у Muzzle)")]
    [SerializeField] ParticleSystem[] muzzleEffects;   // вспышка, дым, искры и т.д.

    [SerializeField] ShellData[] shells;

    public GunState State { get; private set; } = GunState.Empty;
    public int SelectedShell { get; private set; }
    public float LoadProgress { get; private set; }
    public ShellData[] Shells => shells;

    public event Action<int> Fired;

    int loadedShell = -1;
    float loadTimer, recoilT = 1f;
    Vector3 barrelStart, recoilDirLocal;
    Collider[] ownColliders;

    void Awake()
    {
        ownColliders = GetComponentsInChildren<Collider>();

        if (recoilBarrel)
        {
            barrelStart = recoilBarrel.localPosition;
            Transform p = recoilBarrel.parent;
            Vector3 a = muzzle.position;
            Vector3 b = muzzle.position + muzzle.forward;
            recoilDirLocal = p ? p.InverseTransformPoint(b) - p.InverseTransformPoint(a)
                               : muzzle.forward;
        }
    }

    public void SelectShell(int i)
    {
        if (State == GunState.Empty) SelectedShell = Mathf.Clamp(i, 0, shells.Length - 1);
    }

    public void StartLoading()
    {
        if (State != GunState.Empty) return;
        loadedShell = SelectedShell;
        loadTimer = 0f;
        State = GunState.Loading;
    }

    public bool TryFire()
    {
        if (State != GunState.Ready) return false;

        var s = shells[loadedShell];
        Vector3 pos = muzzle.position + muzzle.forward * spawnOffset;
        Instantiate(s.prefab, pos, muzzle.rotation).Launch(s.speed, s.damage, ownColliders);

        PlayMuzzleEffects();

        Fired?.Invoke(loadedShell);
        State = GunState.Empty;
        LoadProgress = 0f;
        loadedShell = -1;
        recoilT = 0f;
        return true;
    }

    void PlayMuzzleEffects()
    {
        if (muzzleEffects == null) return;
        foreach (var ps in muzzleEffects)
            if (ps) ps.Play();
    }

    void Update()
    {
        if (State == GunState.Loading)
        {
            loadTimer += Time.deltaTime;
            LoadProgress = Mathf.Clamp01(loadTimer / shells[loadedShell].loadTime);
            if (LoadProgress >= 1f) State = GunState.Ready;
        }

        if (recoilBarrel && recoilT < 1f)
        {
            recoilT = Mathf.Min(1f, recoilT + Time.deltaTime / recoilTime);
            recoilBarrel.localPosition = barrelStart - recoilDirLocal * (recoilDistance * (1f - recoilT));
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!muzzle) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(muzzle.position + muzzle.forward * spawnOffset, 0.1f);
        Gizmos.DrawRay(muzzle.position, muzzle.forward * 3f);
    }
}