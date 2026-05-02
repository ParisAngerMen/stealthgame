using UnityEngine;

public class SecurityCamera : MonoBehaviour, IHackable
{
    [Header("Rotation")]
    [SerializeField] private Vector3[] lookPoints;
    [SerializeField] private float rotateSpeed = 2f;
    [SerializeField] private float waitTime = 3f;

    [Header("Detection")]
    [SerializeField] private float viewDistance = 15f;
    [SerializeField] private float viewAngle = 45f;
    [SerializeField] private LayerMask detectionMask;
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private string playerTag = "Player";

    [Header("Alert")]
    [SerializeField] private float detectionTime = 1.5f;
    [SerializeField] private float alertRadius = 30f;
    [SerializeField] private LayerMask enemyMask;

    [Header("Visuals")]
    [SerializeField] private Light spotLight;
    [SerializeField] private Color normalColor = Color.green;
    [SerializeField] private Color suspiciousColor = Color.yellow;
    [SerializeField] private Color alertColor = Color.red;
    [SerializeField] private Color hackedColor = new Color(0f, 0.8f, 1f, 1f);

    [Header("Audio")]
    [SerializeField] private AudioSource cameraAudio;
    [SerializeField] private AudioClip rotateClip;
    [SerializeField] private AudioClip alertClip;
    [SerializeField] private AudioClip hackedClip;
    [SerializeField] private AudioClip rebootClip;

    // Patrol
    private int _currentPointIndex = 0;
    private float _waitTimer = 0f;
    private bool _isWaiting = false;
    private Quaternion _targetRotation;

    // Detection
    private float _detectionTimer = 0f;
    private bool _isAlerted = false;
    private bool _playerInSight = false;
    private Transform _playerTransform;

    // Hacking
    private bool _isHacked = false;
    private float _hackTimer = 0f;
    private float _hackDuration = 0f;
    private float _rebootProgress = 0f;

    public bool IsHacked => _isHacked;

    private enum CameraState
    {
        Patrolling,
        Suspicious,
        Alerted,
        Hacked,
        Rebooting
    }

    private CameraState _state = CameraState.Patrolling;

    // ============================================================
    // UNITY CALLBACKS
    // ============================================================

    private void Start()
    {
        if (lookPoints.Length == 0)
        {
            Debug.LogWarning($"{name}: No look points assigned!");
            return;
        }

        _targetRotation = Quaternion.Euler(lookPoints[0]);
        transform.rotation = _targetRotation;

        if (spotLight != null)
        {
            spotLight.color = normalColor;
            spotLight.spotAngle = viewAngle * 2f;
            spotLight.range = viewDistance;
        }
    }

    private void Update()
    {
        if (lookPoints.Length == 0) return;

        switch (_state)
        {
            case CameraState.Hacked:
                UpdateHacked();
                break;

            case CameraState.Rebooting:
                UpdateRebooting();
                break;

            default:
                DetectPlayer();
                UpdateState();
                UpdateVisuals();

                if (!_isAlerted)
                    Patrol();
                else
                    TrackPlayer();
                break;
        }
    }

    // ============================================================
    // HACKING
    // ============================================================

    public void Hack(float duration)
    {
        _isHacked = true;
        _hackDuration = duration;
        _hackTimer = 0f;
        _state = CameraState.Hacked;

        // Reset detection
        _detectionTimer = 0f;
        _isAlerted = false;
        _playerInSight = false;

        // Visual feedback
        if (spotLight != null)
        {
            spotLight.color = hackedColor;
            spotLight.intensity *= 0.3f;
        }

        // Audio
        if (cameraAudio != null && hackedClip != null)
        {
            cameraAudio.clip = hackedClip;
            cameraAudio.Play();
        }

        Debug.Log($"{name} has been hacked for {duration} seconds!");
    }

    private void UpdateHacked()
    {
        _hackTimer += Time.deltaTime;

        // Flicker the light
        if (spotLight != null)
        {
            float flicker = Mathf.Sin(Time.time * 10f) > 0 ? 0.3f : 0.1f;
            spotLight.intensity = flicker;
        }

        if (_hackTimer >= _hackDuration)
        {
            StartReboot();
        }
    }

    private void StartReboot()
    {
        _state = CameraState.Rebooting;
        _rebootProgress = 0f;

        if (cameraAudio != null && rebootClip != null)
        {
            cameraAudio.clip = rebootClip;
            cameraAudio.Play();
        }

        Debug.Log($"{name} is rebooting...");
    }

    private void UpdateRebooting()
    {
        _rebootProgress += Time.deltaTime;

        // Reboot takes 3 seconds
        float rebootDuration = 3f;

        // Light slowly comes back on
        if (spotLight != null)
        {
            float t = _rebootProgress / rebootDuration;
            spotLight.intensity = Mathf.Lerp(0f, 1f, t);
            spotLight.color = Color.Lerp(hackedColor, normalColor, t);
        }

        if (_rebootProgress >= rebootDuration)
        {
            FinishReboot();
        }
    }

    private void FinishReboot()
    {
        _isHacked = false;
        _state = CameraState.Patrolling;
        _detectionTimer = 0f;
        _isAlerted = false;

        // Reset visuals
        if (spotLight != null)
        {
            spotLight.intensity = 1f;
            spotLight.color = normalColor;
        }

        // Reset patrol
        _currentPointIndex = 0;
        _targetRotation = Quaternion.Euler(lookPoints[0]);
        _isWaiting = false;

        Debug.Log($"{name} has rebooted and resumed patrol");
    }

    // ============================================================
    // PATROL
    // ============================================================

