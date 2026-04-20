using StarterAssets;
using UnityEngine;

public class KeyItem : MonoBehaviour, IInteractable
{
    [Header("Key Settings")]
    [Tooltip("Unique ID to match this key with its door")]
    [SerializeField] private string keyID = "key_01";

    [Tooltip("Name shown in UI")]
    [SerializeField] private string keyName = "Rusty Key";

    [Header("Player Settings")] 
    [SerializeField] private ThirdPersonController player;

    public void Interact()
    {
        player.hasKey = true;
        Destroy(gameObject);
        
    }
}