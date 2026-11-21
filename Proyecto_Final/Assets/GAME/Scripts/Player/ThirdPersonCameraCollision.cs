using UnityEngine;

public class ThirdPersonCameraCollision : MonoBehaviour
{
    [Header("Deteccion de obstaculos")]
    [SerializeField] private float sphereRadius = 0.25f;
    [SerializeField] private float wallPadding = 0.15f;
    [SerializeField] private LayerMask collisionMask = ~0;

    private void Awake()
    {
        int playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer >= 0)
            collisionMask &= ~(1 << playerLayer);
    }

    public Vector3 ResolvePosition(
        Transform target,
        Vector3 pivot,
        Vector3 desiredPosition,
        out bool obstructed)
    {
        obstructed = false;

        Vector3 offset = desiredPosition - pivot;
        float desiredDistance = offset.magnitude;

        if (desiredDistance <= 0.01f)
            return desiredPosition;

        Vector3 direction = offset / desiredDistance;

        RaycastHit[] hits = Physics.SphereCastAll(
            pivot,
            sphereRadius,
            direction,
            desiredDistance,
            collisionMask,
            QueryTriggerInteraction.Ignore
        );

        float nearestDistance = desiredDistance;
        bool foundObstacle = false;

        foreach (RaycastHit hit in hits)
        {
            Transform hitTransform = hit.collider.transform;

            if (target != null &&
                (hitTransform == target || hitTransform.IsChildOf(target)))
            {
                continue;
            }

            if (hit.distance >= 0f && hit.distance < nearestDistance)
            {
                nearestDistance = hit.distance;
                foundObstacle = true;
            }
        }

        if (!foundObstacle)
            return desiredPosition;

        obstructed = true;

        float safeDistance = Mathf.Clamp(
            nearestDistance - wallPadding,
            0.2f,
            desiredDistance
        );

        return pivot + direction * safeDistance;
    }
}
