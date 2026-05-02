using UnityEngine;

public class SniperRifle : MonoBehaviour
{
    [Header("Weapon Settings")]
    [Tooltip("Damage per shot")]
    [SerializeField] private float damage = 100f;

    [Tooltip("Max range of the shot")]
    [SerializeField] private float range = 300f;

    [Tooltip("Time between shots")]
    [SerializeField] private float fireRate = 1.5f;

    [Tooltip("Layers the bullet can hit")]
    [SerializeField] private LayerMask hitMask;

    [Header("Scope Settings")]
    [Tooltip("Normal field of view")]
    [SerializeField] private float normalFOV = 60f;

    [Tooltip("Scoped field of view")]
    [SerializeField] private float scopedFOV = 15f;

    [Tooltip("How fast the scope zooms in")]
    [SerializeField] private float scopeSpeed = 10f;

    [Header("Scope Zoom Levels")]
    [Tooltip("Different zoom levels the player can cycle through")]
    [SerializeField] private float[] zoomLevels = { 15f, 8f, 4f };

    [Header("Sway")]
    [Tooltip("How much the scope sways")]
    [SerializeField] private float swayAmount = 1f;

    [Tooltip("How fast the scope sways")]
    [SerializeField] private float swaySpeed = 1.5f;

    [Tooltip("How long holding breath lasts")]
    [SerializeField] private float holdBreathDuration = 3f;

    [Tooltip("How long to recover breath")]
    [SerializeField] private float breathRecoveryTime = 4f;

    [Header("Alert")]
    [Tooltip("How far enemies can hear the shot")]
    [SerializeField] private float alertRadius = 50f;

    [Tooltip("Layer mask for enemies")]
    [SerializeField] private LayerMask enemyMask;

    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform muzzlePoint;

    [Header("UI")]
    [Tooltip("The scope overlay UI")]
    [SerializeField] private GameObject scopeOverlay;

    [Tooltip("The normal crosshair")]
    [SerializeField] private GameObject normalCrosshair;

    [Tooltip("The scope crosshair that sways")]
    [SerializeField] private RectTransform scopeCrosshair;

    // Private
    private float _fireTimer = 0f;
    private bool _canShoot = true;
    private bool _isScoped = false;
    private int _currentZoomIndex = 0;
    private float _currentFOV;

    // Sway
    private float _swayX;
    private float _swayY;
    private float _swayTimer;
    private Vector2 _currentSway;

    // Breath
    private bool _holdingBreath = false;
    private float _breathTimer;
    private float _breathAmount = 1f;
    private bool _breathExhausted = false;

    // ============================================================
    // UNITY CALLBACKS
    // ============================================================

    private void Start()
    {
        _currentFOV = normalFOV;
        _breathTimer = holdBreathDuration;

        if (scopeOverlay != null)
            scopeOverlay.SetActive(false);
    }

    private void Update()
    {
        UpdateFireTimer();
        UpdateSway();
        UpdateBreath();
        UpdateScopeCrosshair();
        UpdateFOV();
    }

    // ============================================================
    // SHOOTING
    // ============================================================

    private void UpdateFireTimer()
    {
        if (!_canShoot)
        {
            _fireTimer += Time.deltaTime;
            if (_fireTimer >= fireRate)
            {
                _canShoot = true;
                _fireTimer = 0f;
            }
        }
    }

    /// <summary>
    /// Call this from ThirdPersonController when attack is pressed while aiming.
    /// </summary>
    public bool TryShoot()
    {
        if (!_canShoot) return false;

        _canShoot = false;
        _fireTimer = 0f;

        Shoot();
        return true;
    }

    private void Shoot()
    {
        // Calculate shot direction with sway applied
        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);

        // Apply sway offset to the shot
        Vector2 shotPoint = screenCenter;
        if (_isScoped)
        {
            shotPoint += _currentSway;
        }

        Ray ray = playerCamera.ScreenPointToRay(shotPoint);

        if (Physics.Raycast(ray, out RaycastHit hit, range, hitMask))
        {
            HandleHit(hit);
        }

        // Alert enemies
        AlertEnemies(muzzlePoint.position);

        // Add recoil kick
        ApplyRecoil();

