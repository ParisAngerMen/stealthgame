using System;
using UnityEngine;
using FOV;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public class GuardAI : MonoBehaviour, IInteractable
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
    
    // Timers
    private float waitTimer = 0f;
    private float searchTimer = 0f;
    private float searchCooldown = 0f;
    private float hearingCooldown = 0f;
    private float attackTimer = 0f;

    // Search
    private bool hasSearchPoint = false;
    private Vector3 lastKnownPos;

    // State
    private enum GuardState
    {
        Patrolling,
        Waiting,
        Chasing,
        Searching,
        Investigating
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
    }

    private void Update()
    {
        // Tick cooldowns
        if (searchCooldown > 0f) searchCooldown -= Time.deltaTime;

        // Update FOV detection every frame
        fov.Field<Transform>("Player");

        HandleState();
        Debug.Log("chase timer: " + chaseTimer);
    }

    /*
    public void OnTriggerStay(Collider other)
    {
        // Cooldown prevents spam calling every frame
        if (hearingCooldown > 0f) return;
        if (!other.CompareTag("Player")) Debug.Log("tag: " + other.tag);

        // Dont interrupt chase or investigate
        if (currentState == GuardState.Chasing) return;
        if (currentState == GuardState.Investigating) return;

        hearingCooldown = hearingCooldownDuration;
        Debug.Log("Hearing player!");
        agent.SetDestination(other.transform.position);
    }
    */
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
        // Path not ready yet, wait
        if (agent.pathPending) return;

        if (agent.remainingDistance <= agent.stoppingDistance)
        {
            Debug.Log("Reached waypoint, waiting...");
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
            Debug.Log("Done waiting, patrolling...");
            agent.isStopped = false;
            NextWaypoint();
            currentState = GuardState.Patrolling;
        }
    }

    private void HandleChasing()
    {
        chaseTimer += Time.deltaTime;

        // Player went missing
        if (player == null || chaseTimer >= chaseDuration)
        {
            isChasingPlayer = false;
            agent.isStopped = true;
            Debug.Log("Lost player reference, searching...");
            currentState = GuardState.Investigating;
            chaseTimer = 0f;
            return;
        }

        agent.SetDestination(player.transform.position);

        attackTimer += Time.deltaTime;

        
        // Path not ready yet, wait
        if (agent.pathPending) return;

        if (agent.remainingDistance <= agent.stoppingDistance)
        {
            Debug.Log("can attack");
            Attack();
        }
    }

    private void HandleSearching()
    {
        // Path not ready yet, wait
        if (agent.pathPending) return;

        if (agent.remainingDistance <= agent.stoppingDistance)
        {
            Debug.Log("Reached last known position, investigating...");
            searchTimer = 0f;
            hasSearchPoint = false;
            agent.isStopped = true;
            currentState = GuardState.Investigating;
        }
    }

    private void HandleInvestigating()
    {
        searchTimer += Time.deltaTime;

        // Pick a new random nearby point to walk to
        if (!hasSearchPoint)
        {
            Vector3 searchPoint;

            // Make sure point is on NavMesh
            if (GetRandomNavMeshPoint(out searchPoint))
            {
                agent.isStopped = false;
                agent.SetDestination(searchPoint);
                hasSearchPoint = true;
                Debug.Log("Going to search point: " + searchPoint);
            }
            else
            {
                // Could not find valid point, try again next frame
                Debug.LogWarning("Could not find valid NavMesh search point, retrying...");
            }
        }

        // Reached search point, get a new one next frame
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            hasSearchPoint = false;
        }

        // Search time is up, return to patrol
        if (searchTimer >= searchDuration)
        {
            Debug.Log("Search complete, returning to patrol...");
            agent.isStopped = false;
            hasSearchPoint = false;
            searchCooldown = searchCooldownDuration;
            NextWaypoint();
            currentState = GuardState.Patrolling;
        }
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

        isChasingPlayer = true;
        player = target;
        agent.isStopped = false;
        currentState = GuardState.Chasing;
        Debug.Log("Chasing player!");
    }

    private void Attack()
    {
        Debug.Log("Attack Timer: " + attackTimer);
        
        if (attackTimer >= attackCooldown)
        {
            Debug.Log("Attacking player!");
            attackTimer = 0;
            
            if (player.GetComponent<PlayerHealth>() == null) return;
            else
            {
                player.GetComponent<PlayerHealth>().TakeDamage(attackDamage);
            }
        }
        
        // Once in range, attack player
    }

    public void SearchForPlayer(Vector3 pos)
    {
        if (searchCooldown > 0f) return;
        if (currentState == GuardState.Searching) return;
        if (currentState == GuardState.Investigating) return;

        lastKnownPos = pos;
        agent.isStopped = false;
        agent.SetDestination(lastKnownPos);
        currentState = GuardState.Searching;
        Debug.Log("Searching for player at: " + lastKnownPos);
    }

    private bool GetRandomNavMeshPoint(out Vector3 result)
    {
        // Try a few times to find a valid point on the NavMesh
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

    public void TakeDamage(float damage, Transform objectPos)
    {
        agent.isStopped = true;
        SearchForPlayer(objectPos.position);
        curHealth -= damage;

        if (curHealth <= 0)
        {
            Die();
        }
    }

    public void Interact()
    {
        if (!isChasingPlayer)
        {
            Debug.Log("Die");
            Die();
        }
    }

    private void Die()
    {
        Destroy(this.gameObject);
    }
}