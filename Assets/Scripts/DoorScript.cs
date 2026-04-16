using UnityEngine;
using UnityEngine.Rendering.Universal;

public class DoorScript : MonoBehaviour, IInteractable
{
    [SerializeField] private bool isBlocked;
    private bool isOpen;
    [SerializeField] private Vector3 openPosition;
    [SerializeField] private Vector3 closePosition;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        isOpen = false;
    }

    public void Interact()
    {
        if (!isBlocked)
        {
            if (!isOpen)
            {
                // Open door
                Debug.Log("Open door");
                isOpen = true;
            }

            else
            {
                // Close door
                Debug.Log("Close door");
                isOpen = false;

            }
        }
    }
}