    private void Patrol()
    {
        if (_isWaiting)
        {
            _waitTimer += Time.deltaTime;

            if (_waitTimer >= waitTime)
            {
                _isWaiting = false;
                _waitTimer = 0f;
                NextPoint();
            }

            return;
        }

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            _targetRotation,
            Time.deltaTime * rotateSpeed);

        if (Quaternion.Angle(transform.rotation, _targetRotation) < 0.5f)
        {
            transform.rotation = _targetRotation;
            _isWaiting = true;
            _waitTimer = 0f;
        }
    }

    private void NextPoint()
    {
        _currentPointIndex = (_currentPointIndex + 1) % lookPoints.Length;
        _targetRotation = Quaternion.Euler(lookPoints[_currentPointIndex]);

        if (cameraAudio != null && rotateClip != null)
        {
            cameraAudio.clip = rotateClip;
            cameraAudio.Play();
        }
    }

    // ============================================================
    // DETECTION
    // ============================================================

    private void DetectPlayer()
    {
        _playerInSight = false;

        Collider[] targets = Physics.OverlapSphere(
            transform.position, viewDistance, detectionMask);

        foreach (Collider target in targets)
        {
            if (!target.CompareTag(playerTag)) continue;

            Transform playerTransform = target.transform;
            Vector3 directionToPlayer = (playerTransform.position - transform.position).normalized;

            float angle = Vector3.Angle(transform.forward, directionToPlayer);
            if (angle > viewAngle) continue;

            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

            if (Physics.Raycast(
                    transform.position,
                    directionToPlayer,
                    out RaycastHit hit,
                    distanceToPlayer,
                    obstacleMask))
            {
                continue;
            }

            _playerInSight = true;
            _playerTransform = playerTransform;
            break;
        }
    }

    private void UpdateState()
    {
        switch (_state)
        {
            case CameraState.Patrolling:
                if (_playerInSight)
                {
                    _state = CameraState.Suspicious;
                    _detectionTimer = 0f;
                }
                break;

            case CameraState.Suspicious:
                if (_playerInSight)
                {
                    _detectionTimer += Time.deltaTime;

                    if (_detectionTimer >= detectionTime)
                    {
                        _state = CameraState.Alerted;
                        _isAlerted = true;
                        OnAlert();
                    }
                }
                else
                {
                    _detectionTimer -= Time.deltaTime;

                    if (_detectionTimer <= 0f)
                    {
                        _detectionTimer = 0f;
                        _state = CameraState.Patrolling;
                    }
                }
                break;

            case CameraState.Alerted:
                if (!_playerInSight)
                {
                    _detectionTimer -= Time.deltaTime * 0.5f;

                    if (_detectionTimer <= 0f)
                    {
                        _detectionTimer = 0f;
                        _state = CameraState.Patrolling;
                        _isAlerted = false;
                    }
                }
                break;
        }
    }

    // ============================================================
    // ALERT
    // ============================================================

    private void OnAlert()
    {
        Debug.Log($"{name}: PLAYER DETECTED!");

        if (cameraAudio != null && alertClip != null)
        {
            cameraAudio.clip = alertClip;
            cameraAudio.Play();
        }

        AlertNearbyEnemies();
    }

    private void AlertNearbyEnemies()
    {
        if (_playerTransform == null) return;

        Collider[] enemies = Physics.OverlapSphere(
            transform.position, alertRadius, enemyMask);

        foreach (Collider enemy in enemies)
        {
            if (enemy.TryGetComponent<IAlertable>(out IAlertable alertable))
            {
                alertable.Alert(_playerTransform.position);
            }
        }
    }

    // ============================================================
    // TRACKING
    // ============================================================

    private void TrackPlayer()
    {
        if (_playerTransform == null) return;

        Vector3 directionToPlayer = (_playerTransform.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(directionToPlayer);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            lookRotation,
            Time.deltaTime * rotateSpeed * 2f);
    }

    // ============================================================
    // VISUALS
    // ============================================================

    private void UpdateVisuals()
    {
        if (spotLight == null) return;

        switch (_state)
        {
            case CameraState.Patrolling:
                spotLight.color = Color.Lerp(spotLight.color, normalColor, Time.deltaTime * 5f);
                break;

            case CameraState.Suspicious:
                spotLight.color = Color.Lerp(spotLight.color, suspiciousColor, Time.deltaTime * 5f);
                break;

            case CameraState.Alerted:
                spotLight.color = Color.Lerp(spotLight.color, alertColor, Time.deltaTime * 5f);
                break;
        }
    }

    // ============================================================
    // GIZMOS
    // ============================================================

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0f, 0.1f);
        Gizmos.DrawWireSphere(transform.position, viewDistance);

        Vector3 leftBound = Quaternion.Euler(0, -viewAngle, 0) * transform.forward * viewDistance;
        Vector3 rightBound = Quaternion.Euler(0, viewAngle, 0) * transform.forward * viewDistance;

        Gizmos.color = _isHacked ? Color.cyan : Color.green;
        Gizmos.DrawRay(transform.position, leftBound);
        Gizmos.DrawRay(transform.position, rightBound);

        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, transform.forward * viewDistance);

        if (lookPoints != null)
        {
            Gizmos.color = Color.yellow;
            foreach (Vector3 point in lookPoints)
            {
                Vector3 direction = Quaternion.Euler(point) * Vector3.forward * 3f;
                Gizmos.DrawRay(transform.position, direction);
            }
        }

        Gizmos.color = new Color(1f, 0f, 0f, 0.1f);
        Gizmos.DrawWireSphere(transform.position, alertRadius);

        if (_playerInSight && _playerTransform != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, _playerTransform.position);
        }
    }
}