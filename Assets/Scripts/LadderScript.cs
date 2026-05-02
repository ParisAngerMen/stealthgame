using UnityEngine;

public class Ladder : MonoBehaviour, IInteractable
{
    [Header("Ladder Settings")]
    [Tooltip("How fast the player climbs")]
    [SerializeField] private float climbSpeed = 3f;

    [Tooltip("The point where the player starts climbing")]
    [SerializeField] private Transform bottomPoint;

    [Tooltip("The point where the player reaches the top")]
    [SerializeField] private Transform topPoint;

    [Tooltip("Where the player stands after reaching the top")]
    [SerializeField] private Transform dismountPoint;

    [Tooltip("Where the player stands after going back down")]
    [SerializeField] private Transform bottomDismountPoint;

    // Private
    private LadderClimber _currentClimber;

    public float ClimbSpeed => climbSpeed;
    public Transform BottomPoint => bottomPoint;
    public Transform TopPoint => topPoint;
    public Transform DismountPoint => dismountPoint;
    public Transform BottomDismountPoint => bottomDismountPoint;

    public void Interact()
    {
        // Find the player's LadderClimber component
        LadderClimber climber = FindFirstObjectByType<LadderClimber>();

        if (climber != null && !climber.IsClimbing)
        {
            climber.StartClimbing(this);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Bottom point
        if (bottomPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(bottomPoint.position, 0.3f);
            Gizmos.DrawLine(bottomPoint.position, bottomPoint.position + Vector3.up * 0.5f);
        }

        // Top point
        if (topPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(topPoint.position, 0.3f);
            Gizmos.DrawLine(topPoint.position, topPoint.position + Vector3.up * 0.5f);
        }

        // Dismount point
        if (dismountPoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(dismountPoint.position, 0.3f);
        }

        // Bottom dismount point
        if (bottomDismountPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(bottomDismountPoint.position, 0.3f);
        }

        // Ladder line
        if (bottomPoint != null && topPoint != null)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawLine(bottomPoint.position, topPoint.position);
        }
    }
}