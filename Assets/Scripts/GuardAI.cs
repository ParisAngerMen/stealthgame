using System;
using System.Collections;
using UnityEngine;
using FOV;
using UnityEngine.AI;

public class GuardAI : MonoBehaviour
{
    private FieldOfView fov;
    private NavMeshAgent agent;
    public Transform[] waypoints;

    private GameObject player;
    
    private bool isPatrolling = true;
    private bool isSearching = false;
    private bool isRotating = false;

    private bool isChasing = false;
    
    private int waypointIndex = 0;

    [SerializeField] private float timeToWaitPatrol = 5f;
    private float waitTimer;
    
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
        // Update player detection
        var detectedTargets = fov.Field<Transform>("Player");
        // 
        if (agent.remainingDistance <= agent.stoppingDistance && isPatrolling)
        {
            Debug.Log("reached");
            
            StopPatrol();
        }

        if (isSearching)
        {
            Rotate(2);
        }
        
    }

    void Patrol()
    {
        isPatrolling = true;

        // Change to next patrol
        if (waypointIndex >= waypoints.Length)
        {
            waypointIndex = 0;
        }

        // Reset patrol points if final one has been reached
        else
        {
            waypointIndex++;
        }

        if (waypointIndex <= waypoints.Length - 1)
        {
            agent.SetDestination(waypoints[waypointIndex].position);
        }
        
    }

    private void StopPatrol()
    {
        if (!isPatrolling)
        {
            Debug.Log("Stopping patrol");
        
            agent.isStopped = true;

            if (waitTimer < timeToWaitPatrol && !isPatrolling)
            {
                waitTimer += Time.deltaTime;
            }
        
            agent.isStopped = false; 
            isSearching = false;
        
            Patrol(); 
        }


    }

    public void Chase(GameObject target)
    {
        player = target;
        
        isPatrolling = false;
        isChasing = true;
        agent.SetDestination(target.transform.position);
        
        if (agent.remainingDistance <= agent.stoppingDistance)
        {
            Attack();
        }
    }

    void Attack()
    {
        // Once in range, attack player
    }

    public void SearchForPlayer(Vector3 lastKnownPos)
    {
        isChasing = false;
        // Search last known pos
        agent.SetDestination(lastKnownPos);
        
        // Search for a while
        if (!isChasing && agent.remainingDistance <= agent.stoppingDistance && !isSearching)
        {
            isRotating = true;
            Debug.Log("Searching for player");
            Rotate(2);
            //StartCoroutine(StopPatrol());
        }
        
        // Return 
        
    }
    
    public void Rotate(float duration)
    {
        if (isRotating)
        {
            isSearching = true;
            float startRotation = transform.eulerAngles.y;
            float endRotation = startRotation + 360.0f;

            if (waitTimer < duration)
            {
                waitTimer += Time.deltaTime;
                float yRotation = Mathf.Lerp(startRotation, endRotation, duration * Time.deltaTime) % 360.0f;
                transform.eulerAngles = new Vector3(transform.eulerAngles.x, yRotation, transform.eulerAngles.z);
                Debug.Log("Time: " + waitTimer);
            }

            if (waitTimer >= duration)
            {
                isSearching = false;
                isPatrolling = false;
                StopPatrol(); 
                isRotating = false;
            }
        
            Debug.Log("Search playlist: " + waitTimer);
        }
       

    }
}