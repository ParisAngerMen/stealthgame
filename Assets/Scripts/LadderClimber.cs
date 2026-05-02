using StarterAssets;
using UnityEngine;
using UnityEngine.InputSystem;

public class LadderClimber : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("How far from top/bottom to auto dismount")]
    [SerializeField] private float dismountThreshold = 0.3f;

    [Tooltip("How fast player snaps to the ladder")]
    [SerializeField] private float snapSpeed = 10f;

    [Header("References")]
    [SerializeField] private CharacterController controller;
    [SerializeField] private Animator animator;

    // Public
    public bool IsClimbing { get; private set; }

    // Private
    private Ladder _currentLadder;
    private StarterAssetsInputs _input;
    private float _climbProgress;
    private bool _isSnapping;
    private Vector3 _snapTarget;
    private bool _hasAnimator;

    // Animation
    private int _animIDClimbing;
    private int _animIDClimbSpeed;

    private void Start()
    {
        _input = GetComponent<StarterAssetsInputs>();

        if (controller == null)
            controller = GetComponent<CharacterController>();

        _hasAnimator = animator != null;

        _animIDClimbing = Animator.StringToHash("Climbing");
        _animIDClimbSpeed = Animator.StringToHash("ClimbSpeed");
    }

    private void Update()
    {
        if (!IsClimbing) return;

        if (_isSnapping)
        {
            SnapToLadder();
            return;
        }

        Climb();
    }

    // ============================================================
    // START / STOP CLIMBING
    // ============================================================

    public void StartClimbing(Ladder ladder)
    {
        if (IsClimbing) return;

        _currentLadder = ladder;
        IsClimbing = true;

        // Disable character controller so we can move freely
        controller.enabled = false;

        // Figure out if player is closer to bottom or top
        float distToBottom = Vector3.Distance(
            transform.position, ladder.BottomPoint.position);
        float distToTop = Vector3.Distance(
            transform.position, ladder.TopPoint.position);

        if (distToBottom <= distToTop)
        {
            _climbProgress = 0f;
            _snapTarget = ladder.BottomPoint.position;
        }
        else
        {
            _climbProgress = 1f;
            _snapTarget = ladder.TopPoint.position;
        }

        _isSnapping = true;

        // Face the ladder
        Vector3 ladderForward = ladder.transform.forward;
        transform.rotation = Quaternion.LookRotation(-ladderForward);

        // Animation
        if (_hasAnimator)
        {
            animator.SetBool(_animIDClimbing, true);
        }

        // Disable other inputs
        _input.jump = false;
        _input.move = Vector2.zero;

        Debug.Log("Started climbing");
    }

    public void StopClimbing(Vector3 dismountPosition)
    {
        if (!IsClimbing) return;

        IsClimbing = false;
        _currentLadder = null;
        _isSnapping = false;

        // Move player to dismount position
        transform.position = dismountPosition;

        // Re-enable character controller
        controller.enabled = true;

        // Animation
        if (_hasAnimator)
        {
            animator.SetBool(_animIDClimbing, false);
            animator.SetFloat(_animIDClimbSpeed, 0f);
        }

        Debug.Log("Stopped climbing");
    }

    // ============================================================
    // SNAP TO LADDER
    // ============================================================

    private void SnapToLadder()
    {
        transform.position = Vector3.Lerp(
            transform.position,
            _snapTarget,
            Time.deltaTime * snapSpeed);

        if (Vector3.Distance(transform.position, _snapTarget) < 0.05f)
        {
            transform.position = _snapTarget;
            _isSnapping = false;
        }
    }

    // ============================================================
    // CLIMBING
    // ============================================================

    private void Climb()
    {
        if (_currentLadder == null)
        {
            StopClimbing(transform.position);
            return;
        }

        // Get vertical input (W/S or up/down)
        float verticalInput = _input.move.y;

        // Calculate climb speed
        float climbAmount = verticalInput * _currentLadder.ClimbSpeed * Time.deltaTime;

        // Calculate total ladder length
        float ladderLength = Vector3.Distance(
            _currentLadder.BottomPoint.position,
            _currentLadder.TopPoint.position);

        // Update progress (0 = bottom, 1 = top)
        _climbProgress += climbAmount / ladderLength;

        // Check dismount at top
        if (_climbProgress >= 1f)
        {
            _climbProgress = 1f;

            if (_currentLadder.DismountPoint != null)
            {
                StopClimbing(_currentLadder.DismountPoint.position);
            }
            else
            {
                StopClimbing(_currentLadder.TopPoint.position);
            }

            return;
        }

        // Check dismount at bottom
        if (_climbProgress <= 0f)
        {
            _climbProgress = 0f;

            if (_currentLadder.BottomDismountPoint != null)
            {
                StopClimbing(_currentLadder.BottomDismountPoint.position);
            }
            else
            {
                StopClimbing(_currentLadder.BottomPoint.position);
            }

            return;
        }

        // Cancel climbing with jump
        if (_input.jump)
        {
            _input.jump = false;
            Vector3 dropPosition = transform.position;
            dropPosition.y -= 0.5f;
            StopClimbing(dropPosition);
            return;
        }

        // Move player along the ladder
        Vector3 newPosition = Vector3.Lerp(
            _currentLadder.BottomPoint.position,
            _currentLadder.TopPoint.position,
            _climbProgress);

        transform.position = newPosition;

        // Animation
        if (_hasAnimator)
        {
            animator.SetFloat(_animIDClimbSpeed, verticalInput);
        }
    }
}