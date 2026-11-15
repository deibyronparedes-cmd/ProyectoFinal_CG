
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class AgentController : MonoBehaviour
{
    // ESTADOS DEL AGENTE
    private enum AgentState
    {
        Patrol,
        Alert,
        Chase,
        Attack,
        Return
    }

    [Header("Referencias")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform patrolPointsParent;
    [SerializeField] private Renderer visualRenderer;
    [SerializeField] private AudioSource audioSource;

    [Header("Animaciones")]
    [SerializeField] private Animator animator;
    [SerializeField] private string speedParameter = "Speed";
    [SerializeField] private string attackParameter = "Attack";

    [Header("Percepcion")]
    [SerializeField] private float detectionRange = 15f;
    [SerializeField] private float fieldOfView = 160f;
    [SerializeField] private float eyeHeight = 1.4f;
    [SerializeField] private float targetHeight = 0.9f;
    [SerializeField] private LayerMask visionMask = ~0;

    [Header("Movimiento")]
    [SerializeField] private float patrolSpeed = 2.5f;
    [SerializeField] private float chaseSpeed = 4f;
    [SerializeField] private float rotationSpeed = 8f;

    [Header("Combate")]
    [SerializeField] private float attackRange = 1.8f;
    [SerializeField] private int damage = 10;
    [SerializeField] private float attackCooldown = 1.5f;

    [Header("Tiempo de estados")]
    [SerializeField] private float alertDuration = 0.5f;
    [SerializeField] private float investigationDuration = 4f;
    [SerializeField] private float loseSightDuration = 5f;

    [Header("Sonidos")]
    [SerializeField] private AudioClip alertClip;
    [SerializeField] private AudioClip attackClip;
    [SerializeField] private AudioClip returnClip;

    [Header("Colores de estado")]
    [SerializeField] private Color patrolColor = Color.white;
    [SerializeField] private Color alertColor = Color.yellow;
    [SerializeField] private Color chaseColor = Color.red;
    [SerializeField] private Color attackColor = new Color(1f, 0.4f, 0f);
    [SerializeField] private Color returnColor = Color.cyan;

    // COMPONENTES
    private NavMeshAgent agent;
    private PlayerHealth playerHealth;

    // PATRULLA
    private Transform[] patrolPoints = new Transform[0];
    private int patrolIndex;

    // ESTADO ACTUAL
    private AgentState currentState;
    private bool stateInitialized;

    // TEMPORIZADORES
    private float stateStartTime;
    private float lastSeenTime;
    private float nextAttackTime;

    // ULTIMA POSICION CONOCIDA DEL JUGADOR
    private Vector3 lastKnownPlayerPosition;

    // INICIALIZACION
    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (visualRenderer == null)
            visualRenderer = GetComponentInChildren<Renderer>();

        if (patrolPointsParent != null)
        {
            patrolPoints = new Transform[patrolPointsParent.childCount];

            for (int i = 0; i < patrolPoints.Length; i++)
            {
                patrolPoints[i] = patrolPointsParent.GetChild(i);
            }
        }
    }

    private void Start()
    {
        if (player == null)
        {
            Debug.LogError("AgentController: falta asignar Player.");
            enabled = false;
            return;
        }

        playerHealth = player.GetComponent<PlayerHealth>();

        if (playerHealth == null)
        {
            Debug.LogError("El Player no tiene PlayerHealth.");
            enabled = false;
            return;
        }

        if (!agent.isOnNavMesh)
        {
            Debug.LogError("El agente no esta sobre un NavMesh.");
            enabled = false;
            return;
        }

        if (patrolPoints.Length == 0)
        {
            Debug.LogWarning("No hay puntos de patrulla asignados.");
        }

        lastKnownPlayerPosition = player.position;
        lastSeenTime = -loseSightDuration;

        agent.speed = patrolSpeed;

        ChangeState(AgentState.Patrol);
    }

    // ACTUALIZACION PRINCIPAL
    private void Update()
    {
        if (player == null || !agent.isOnNavMesh)
            return;

        bool seesPlayer = CanSeePlayer();

        if (seesPlayer)
        {
            lastKnownPlayerPosition = player.position;
            lastSeenTime = Time.time;
        }

        switch (currentState)
        {
            case AgentState.Patrol:
                UpdatePatrol(seesPlayer);
                break;

            case AgentState.Alert:
                UpdateAlert(seesPlayer);
                break;

            case AgentState.Chase:
                UpdateChase(seesPlayer);
                break;

            case AgentState.Attack:
                UpdateAttack(seesPlayer);
                break;

            case AgentState.Return:
                UpdateReturn(seesPlayer);
                break;
        }

        // Actualizar animaciones continuamente
        UpdateAnimation();
    }

    // ESTADO 1: PATRULLA
    private void UpdatePatrol(bool seesPlayer)
    {
        if (seesPlayer)
        {
            ChangeState(AgentState.Alert);
            return;
        }

        if (patrolPoints.Length == 0)
            return;

        if (HasReachedDestination())
        {
            patrolIndex = (patrolIndex + 1) % patrolPoints.Length;

            SetDestination(patrolPoints[patrolIndex].position);
        }
    }

    // ESTADO 2: ALERTA E INVESTIGACION
    private void UpdateAlert(bool seesPlayer)
    {
        agent.isStopped = false;
        agent.speed = chaseSpeed;

        SetDestination(lastKnownPlayerPosition);

        if (Time.time - stateStartTime < alertDuration)
            return;

        if (Time.time - lastSeenTime <= loseSightDuration)
        {
            ChangeState(AgentState.Chase);
            return;
        }

        bool reachedPosition = HasReachedDestination();

        bool timeExpired =
            Time.time - stateStartTime >= investigationDuration;

        if (reachedPosition || timeExpired)
        {
            ChangeState(AgentState.Return);
        }
    }

    // ESTADO 3: PERSECUCION
    private void UpdateChase(bool seesPlayer)
    {
        if (seesPlayer)
        {
            float distance = Vector3.Distance(
                transform.position,
                player.position
            );

            if (distance <= attackRange)
            {
                ChangeState(AgentState.Attack);
                return;
            }

            agent.isStopped = false;
            agent.speed = chaseSpeed;

            SetDestination(player.position);
        }
        else
        {
            if (Time.time - lastSeenTime >= loseSightDuration)
            {
                ChangeState(AgentState.Return);
                return;
            }

            SetDestination(lastKnownPlayerPosition);
        }
    }

    // ESTADO 4: ATAQUE
    private void UpdateAttack(bool seesPlayer)
    {
        if (!seesPlayer)
        {
            ChangeState(AgentState.Chase);
            return;
        }

        float distance = Vector3.Distance(
            transform.position,
            player.position
        );

        if (distance > attackRange * 1.2f)
        {
            ChangeState(AgentState.Chase);
            return;
        }

        LookAtPlayer();

        // Atacar respetando el tiempo de enfriamiento
        if (Time.time >= nextAttackTime)
        {
            // Aplicar daño
            playerHealth.TakeDamage(damage);

            // Activar animacion de ataque
            if (animator != null)
            {
                animator.SetFloat(speedParameter, 0f);
                animator.ResetTrigger(attackParameter);
                animator.SetTrigger(attackParameter);
            }

            // Reproducir sonido
            PlaySound(attackClip);

            Debug.Log(
                "Agente ataco al jugador. Daño: " + damage
            );

            // Programar siguiente ataque
            nextAttackTime = Time.time + attackCooldown;
        }
    }

    // ESTADO 5: RETORNO
    private void UpdateReturn(bool seesPlayer)
    {
        if (seesPlayer)
        {
            ChangeState(AgentState.Alert);
            return;
        }

        if (patrolPoints.Length == 0)
        {
            ChangeState(AgentState.Patrol);
            return;
        }

        if (HasReachedDestination())
        {
            ChangeState(AgentState.Patrol);
        }
    }

    // DETECCION DEL JUGADOR
    private bool CanSeePlayer()
    {
        if (player == null || playerHealth == null)
            return false;

        Vector3 direction =
            player.position - transform.position;

        float distance = direction.magnitude;

        if (distance > detectionRange || distance < 0.01f)
            return false;

        Vector3 flatDirection = direction;
        flatDirection.y = 0f;

        float angle = Vector3.Angle(
            transform.forward,
            flatDirection
        );

        if (angle > fieldOfView * 0.5f)
            return false;

        Vector3 origin =
            transform.position + Vector3.up * eyeHeight;

        CharacterController characterController =
            player.GetComponent<CharacterController>();

        Vector3 targetPoint;

        if (characterController != null)
        {
            targetPoint = player.TransformPoint(
                characterController.center +
                Vector3.up * (characterController.height * 0.35f)
            );
        }
        else
        {
            targetPoint = player.position +
                          Vector3.up * targetHeight;
        }

        Vector3 rayDirection = targetPoint - origin;

        RaycastHit[] hits = Physics.RaycastAll(
            origin,
            rayDirection.normalized,
            rayDirection.magnitude + 0.1f,
            visionMask,
            QueryTriggerInteraction.Ignore
        );

        float nearestDistance = Mathf.Infinity;
        RaycastHit nearestHit = default;
        bool foundHit = false;

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.transform == transform ||
                hit.collider.transform.IsChildOf(transform))
            {
                continue;
            }

            if (hit.distance < nearestDistance)
            {
                nearestDistance = hit.distance;
                nearestHit = hit;
                foundHit = true;
            }
        }

        if (!foundHit)
            return false;

        PlayerHealth detectedPlayer =
            nearestHit.collider.GetComponentInParent<PlayerHealth>();

        bool detected = detectedPlayer == playerHealth;

        Debug.DrawLine(
            origin,
            nearestHit.point,
            detected ? Color.green : Color.red
        );

        return detected;
    }

    // CAMBIO DE ESTADOS
    private void ChangeState(AgentState newState)
    {
        if (stateInitialized && currentState == newState)
            return;

        currentState = newState;
        stateInitialized = true;

        stateStartTime = Time.time;

        Debug.Log("Agente: " + currentState);

        agent.isStopped = newState == AgentState.Attack;

        switch (newState)
        {
            case AgentState.Patrol:

                agent.speed = patrolSpeed;

                SetPatrolDestination();

                ApplyColor(patrolColor);

                break;

            case AgentState.Alert:

                agent.speed = patrolSpeed;
                agent.isStopped = false;

                SetDestination(lastKnownPlayerPosition);

                ApplyColor(alertColor);

                PlaySound(alertClip);

                break;

            case AgentState.Chase:

                agent.speed = chaseSpeed;
                agent.isStopped = false;

                ApplyColor(chaseColor);

                break;

            case AgentState.Attack:

                agent.isStopped = true;

                nextAttackTime = Time.time;

                ApplyColor(attackColor);

                break;

            case AgentState.Return:

                agent.speed = patrolSpeed;

                SetPatrolDestination();

                ApplyColor(returnColor);

                PlaySound(returnClip);

                break;
        }
    }

    // PUNTOS DE PATRULLA
    private void SetPatrolDestination()
    {
        if (patrolPoints.Length == 0)
            return;

        agent.isStopped = false;

        SetDestination(patrolPoints[patrolIndex].position);
    }

    private void SetDestination(Vector3 destination)
    {
        if (agent.isOnNavMesh)
        {
            agent.SetDestination(destination);
        }
    }

    private bool HasReachedDestination()
    {
        return !agent.pathPending &&
               agent.hasPath &&
               agent.remainingDistance <=
               agent.stoppingDistance + 0.15f;
    }

    // ORIENTACION HACIA EL JUGADOR
    private void LookAtPlayer()
    {
        Vector3 direction =
            player.position - transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.01f)
            return;

        Quaternion targetRotation =
            Quaternion.LookRotation(direction);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    // ACTUALIZACION DE ANIMACIONES
    private void UpdateAnimation()
    {
        if (animator == null)
            return;

        float animationSpeed = 0f;

        if (currentState == AgentState.Attack)
        {
            animationSpeed = 0f;
        }
        else if (agent.velocity.magnitude > 0.1f)
        {
            if (currentState == AgentState.Chase)
            {
                animationSpeed = 1f;
            }
            else
            {
                animationSpeed = 0.5f;
            }
        }

        animator.SetFloat(
            speedParameter,
            animationSpeed,
            0.1f,
            Time.deltaTime
        );
    }

    // CAMBIOS VISUALES
    private void ApplyColor(Color color)
    {
        if (visualRenderer != null)
        {
            visualRenderer.material.color = color;
        }
    }

    // SONIDOS
    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}
