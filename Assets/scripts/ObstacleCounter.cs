using UnityEngine;

public class ObstacleCounter : MonoBehaviour
{
    public int HitCount { get; private set; }

    public void RegisterHit()
    {
        HitCount++;

        Debug.Log($"Obstacle hit. Total: {HitCount}");
    }

    public void ResetCounter()
    {
        HitCount = 0;
    }
}
