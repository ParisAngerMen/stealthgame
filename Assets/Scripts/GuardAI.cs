using System;
using UnityEngine;
using FOV;
using UnityEngine.AI;

public class GuardAI : MonoBehaviour
{
    private FieldOfView fov;
    private NavMeshAgent agent;
    public Transform[] waypoints;

    private bool isPatrolling = true;
    
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
        if (agent.remainingDistance == agent.stoppingDistance && isPatrolling)
        {
            Debug.Log("reached");
            
            StopPatrol();
        }
        
    }

    void Patrol()
    {
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

    void StopPatrol()
    {
        agent.isStopped = true;
        waitTimer += Time.deltaTime;
        
        if (waitTimer >= timeToWaitPatrol)
        {
            agent.isStopped = false; 
            waitTimer = 0;
        }
    }

    void Chase()
    {
        // Once player spotted, chase player
    }

    void Attack()
    {
        // Once in range, attack player
    }
}