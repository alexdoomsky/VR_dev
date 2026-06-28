using UnityEngine;

public class ObstacleBehaviour : MonoBehaviour
{
    [Header("Behaviour")]

    [SerializeField]
    private ObstacleReaction reaction;

    [SerializeField]
    private ObstacleCounter counter;

    [Header("Hide Mode")]

    [SerializeField]
    private GameObject visualRoot;

    private bool counted;

    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.collider.TryGetComponent(out TankCollisionDetector tank))
            return;

        HandleHit();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.TryGetComponent(out TankCollisionDetector tank))
            return;

        if (reaction == ObstacleReaction.HideWhileInside)
        {
            if (visualRoot != null)
                visualRoot.SetActive(false);

            return;
        }

        HandleHit();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.TryGetComponent(out TankCollisionDetector tank))
            return;

        if (reaction == ObstacleReaction.HideWhileInside)
        {
            if (visualRoot != null)
                visualRoot.SetActive(true);
        }
    }

    private void HandleHit()
    {
        if (counted)
            return;

        counted = true;

        if (counter != null)
            counter.RegisterHit();

        switch (reaction)
        {
            case ObstacleReaction.Destroy:

                Destroy(gameObject);
                break;

            case ObstacleReaction.CountOnly:

                break;

            case ObstacleReaction.Ignore:

                break;
        }
    }

    public void ResetObstacle()
    {
        counted = false;

        if (visualRoot != null)
            visualRoot.SetActive(true);
    }
}
