using TMPro;
using Unity.Cinemachine;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM 
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
    [RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM 
    [RequireComponent(typeof(PlayerInput))]
#endif
    public class ThirdPersonController : MonoBehaviour
    {
        // ============================================================
        // INSPECTOR FIELDS
        // ============================================================

        [Header("Movement")]
        [Tooltip("Move speed of the character in m/s")]
        public float MoveSpeed = 2.0f;

        [Tooltip("Sprint speed of the character in m/s")]
        public float SprintSpeed = 5.335f;

        [Tooltip("Crouch speed of the character in m/s")]
        public float CrouchSpeed = 1f;

        [Tooltip("How fast the character turns to face movement direction")]
        [Range(0.0f, 0.3f)]
        public float RotationSmoothTime = 0.12f;

        [Tooltip("Acceleration and deceleration")]
        public float SpeedChangeRate = 10.0f;

        [Header("Jumping & Gravity")]
        [Tooltip("The height the player can jump")]
        public float JumpHeight = 1.2f;

        [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
        public float Gravity = -15.0f;

        [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
        public float JumpTimeout = 0.50f;

        [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
        public float FallTimeout = 0.15f;

        [Header("Grounded Check")]
        [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
        public bool Grounded = true;

        [Tooltip("Useful for rough ground")]
        public float GroundedOffset = -0.14f;

        [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
        public float GroundedRadius = 0.28f;

        [Tooltip("What layers the character uses as ground")]
        public LayerMask GroundLayers;

        [Header("Noise")]
        [Tooltip("Multiplier for noise radius based on speed")]
        public float noiseMultiplier = 2.0f;
        public CapsuleCollider _noiseCollider;

        [Header("Camera")]
        [Tooltip("The follow target set in the Cinemachine Camera that the camera will follow")]
        public GameObject CinemachineCameraTarget;

        [SerializeField] private Camera PlayerCamera;

        [Tooltip("How far in degrees can you move the camera up")]
        public float TopClamp = 70.0f;

        [Tooltip("How far in degrees can you move the camera down")]
        public float BottomClamp = -30.0f;

        [Tooltip("Additional degrees to override the camera. Useful for fine tuning camera position when locked")]
        public float CameraAngleOverride = 0.0f;

        [Tooltip("For locking the camera position on all axis")]
        public bool LockCameraPosition = false;

        [Tooltip("Camera sensitivity")]
        public float CameraSensitivity = 1.0f;

        [Header("Camera Follow")]
        [Tooltip("How fast the camera rotates to follow the player movement direction")]
        [Range(0f, 10f)]
        public float CameraFollowSpeed = 2.0f;

        [Tooltip("How long the player must be moving before the camera starts following")]
        public float CameraFollowDelay = 0.8f;

        [Tooltip("How much the player must move the mouse to override camera follow")]
        public float CameraLookOverrideThreshold = 0.5f;

        [Tooltip("Enable or disable the auto camera follow behavior")]
        public bool AutoCameraFollow = true;

        [Header("Audio")]
        public AudioSource AudioFootsteps;
        public AudioSource LandingAudio;
        public AudioSource AudioFoley;
        public AudioClip LandingAudioClip;
        public AudioClip[] FootstepAudioClips;

        [Range(0, 1)]
        public float FootstepAudioVolume = 0.5f;

        [Header("Interaction")]
        [Tooltip("Layer mask for interactable objects")]
        [SerializeField] private LayerMask interactMask;

        [Tooltip("Transform used as origin for interaction detection")]
        [SerializeField] private Transform interactTransform;

        [Tooltip("The UI prompt shown when near an interactable")]
        [SerializeField] private GameObject interactPrompt;
        [SerializeField] private TextMeshProUGUI interactText;

        [Tooltip("The canvas that contains the interact prompt")]
        [SerializeField] private Canvas promptCanvas;

        [Tooltip("World space offset for the prompt above the interactable")]
        [SerializeField] private Vector3 promptOffset = new Vector3(0f, 1.5f, 0f);

        [Tooltip("How often interaction detection runs (in seconds)")]
        public float interactCooldown = 0.1f;

        [Tooltip("Radius of the interaction sphere")]
        public float interactRadius = 3.0f;
        
        [Header("Weapon")]
        [SerializeField] private Pistol pistol;

        [SerializeField] private GameObject pistolSprite;
        
        [Header("Aiming")]
        [Tooltip("Camera used when aiming")]
        [SerializeField] private CinemachineCamera aimCamera;

        [Tooltip("How fast the player rotates toward aim direction")]
        [Range(0f, 50f)]
        [SerializeField] private float aimRotationSpeed = 20f;

        [Tooltip("Layer mask for aim raycasting")]
        [SerializeField] private LayerMask aimMask;

        [Tooltip("Crosshair UI element")]
        [SerializeField] private RectTransform crosshair;

        [Tooltip("Max aim raycast distance")]
        [SerializeField] private float aimDistance = 100f;
        
        private bool _isAiming = false;
        private Vector3 _aimPoint;
        public bool hasKey = false;

        // ============================================================
        // PUBLIC FIELDS
        // ============================================================

        public bool isMakingNoise;
        public GameObject _mainCamera;

        // ============================================================
        // PRIVATE FIELDS
        // ============================================================

        // Camera
        private float _cinemachineTargetYaw;
        private float _cinemachineTargetPitch;
        private float _cameraFollowTimer = 0f;
        private bool _isPlayerLooking = false;
        private float _lookOverrideTimer = 0f;
        private const float _lookOverrideDuration = 1.5f;
        private const float _threshold = 0.01f;

        // Movement
        private float _speed;
        private float _animationBlend;
        private float _targetRotation = 0.0f;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;
        private float noiseRadius;

        // Jumping
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

        // Interaction
        private float _interactTimer = 0.0f;
        private Transform _currentInteractTarget;
        private IInteractable _currentInteractable;

        // Animation IDs
        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;

        // Components
        private Animator _animator;
        private CharacterController _controller;
        private StarterAssetsInputs _input;
        private bool _hasAnimator;

#if ENABLE_INPUT_SYSTEM 
        private PlayerInput _playerInput;
#endif

        // ============================================================
        // PROPERTIES
        // ============================================================

        private bool IsCurrentDeviceMouse
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return _playerInput.currentControlScheme == "KeyboardMouse";
#else
                return false;
#endif
            }
        }

        // ============================================================
        // UNITY CALLBACKS
        // ============================================================

        private void Awake()
        {
            if (_mainCamera == null)
            {
                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            }
        }

        private void Start()
        {
            _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;

            _hasAnimator = TryGetComponent(out _animator);
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<StarterAssetsInputs>();

#if ENABLE_INPUT_SYSTEM
            _playerInput = GetComponent<PlayerInput>();
#else
            Debug.LogError("Starter Assets package is missing dependencies.");
#endif

            AssignAnimationIDs();

            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;
        }

        private void Update()
        {
            _hasAnimator = TryGetComponent(out _animator);

            JumpAndGravity();
            GroundedCheck();
            Crouch();
            Aim();
            Attack();
            Move();
            EmitNoise();
            FindClosestInteractable();
            HandleInteractInput();
        }

        private void LateUpdate()
        {
            CameraRotation();
            UpdatePromptPosition();
        }

        // ============================================================
        // ANIMATION
        // ============================================================

        private void AssignAnimationIDs()
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        }

        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                if (AudioFootsteps != null)
                    AudioFootsteps.Play();

                if (AudioFoley != null)
                    AudioFoley.Play();
            }
        }

        private void OnLand(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                if (LandingAudio != null)
                    LandingAudio.Play();
            }
        }

        // ============================================================
        // MOVEMENT
        // ============================================================

        private void Move()
        {
            float targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed;
            targetSpeed = _input.crouch ? CrouchSpeed : targetSpeed;

            if (_input.move == Vector2.zero) targetSpeed = 0.0f;

            float currentHorizontalSpeed = new Vector3(
                _controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

            float speedOffset = 0.1f;
            float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

            if (currentHorizontalSpeed < targetSpeed - speedOffset ||
                currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude,
                    Time.deltaTime * SpeedChangeRate);
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }

            _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
            if (_animationBlend < 0.01f) _animationBlend = 0f;

            Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

            if (_input.move != Vector2.zero)
            {
                _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
                                  _mainCamera.transform.eulerAngles.y;

                // Only rotate toward movement if NOT aiming
                if (!_isAiming)
                {
                    float rotation = Mathf.SmoothDampAngle(
                        transform.eulerAngles.y, _targetRotation,
                        ref _rotationVelocity, RotationSmoothTime);

                    transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
                }
            }
            else
            {
                // Only softly follow camera when NOT aiming
                if (!_isAiming)
                {
                    _targetRotation = _cinemachineTargetYaw;

                    float rotation = Mathf.SmoothDampAngle(
                        transform.eulerAngles.y, _targetRotation,
                        ref _rotationVelocity, RotationSmoothTime * 3f);

                    transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
                }
            }
           
            Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

            _controller.Move(
                targetDirection.normalized * (_speed * Time.deltaTime) +
                new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

            if (_hasAnimator)
            {
                _animator.SetFloat(_animIDSpeed, _animationBlend);
                _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
            }

            noiseRadius = targetSpeed <= CrouchSpeed ? 0 : _speed * noiseMultiplier;
        }

        private void Crouch()
        {
            if (Grounded)
            {
                if (_input.crouch)
                {
                    _speed = CrouchSpeed;
                }
            }
            else
            {
                _input.crouch = false;
            }
        }

        private void Aim()
        {
            // Get the center of the screen
            Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);

            // Raycast from the center of the screen into the world
            Ray ray = PlayerCamera.ScreenPointToRay(screenCenter);
    
            if (Physics.Raycast(ray, out RaycastHit hit, aimDistance, aimMask))
            {
                _aimPoint = hit.point;
            }
            else
            {
                _aimPoint = ray.GetPoint(aimDistance);
            }

            if (_input.aim)
            {
                pistolSprite.SetActive(true);
                
                _isAiming = true;

                // Enable aim camera
                if (aimCamera != null)
                    aimCamera.Priority = 20;

                // Show crosshair
                if (crosshair != null)
                    crosshair.gameObject.SetActive(true);

                // Rotate player toward aim direction
                Vector3 aimDirection = _aimPoint - transform.position;
                aimDirection.y = 0f; // Keep rotation horizontal
                aimDirection.Normalize();

                if (aimDirection.sqrMagnitude > 0.01f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(aimDirection);
                    transform.rotation = Quaternion.Slerp(
                        transform.rotation,
                        targetRotation,
                        Time.deltaTime * aimRotationSpeed);
                }
            }
            else
            {
                pistolSprite.SetActive(false);
                _isAiming = false;

                // Disable aim camera
                if (aimCamera != null)
                    aimCamera.Priority = 0;

                // Hide crosshair
                if (crosshair != null)
                    crosshair.gameObject.SetActive(false);
            }
        }

        private void Attack()
        {
            if (_input.attack && _input.aim)
            {
                if (pistol != null)
                {
                    Debug.Log("shoot");
                    pistol.TryShoot();
                }            
            }
            
            _input.attack  = false;

        }

        // ============================================================
        // JUMPING & GRAVITY
        // ============================================================

        private void JumpAndGravity()
        {
            if (Grounded)
            {
                _fallTimeoutDelta = FallTimeout;

                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, false);
                    _animator.SetBool(_animIDFreeFall, false);
                }

                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = -2f;
                }

                if (_input.jump && _jumpTimeoutDelta <= 0.0f)
                {
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDJump, true);
                    }
                }

                if (_jumpTimeoutDelta >= 0.0f)
                {
                    _jumpTimeoutDelta -= Time.deltaTime;
                }
            }
            else
            {
                _jumpTimeoutDelta = JumpTimeout;

                if (_fallTimeoutDelta >= 0.0f)
                {
                    _fallTimeoutDelta -= Time.deltaTime;
                }
                else
                {
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDFreeFall, true);
                    }
                }

                _input.jump = false;
            }

            if (_verticalVelocity < _terminalVelocity)
            {
                _verticalVelocity += Gravity * Time.deltaTime;
            }
        }

        // ============================================================
        // GROUNDED CHECK
        // ============================================================

        private void GroundedCheck()
        {
            Vector3 spherePosition = new Vector3(
                transform.position.x,
                transform.position.y - GroundedOffset,
                transform.position.z);

            Grounded = Physics.CheckSphere(
                spherePosition, GroundedRadius, GroundLayers,
                QueryTriggerInteraction.Ignore);

            if (_hasAnimator)
            {
                _animator.SetBool(_animIDGrounded, Grounded);
            }
        }

        // ============================================================
        // CAMERA
        // ============================================================

        private void CameraRotation()
        {
            bool playerIsLooking = _input.look.sqrMagnitude >= CameraLookOverrideThreshold;
            bool playerIsMoving = _input.move != Vector2.zero;

            // Apply manual look input
            if (_input.look.sqrMagnitude >= _threshold && !LockCameraPosition)
            {
                float deltaTimeMultiplier = IsCurrentDeviceMouse
                    ? CameraSensitivity
                    : Time.deltaTime * CameraSensitivity;

                _cinemachineTargetYaw += _input.look.x * deltaTimeMultiplier;
                _cinemachineTargetPitch += _input.look.y * deltaTimeMultiplier;
            }

            // Track if player is actively looking to override auto follow
            if (playerIsLooking)
            {
                _lookOverrideTimer = _lookOverrideDuration;
                _isPlayerLooking = true;
            }
            else
            {
                _lookOverrideTimer -= Time.deltaTime;
                if (_lookOverrideTimer <= 0f)
                {
                    _lookOverrideTimer = 0f;
                    _isPlayerLooking = false;
                }
            }

            // Auto follow player movement direction
            if (AutoCameraFollow && playerIsMoving && !_isPlayerLooking)
            {
                _cameraFollowTimer += Time.deltaTime;

                if (_cameraFollowTimer >= CameraFollowDelay)
                {
                    _cinemachineTargetYaw = Mathf.LerpAngle(
                        _cinemachineTargetYaw,
                        transform.eulerAngles.y,
                        Time.deltaTime * CameraFollowSpeed);
                }
            }
            else if (!playerIsMoving)
            {
                _cameraFollowTimer = 0f;
            }

            // Clamp angles
            _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            // Apply rotation to Cinemachine target
            CinemachineCameraTarget.transform.rotation = Quaternion.Euler(
                _cinemachineTargetPitch + CameraAngleOverride,
                _cinemachineTargetYaw,
                0.0f);
        }

        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }

        // ============================================================
        // NOISE
        // ============================================================

        private void EmitNoise()
        {
            if (_noiseCollider != null)
                _noiseCollider.radius = noiseRadius;
        }

        // ============================================================
        // INTERACTION
        // ============================================================

        /// <summary>
        /// Scans for the closest interactable every frame.
        /// </summary>
        private void FindClosestInteractable()
        {
            Collider[] colliders = Physics.OverlapSphere(
                interactTransform.position + interactTransform.forward * interactRadius,
                interactRadius,
                interactMask);

            float closestDistance = Mathf.Infinity;
            _currentInteractable = null;
            _currentInteractTarget = null;

            foreach (Collider col in colliders)
            {
                if (!col.TryGetComponent<IInteractable>(out IInteractable interactable))
                    continue;

                float distance = Vector3.Distance(transform.position, col.transform.position);

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    _currentInteractable = interactable;
                    _currentInteractTarget = col.transform;
                }

                if (col.gameObject.layer == 7)
                {
                    interactText.text = "Stealth Kill";

                    if (col.gameObject.GetComponent<GuardAI>().isChasingPlayer)
                    {
                        interactPrompt.SetActive(false);
                    }
                }

                else
                {
                    interactText.text = "Interact";
                }
            }

            if (interactPrompt != null)
                interactPrompt.SetActive(_currentInteractTarget != null);
        }

        /// <summary>
        /// Handles the interact button input with cooldown.
        /// </summary>
        private void HandleInteractInput()
        {
            _interactTimer += Time.deltaTime;

            if (_currentInteractable != null && _input.interact && _interactTimer >= interactCooldown)
            {
                
                _currentInteractable.Interact();
                
                _interactTimer = 0.0f;
            }


            _input.interact = false;
        }

        /// <summary>
        /// Updates the interact prompt position every LateUpdate
        /// so it stays locked to the interactable object on screen.
        /// </summary>
        private void UpdatePromptPosition()
        {
            if (_currentInteractTarget == null || interactPrompt == null)
            {
                if (interactPrompt != null)
                    interactPrompt.SetActive(false);
                return;
            }

            Vector3 worldPos = _currentInteractTarget.position + promptOffset;
            Vector3 screenPos = PlayerCamera.WorldToScreenPoint(worldPos);

            if (screenPos.z <= 0f)
            {
                interactPrompt.SetActive(false);
                return;
            }

            interactPrompt.SetActive(true);

            RectTransform promptRect = interactPrompt.GetComponent<RectTransform>();
            RectTransform canvasRect = promptCanvas.GetComponent<RectTransform>();
            Camera uiCamera = promptCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : promptCanvas.worldCamera;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect,
                    screenPos,
                    uiCamera,
                    out Vector2 localPoint))
            {
                promptRect.anchoredPosition = localPoint;
            }
        }

        // ============================================================
        // GIZMOS
        // ============================================================

        private void OnDrawGizmosSelected()
        {
            // Grounded check sphere
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

            Gizmos.color = Grounded ? transparentGreen : transparentRed;

            Gizmos.DrawSphere(
                new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z),
                GroundedRadius);

            // Noise radius sphere
            Gizmos.color = new Color(1.0f, 1.0f, 0.0f, 0.15f);
            Gizmos.DrawSphere(
                new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z),
                noiseRadius);
        }

        private void OnDrawGizmos()
        {
            if (interactTransform == null) return;

            Vector3 center = interactTransform.position + interactTransform.forward * interactRadius;
            Collider[] colliders = Physics.OverlapSphere(center, interactRadius, interactMask);

            Gizmos.color = colliders.Length > 0 ? Color.green : Color.red;
            Gizmos.DrawWireSphere(center, interactRadius);

            foreach (Collider col in colliders)
            {
                if (col.GetComponent<IInteractable>() != null)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
                }
            }
        }
    }
}