        Debug.Log("Sniper fired!");
    }

    private void HandleHit(RaycastHit hit)
    {
        if (hit.collider.TryGetComponent<IDamageable>(out IDamageable damageable))
        {
            // Check for headshot
            float hitHeight = hit.point.y - hit.collider.bounds.min.y;
            float targetHeight = hit.collider.bounds.size.y;
            bool isHeadshot = hitHeight > targetHeight * 0.85f;

            float finalDamage = isHeadshot ? damage * 3f : damage;

            damageable.TakeDamage(finalDamage);
            Debug.Log($"Hit {hit.collider.name} for {finalDamage} damage! Headshot: {isHeadshot}");
        }
    }

    private void AlertEnemies(Vector3 shotOrigin)
    {
        Collider[] enemies = Physics.OverlapSphere(shotOrigin, alertRadius, enemyMask);

        foreach (Collider enemy in enemies)
        {
            if (enemy.TryGetComponent<IAlertable>(out IAlertable alertable))
            {
                alertable.Alert(shotOrigin);
            }
        }
    }

    // ============================================================
    // SCOPE
    // ============================================================

    /// <summary>
    /// Call this from ThirdPersonController when aim is pressed.
    /// </summary>
    public void SetScoped(bool scoped)
    {
        _isScoped = scoped;

        if (scopeOverlay != null)
            scopeOverlay.SetActive(scoped);

        if (normalCrosshair != null)
            normalCrosshair.SetActive(!scoped);

        if (!scoped)
        {
            _currentZoomIndex = 0;
            StopHoldingBreath();
        }
    }

    /// <summary>
    /// Cycles through zoom levels. Call when scroll wheel is used while scoped.
    /// </summary>
    public void CycleZoom()
    {
        if (!_isScoped) return;

        _currentZoomIndex = (_currentZoomIndex + 1) % zoomLevels.Length;
        Debug.Log($"Zoom level: {_currentZoomIndex + 1}/{zoomLevels.Length}");
    }

    private void UpdateFOV()
    {
        float targetFOV;

        if (_isScoped)
        {
            targetFOV = zoomLevels[_currentZoomIndex];
        }
        else
        {
            targetFOV = normalFOV;
        }

        _currentFOV = Mathf.Lerp(_currentFOV, targetFOV, Time.deltaTime * scopeSpeed);
        playerCamera.fieldOfView = _currentFOV;
    }

    // ============================================================
    // SWAY
    // ============================================================

    private void UpdateSway()
    {
        if (!_isScoped)
        {
            _currentSway = Vector2.zero;
            return;
        }

        _swayTimer += Time.deltaTime * swaySpeed;

        // Use multiple sine waves for more natural sway
        float currentSwayAmount = swayAmount;

        // Reduce sway when holding breath
        if (_holdingBreath && !_breathExhausted)
        {
            currentSwayAmount *= 0.1f;
        }

        // Increase sway when breath is exhausted
        if (_breathExhausted)
        {
            currentSwayAmount *= 2f;
        }

        _swayX = (Mathf.Sin(_swayTimer * 1.0f) + 
                  Mathf.Sin(_swayTimer * 0.7f) * 0.5f) * currentSwayAmount;

        _swayY = (Mathf.Sin(_swayTimer * 0.8f) + 
                  Mathf.Sin(_swayTimer * 1.2f) * 0.3f) * currentSwayAmount;

        _currentSway = new Vector2(_swayX, _swayY);
    }

    private void UpdateScopeCrosshair()
    {
        if (scopeCrosshair == null) return;

        if (_isScoped)
        {
            scopeCrosshair.gameObject.SetActive(true);
            scopeCrosshair.anchoredPosition = _currentSway;
        }
        else
        {
            scopeCrosshair.gameObject.SetActive(false);
        }
    }

    // ============================================================
    // HOLD BREATH
    // ============================================================

    /// <summary>
    /// Call when the player holds the breath button (e.g. Left Shift while scoped).
    /// </summary>
    public void StartHoldingBreath()
    {
        if (_breathExhausted) return;
        if (!_isScoped) return;

        _holdingBreath = true;
    }

    public void StopHoldingBreath()
    {
        _holdingBreath = false;
    }

    private void UpdateBreath()
    {
        if (_holdingBreath && !_breathExhausted)
        {
            _breathTimer -= Time.deltaTime;

            if (_breathTimer <= 0f)
            {
                _breathTimer = 0f;
                _breathExhausted = true;
                _holdingBreath = false;
                Debug.Log("Breath exhausted!");
            }
        }
        else
        {
            // Recover breath
            _breathTimer += Time.deltaTime * (1f / breathRecoveryTime);

            if (_breathTimer >= holdBreathDuration)
            {
                _breathTimer = holdBreathDuration;
                _breathExhausted = false;
            }
        }

        _breathAmount = _breathTimer / holdBreathDuration;
    }

    // ============================================================
    // RECOIL
    // ============================================================

    private void ApplyRecoil()
    {
        // Simple recoil - push sway timer to create a kick
        _swayTimer += 5f;
    }

    // ============================================================
    // PUBLIC GETTERS
    // ============================================================

    public bool IsScoped => _isScoped;
    public float BreathAmount => _breathAmount;
    public bool IsBreathExhausted => _breathExhausted;
    public Vector2 CurrentSway => _currentSway;
    public int CurrentZoomLevel => _currentZoomIndex;

    // ============================================================
    // GIZMOS
    // ============================================================

    private void OnDrawGizmosSelected()
    {
        if (muzzlePoint == null) return;

        Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
        Gizmos.DrawWireSphere(muzzlePoint.position, alertRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawRay(muzzlePoint.position, muzzlePoint.forward * range);
    }
}