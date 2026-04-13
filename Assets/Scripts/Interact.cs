using System;
using UnityEngine;

public class Interact : MonoBehaviour
{
    public bool canStealthKill = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log("stealth kill: " + canStealthKill);
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            GuardAI guard = other.gameObject.GetComponent<GuardAI>();
            if (!guard.isChasingPlayer)
            {
                canStealthKill = true;
                // Stealth kill logic
            }
        }

        else
        {
            canStealthKill = false;
        }
        
    }
}
