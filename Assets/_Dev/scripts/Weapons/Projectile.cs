
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
    [SerializeField] float explosionRadius = 3f, explosionForce = 2000f;

    [Header("Ёффекты попадани€")]
    [SerializeField] GameObject hotMetalEffect;
    [SerializeField] GameObject smokeEffect;
    [SerializeField] float hotMetalLifetime = 3f;
    [SerializeField] float smokeLifetime = 8f;
    [SerializeField] float effectOffset = 0.05f;

    [Header("∆изнь снар€да")]
    [SerializeField] bool destroyOnHit = false;
    [SerializeField] float lifetime = 0f;
    [SerializeField] int maxAlive = 30;
    [SerializeField] float ignoreOwnTime = 0.3f;

    static readonly Collider[] hits = new Collider[32];
    static readonly Queue<Projectile> alive = new();

    Rigidbody rb;
    Collider[] myCols, ownCols;
    float damage, age;
    bool hit, ignoreRestored;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    public void Launch(float speed, float dmg, Collider[] ignore)
    {
        damage = dmg;
        SetVelocity(transform.forward * speed);

        myCols = GetComponentsInChildren<Collider>();
        ownCols = ignore;
        SetIgnore(true);

        if (lifetime > 0f)
            Destroy(gameObject, lifetime);

        alive.Enqueue(this);

        while (alive.Count > maxAlive)
        {
            var old = alive.Dequeue();
            if (old)
                Destroy(old.gameObject);
        }
    }

    void SetIgnore(bool value)
    {
        if (ownCols == null) return;

        foreach (var a in myCols)
        {
            foreach (var b in ownCols)
            {
                if (a && b)
                    Physics.IgnoreCollision(a, b, value);
            }
        }
    }

    void SetVelocity(Vector3 v)
    {
        #if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = v;
        #else
        rb.velocity = v;
        #endif
    }

    Vector3 Velocity =>
    #if UNITY_6000_0_OR_NEWER
    rb.linearVelocity;
    #else
    rb.velocity;
    #endif

    void Update()
    {
        if (ignoreRestored) return;

        age += Time.deltaTime;

        if (age >= ignoreOwnTime)
        {
            SetIgnore(false);
            ignoreRestored = true;
        }
    }

    void FixedUpdate()
    {
        if (hit) return;

        var v = Velocity;

        if (v.sqrMagnitude > 1f)
            rb.MoveRotation(Quaternion.LookRotation(v));
    }

    void OnCollisionEnter(Collision c)
    {
        if (hit) return;

        hit = true;

        ContactPoint cp = c.GetContact(0);

        Vector3 point = cp.point + cp.normal * effectOffset;
        Quaternion rot = Quaternion.LookRotation(cp.normal);

        // Ёффекты попадани€
        PlayHit(point, rot);

        var net = GetComponent<ProjectileNet>();
        if (net)
            net.RpcHit(point, rot);

        // ѕр€мое попадание по NPC
        SplineTankMovement directNpc =
        c.collider.GetComponentInParent<SplineTankMovement>();

        if (directNpc != null)
        {
            directNpc.DestroyTank();

            Debug.Log(
                $"[Projectile] Direct hit on NPC: {directNpc.name}",
                directNpc
            );
        }

        // ќбработка взрыва и сплеш-урона остальных объектов
        int n = Physics.OverlapSphereNonAlloc(
            transform.position,
            explosionRadius,
            hits
        );

        for (int i = 0; i < n; i++)
        {
            var col = hits[i];

            if (!col)
                continue;

            // NPC не получают сплеш-урон и взрывную силу
            if (col.GetComponentInParent<SplineTankMovement>() != null)
                continue;

            if (col.attachedRigidbody && col.attachedRigidbody != rb)
            {
                col.attachedRigidbody.AddExplosionForce(
                    explosionForce,
                    transform.position,
                    explosionRadius
                );
            }

            col.GetComponentInParent<IDamageable>()?.TakeDamage(damage);
        }

        if (destroyOnHit)
            Destroy(gameObject);
    }

    public void PlayHit(Vector3 point, Quaternion rot)
    {
        SpawnEffect(hotMetalEffect, point, rot, hotMetalLifetime);
        SpawnEffect(smokeEffect, point, rot, smokeLifetime);
    }

    static void SpawnEffect(
        GameObject prefab,
        Vector3 pos,
        Quaternion rot,
        float life
    )
    {
        if (!prefab) return;

        var fx = Instantiate(prefab, pos, rot);
        Destroy(fx, life);
    }
}
