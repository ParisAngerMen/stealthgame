using System;
using UnityEngine;
using FOV;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public class GuardAI : MonoBehaviour, IInteractable, IDamageable, IAlertable, IHackable
{
    #region Variables
    private FieldOfView fov;
    private NavMeshAgent agent;
    private GameObject player;

    [Header("Waypoints")]
    public Transform[] waypoints;
    private int waypointIndex = 0;

    [Header("Timers")]
    [SerializeField] private float timeToWaitPatrol = 5f;
    [SerializeField] private float searchDuration = 10f;
    [SerializeField] private float searchCooldownDuration = 2f;
    [SerializeField] private float chaseDuration = 5f;
    [SerializeField] private float hearingCooldownDuration = 1f;

    [Header("Search")]
    [SerializeField] private float searchArea = 5f;
    public float chaseTimer = 0f;
    public bool isChasingPlayer = false;

    [Header("Attack")]
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float attackCooldown = 5f;

    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    private float curHealth;

    [Header("Stun")]
    [Tooltip("Visual indicator when stunned")]
    [SerializeField] private GameObject stunEffect;
    [Tooltip("How long after stun before guard becomes alert")]
    [SerializeField] private float stunRecoveryAlertTime = 2f;

    // Timers
    private float waitTimer = 0f;
    private float searchTimer = 0f;
    private float searchCooldown = 0f;
    private float hearingCooldown = 0f;
    private float attackTimer = 0f;

    // Search
    private bool hasSearchPoint = false;
    private Vector3 lastKnownPos;

    // Stun
    private bool _isHacked = false;
    private float _stunTimer = 0f;
    private float _stunDuration = 0f;
    private GuardState _preStunState;

    // State
    private enum GuardState
    {
        Patrolling,
        Waiting,
        Chasing,
        Searching,
        Investigating,
        Stunned,
        StunRecovering
    }

    private GuardState currentState = GuardState.Patrolling;
    #endregion

    #region Unity Methods
    private void Awake()
    {
        fov = GetComponent<FieldOfView>();
        if (fov == null) Debug.LogError("FieldOfView component missing on " + gameObject.name);

        agent = GetComponent<NavMeshAgent>();
        if (agent == null) Debug.LogError("NavMeshAgent component missing on " + gameObject.name);
    }

    private void Start()
    {
        if (waypoints == null || waypoints.Length == 0)
        {
            return;
        }

        GoToWaypoint();
        curHealth = maxHealth;

        if (stunEffect != null)
            stunEffect.SetActive(false);
    }

    private void Update()
    {
        // Tick cooldowns
        if (searchCooldown > 0f) searchCooldown -= Time.deltaTime;

        // Don't do anything while stunned
        if (currentState == GuardState.Stunned)
        {
            HandleStunned();
            return;
        }

        if (currentState == GuardState.StunRecovering)
        {
            HandleStunRecovery();
            return;
        }

        // Update FOV detection every frame
        fov.Field<Transform>("Player");

        HandleState();
    }
    #endregion

    #region State Machine
    private void HandleState()
    {
        switch (currentState)
        {
            case GuardState.Patrolling:
                HandlePatrolling();
                break;

            case GuardState.Waiting:
                HandleWaiting();
                break;

            case GuardState.Chasing:
                HandleChasing();
                break;

            case GuardState.Searching:
                HandleSearching();
                break;

            case GuardState.Investigating:
                HandleInvestigating();
                break;
        }
    }

    private void HandlePatrolling()
    {
        if (agent.pathPending) return;

        if (agent.remainingDistance <= agent.stoppingDistance)
        {
            waitTimer = 0f;
            agent.isStopped = true;
            currentState = GuardState.Waiting;
        }
    }

    private void HandleWaiting()
    {
        waitTimer += Time.deltaTime;

        if (waitTimer >= timeToWaitPatrol)
        {
            agent.isStopped = false;
            NextWaypoint();
            currentState = GuardState.Patrolling;
        }
    }

    private void HandleChasing()
    {
        chaseTimer += Time.deltaTime;

        if (player == null || chaseTimer >= chaseDuration)
        {
            isChasingPlayer = false;
            agent.isStopped = true;
            currentState = GuardState.Investigating;
            chaseTimer = 0f;
            return;
        }

        agent.SetDestination(player.transform.position);
        attackTimer += Time.deltaTime;

        if (agent.pathPending) return;

        if (agent.remainingDistance <= agent.stoppingDistance)
        {
            Attack();
        }
    }

    private void HandleSearching()
    {
        if (agent.pathPending) return;

        if (agent.remainingDistance <= agent.stoppingDistance)
        {
            searchTimer = 0f;
            hasSearchPoint = false;
            agent.isStopped = true;
            currentState = GuardState.Investigating;
        }
    }

    private void HandleInvestigating()
    {
        searchTimer += Time.deltaTime;

        if (!hasSearchPoint)
        {
            Vector3 searchPoint;

            if (GetRandomNavMeshPoint(out searchPoint))
            {
                agent.isStopped = false;
                agent.SetDestination(searchPoint);
                hasSearchPoint = true;
            }
        }

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            hasSearchPoint = false;
        }

        if (searchTimer >= searchDuration)
        {
            agent.isStopped = false;
            hasSearchPoint = false;
            searchCooldown = searchCooldownDuration;
            NextWaypoint();
            currentState = GuardState.Patrolling;
        }
    }

    // ============================================================
    // STUN STATES
    // ============================================================

    private void HandleStunned()
    {
        _stunTimer += Time.deltaTime;

        if (_stunTimer >= _stunDuration)
        {
            StartStunRecovery();
        }
    }

    private void HandleStunRecovery()
    {
        _stunTimer += Time.deltaTime;

        if (_stunTimer >= stunRecoveryAlertTime)
        {
            FinishStunRecovery();
        }
    }

    private void StartStunRecovery()
    {
        _stunTimer = 0f;
        currentState = GuardState.StunRecovering;

        if (stunEffect != null)
            stunEffect.SetActive(false);

        Debug.Log($"{name} is recovering from stun...");
    }

    private void FinishStunRecovery()
    {
        _isHacked = false;
        agent.isStopped = false;

        // After recovering from stun, guard is suspicious
        // and investigates the area
        searchTimer = 0f;
        hasSearchPoint = false;
        currentState = GuardState.Investigating;

        Debug.Log($"{name} has recovered from stun! Investigating area...");
    }

    #endregion

    #region Actions
    private void GoToWaypoint()
    {
        if (waypoints.Length == 0) return;

        agent.isStopped = false;
        agent.SetDestination(waypoints[waypointIndex].position);
    }

    private void NextWaypoint()
    {
        waypointIndex++;

        if (waypointIndex >= waypoints.Length)
        {
            waypointIndex = 0;
        }

        GoToWaypoint();
    }

    public void Chase(GameObject target)
    {
        if (target == null) return;
        if (currentState == GuardState.Stunned) return;
        if (currentState == GuardState.StunRecovering) return;

        isChasingPlayer = true;
        player = target;
        agent.isStopped = false;
        currentState = GuardState.Chasing;
    }

    private void Attack()
    {
        if (attackTimer >= attackCooldown)
        {
            attackTimer = 0;

            if (player == null) return;

            if (player.TryGetComponent<PlayerHealth>(out PlayerHealth health))
            {
                health.TakeDamage(attackDamage);
            }
        }
    }

    public void SearchForPlayer(Vector3 pos)
    {
        if (currentState == GuardState.Stunned) return;
        if (currentState == GuardState.StunRecovering) return;
        if (searchCooldown > 0f) return;
        if (currentState == GuardState.Searching) return;
        if (currentState == GuardState.Investigating) return;

        lastKnownPos = pos;
        agent.isStopped = false;
        agent.SetDestination(lastKnownPos);
        currentState = GuardState.Searching;
    }

    private bool GetRandomNavMeshPoint(out Vector3 result)
    {
        for (int i = 0; i < 5; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * searchArea;
            Vector3 randomPoint = new Vector3(
                transform.position.x + randomCircle.x,
                transform.position.y,
                transform.position.z + randomCircle.y
            );

            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPoint, out hit, searchArea, NavMesh.AllAreas))
            {
                result = hit.position;
                return true;
            }
        }

        result = transform.position;
        return false;
    }
    #endregion

    #region Interface Implementations

    // ============================================================
    // IHackable
    // ============================================================

    /// <summary>
    /// Called when the guard is hit by the EMP gun.
    /// Completely freezes the guard for the duration.
    /// </summary>
    public void Hack(float duration)
    {
        // Can be stunned again even if already stunned (resets timer)
        _isHacked = true;
        _stunTimer = 0f;
        _stunDuration = duration;

        // Remember what state we were in
        if (currentState != GuardState.Stunned && currentState != GuardState.StunRecovering)
        {
            _preStunState = currentState;
        }

        // Stop everything
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        agent.ResetPath();
        isChasingPlayer = false;
        chaseTimer = 0f;

        currentState = GuardState.Stunned;

        // Show stun effect
        if (stunEffect != null)
            stunEffect.SetActive(true);

        Debug.Log($"{name} has been stunned for {duration} seconds!");
    }

    public bool IsHacked => _isHacked;

    // ============================================================
    // IDamageable
    // ============================================================

    /// <summary>
    /// Called when the guard is hit by any weapon (pistol, sniper, etc).
    /// </summary>
    public void TakeDamage(float damage)
    {
        // Taking damage breaks stun
        if (currentState == GuardState.Stunned || currentState == GuardState.StunRecovering)
        {
            BreakStun();
        }

        curHealth -= damage;
        Debug.Log($"{name} took {damage} damage! Health: {curHealth}/{maxHealth}");

        if (curHealth <= 0f)
        {
            Die();
            return;
        }

        if (currentState != GuardState.Chasing)
        {
            agent.isStopped = true;
            searchTimer = 0f;
            hasSearchPoint = false;
            currentState = GuardState.Investigating;
        }
    }

    public void TakeDamage(float damage, Vector3 sourcePosition)
    {
        if (currentState == GuardState.Stunned || currentState == GuardState.StunRecovering)
        {
            BreakStun();
        }

        curHealth -= damage;
        Debug.Log($"{name} took {damage} damage from {sourcePosition}! Health: {curHealth}/{maxHealth}");

        if (curHealth <= 0f)
        {
            Die();
            return;
        }

        SearchForPlayer(sourcePosition);
    }

    public void TakeDamage(float damage, Transform objectPos)
    {
        TakeDamage(damage, objectPos.position);
    }

    /// <summary>
    /// Breaks the guard out of stun when taking damage.
    /// </summary>
    private void BreakStun()
    {
        _isHacked = false;
        _stunTimer = 0f;
        agent.isStopped = false;

        if (stunEffect != null)
            stunEffect.SetActive(false);

        Debug.Log($"{name} stun broken by damage!");
    }

    // ============================================================
    // IAlertable
    // ============================================================

    /// <summary>
    /// Called when a nearby gunshot is heard or a security camera spots the player.
    /// Does nothing if the guard is stunned.
    /// </summary>
    public void Alert(Vector3 position)
    {
        // Can't be alerted while stunned
        if (currentState == GuardState.Stunned) return;
        if (currentState == GuardState.StunRecovering) return;

        Debug.Log($"{name} alerted! Investigating position: {position}");

        if (currentState == GuardState.Chasing) return;

        SearchForPlayer(position);
    }

    // ============================================================
    // IInteractable
    // ============================================================

    /// <summary>
    /// Called when the player interacts with the guard.
    /// Easier to stealth kill when stunned.
    /// </summary>
    public void Interact()
    {
        // Can always interact if stunned
        if (currentState == GuardState.Stunned || currentState == GuardState.StunRecovering)
        {
            Debug.Log($"{name} was neutralized while stunned!");
            Die();
            return;
        }

        // Normal interact only works if not chasing
        if (!isChasingPlayer)
        {
            Debug.Log($"{name} was neutralized via interaction");
            Die();
        }
    }

    #endregion

    #region Health

    private void Die()
    {
        Debug.Log($"{name} has died!");

        if (stunEffect != null)
            stunEffect.SetActive(false);

        Destroy(gameObject);
    }

    #endregion
}