using System;
using System.Collections;
using UnityEngine;

public class DoorScript : MonoBehaviour, IInteractable
{
    [SerializeField] private bool isBlocked;
    [SerializeField] private Vector3 openPosition;
    [SerializeField] private Vector3 closePosition;
    [SerializeField] private float moveSpeed = 2f;

    private bool isOpen = false;
    private bool isMoving = false;

    void Start()
    {
        // Make sure door starts at closed position
        closePosition = transform.localPosition;
        openPosition = new Vector3(transform.localPosition.x, transform.localPosition.y + 5, transform.localPosition.z);
        isOpen = false;
    }

    public void Interact()
    {
        if (isBlocked) return;
        if (isMoving) return;  // Prevent spam clicking while moving

        if (!isOpen)
        {
            StartCoroutine(MoveDoor(openPosition, true));
        }
        else
        {
            StartCoroutine(MoveDoor(closePosition, false));
        }
    }

    IEnumerator MoveDoor(Vector3 targetPosition, bool openState)
    {
        isMoving = true;
        Debug.Log(openState ? "Opening door..." : "Closing door...");

        Vector3 startPosition = transform.localPosition;
        float progress = 0f;

        while (progress < 1f)
        {
            progress += Time.deltaTime * moveSpeed;
            transform.localPosition = Vector3.Lerp(startPosition, targetPosition, progress);
            yield return null;
        }

        // Ensure we end exactly at target position
        transform.localPosition = targetPosition;
        isOpen = openState;
        isMoving = false;

        Debug.Log(openState ? "Door opened" : "Door closed");
    }
}

