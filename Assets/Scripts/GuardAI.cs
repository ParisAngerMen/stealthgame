using System;
using System.Collections;
using UnityEngine;
using FOV;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public class GuardAI : MonoBehaviour
{
    private FieldOfView fov;
    private NavMeshAgent agent;
    public Transform[] waypoints;

    private GameObject player;
    
    private enum GuardState
    {
        Patrolling,
        Waiting,
        Chasing,
        Searching,
        Investigating
    }
    
    private GuardState currentState = GuardState.Patrolling;
    
    private int waypointIndex = 0;

    private bool hasSearchPoint = false;
    
    [SerializeField] private float timeToWaitPatrol = 5f;
    [SerializeField] private float rotateDuration = 2f;
    [SerializeField] private float searchArea = 3f;
    private float waitTimer;
    private float searchTimer;
    private float startRotationY;
    private float searchCooldown = 0f;

    private Vector3 lastKnownPos;
    
    private void Awake()
    {
        fov = GetComponent<FieldOfView>();
        agent = GetComponent<NavMeshAgent>();
    }

    void Start()
    {
        agent.SetDestination(waypoints[waypointIndex].position);
    }

    void Update()
    {
        Debug.Log("CurrentState: " + currentState);
        // Update player detection
        var detectedTargets = fov.Field<Transform>("Player");
        
        if (searchCooldown > 0f)
        {
            searchCooldown -= Time.deltaTime;
        }

        switch (currentState)
        {
            case GuardState.Patrolling:
                if (agent.remainingDistance <= agent.stoppingDistance && !agent.pathPending)
                {
                    Debug.Log("reached");
                    currentState = GuardState.Waiting;
                    waitTimer = 0f;
                    agent.isStopped = true;
                }
                break;

            case GuardState.Waiting:
                waitTimer += Time.deltaTime;
                if (waitTimer >= timeToWaitPatrol)
                {
                    agent.isStopped = false;
                    Patrol();
                    currentState = GuardState.Patrolling;
                }
                break;

            case GuardState.Chasing:
                if (player != null)
                {
                    agent.SetDestination(player.transform.position);
                    if (agent.remainingDistance <= agent.stoppingDistance && !agent.pathPending)
                    {
                        Attack();
                    }
                }
                break;

            case GuardState.Searching:
                if (agent.remainingDistance <= agent.stoppingDistance && !agent.pathPending)
                {
                    Debug.Log("Searching for player");
                    currentState = GuardState.Investigating;
                    searchTimer = 0f;
                    startRotationY = transform.eulerAngles.y;
                    agent.isStopped = true;
                }
                break;

            case GuardState.Investigating:
                SearchRadius(10f);                
                break;
        }
    }

    void Patrol()
    {
        if (waypointIndex >= waypoints.Length)
        {
            waypointIndex = 0;
        }
        else
        {
            waypointIndex++;
        }

        if (waypointIndex <= waypoints.Length - 1)
        {
            agent.SetDestination(waypoints[waypointIndex].position);
        }

        // Change to next patrol

    }

    public void Chase(GameObject target)
    {
        player = target;
        currentState = GuardState.Chasing;
        agent.isStopped = false;
    }

    void Attack()
    {
        // Once in range, attack player
    }

    public void SearchForPlayer(Vector3 pos)
    {
        if (agent.pathPending) agent.isStopped = true;
        
        if (searchCooldown > 0f) return;
        if (currentState == GuardState.Searching || currentState == GuardState.Investigating) return;
        
        lastKnownPos = pos;
        currentState = GuardState.Searching;
        agent.isStopped = false;
        agent.SetDestination(lastKnownPos);
    }
    
    public void SearchRadius(float duration)
    {
        searchTimer += Time.deltaTime;

        if (!hasSearchPoint)
        {
            Vector2 randomCircle = Random.insideUnitCircle * searchArea;
            Vector3 searchPoint = new Vector3(
                transform.position.x + randomCircle.x,
                transform.position.y,
                transform.position.z + randomCircle.y);
            
            agent.isStopped = false;
            agent.SetDestination(searchPoint);
            hasSearchPoint = true;

        }
        
        if (agent.remainingDistance <= agent.stoppingDistance && !agent.pathPending)
        {
            hasSearchPoint = false;  // Get new point next frame
        }
        
        Debug.Log("Time: " + searchTimer);

        if (searchTimer >= duration)
        {
            agent.isStopped = true;
            searchCooldown = 2f;
            hasSearchPoint = false;
            currentState = GuardState.Waiting;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            currentState = GuardState.Searching;
            SearchForPlayer(other.transform.position);
            Debug.Log("HearingPLayer");
        }

    }
}