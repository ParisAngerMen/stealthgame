using System;
using UnityEngine;
using UnityEngine.AI;

public class GuardHear : MonoBehaviour
{
    private float hearingCooldown;
    [SerializeField] private float hearingCooldownDuration;

    public GameObject guard;
    
    private GuardAI _guardAI;
    private NavMeshAgent agent;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _guardAI =  guard.GetComponent<GuardAI>();
        agent = guard.GetComponent<NavMeshAgent>();   
    }

    // Update is called once per frame
    void Update()
    {
        if (hearingCooldown > 0f) hearingCooldown -= Time.deltaTime;
    }

    private void OnTriggerStay(Collider other)
    {
        // Cooldown prevents spam calling every frame
        if (hearingCooldown > 0f) return;
        if (!other.CompareTag("Player")) Debug.Log("tag: " + other.tag);

        // Dont interrupt chase or investigate

        hearingCooldown = hearingCooldownDuration;
        Debug.Log("Hearing player!");
        agent.SetDestination(other.transform.position);
    }
}
