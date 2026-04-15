using System;
using UnityEngine;
using UnityEngine.Rendering;

public class Interact : MonoBehaviour, IInteractable
{
    void IInteractable.Interact()
    {
        Debug.Log("Interact");
    }
}
