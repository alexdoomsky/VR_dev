
using System.Collections.Generic;
using UnityEngine;

public class ExerciseTriggerRelay : MonoBehaviour
{
    public enum TriggerType
    {
        ExerciseZone,
        StartZone
    }

    [Header("Settings")]
    [SerializeField] private TriggerType triggerType;
    [SerializeField] private string requiredTag = "Tank";

    private TankShootingExercise exercise;

    // Один танк может иметь несколько дочерних коллайдеров.
    // Не считаем повторные входы разных коллайдеров отдельными входами танка.
    private readonly Dictionary<Transform, HashSet<Collider>> occupants = new();

    public void Initialize(TankShootingExercise owner)
    {
        exercise = owner;
    }

    private void Reset()
    {
        Collider col = GetComponent<Collider>();

        if (col != null)
            col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (exercise == null)
            return;

        Transform tankRoot = FindTankRoot(other.transform);

        if (tankRoot == null)
            return;

        if (!occupants.TryGetValue(tankRoot, out HashSet<Collider> colliders))
        {
            colliders = new HashSet<Collider>();
            occupants.Add(tankRoot, colliders);
        }

        bool wasOutside = colliders.Count == 0;

        colliders.Add(other);

        if (!wasOutside)
            return;

        NotifyEntered();
    }

    private void OnTriggerExit(Collider other)
    {
        if (exercise == null)
            return;

        Transform tankRoot = FindTankRoot(other.transform);

        if (tankRoot == null)
            return;

        if (!occupants.TryGetValue(tankRoot, out HashSet<Collider> colliders))
            return;

        colliders.Remove(other);

        if (colliders.Count > 0)
            return;

        occupants.Remove(tankRoot);

        NotifyExited();
    }

    private Transform FindTankRoot(Transform current)
    {
        while (current != null)
        {
            if (current.CompareTag(requiredTag))
                return current;

            current = current.parent;
        }

        return null;
    }

    private void NotifyEntered()
    {
        switch (triggerType)
        {
            case TriggerType.ExerciseZone:
                exercise.NotifyExerciseZoneEntered();
                break;

            case TriggerType.StartZone:
                exercise.NotifyStartZoneEntered();
                break;
        }
    }

    private void NotifyExited()
    {
        if (triggerType != TriggerType.ExerciseZone)
            return;

        exercise.NotifyExerciseZoneExited();
    }
}
