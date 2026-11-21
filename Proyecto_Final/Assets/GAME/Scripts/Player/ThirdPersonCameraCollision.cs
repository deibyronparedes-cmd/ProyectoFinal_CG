using UnityEngine;

[DefaultExecutionOrder(100)]
public class ThirdPersonCameraCollision : MonoBehaviour
{
    [Header("Referencia del jugador")]
    [SerializeField] private Transform target;

    [Header("Configuracion de camara")]
    [SerializeField] private float targetHeight = 1.4f;
    [SerializeField] private float sphereRadius = 0.25f;
    [SerializeField] private float wallPadding = 0.15f;

    [Header("Deteccion de obstaculos")]
    [SerializeField] private LayerMask collisionMask = ~0;

    private void Awake()
    {
        int playerLayer = LayerMask.NameToLayer("Player");

        if (playerLayer >= 0)
        {
            collisionMask &= ~(1 << playerLayer);
        }
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        Vector3 pivot =
            target.position + Vector3.up * targetHeight;

        Vector3 cameraPosition = transform.position;

        Vector3 direction = cameraPosition - pivot;

        float distance = direction.magnitude;

        if (distance <= 0.01f)
            return;

        direction.Normalize();

        if (Physics.SphereCast(
            pivot,
            sphereRadius,
            direction,
            out RaycastHit hit,
            distance,
            collisionMask,
            QueryTriggerInteraction.Ignore))
        {
            float safeDistance = Mathf.Max(
                0.25f,
                hit.distance - wallPadding
            );

            transform.position =
                pivot + direction * safeDistance;
        }
    }
}
