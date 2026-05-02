using UnityEngine;

public class ProximityMine : MonoBehaviour, IHackable
{
    [Header("Detection")]
    [Tooltip("How close the player must be to trigger the mine")]
    [SerializeField] private float detectionRadius = 3f;

    [Tooltip("Layer mask for detecting the player")]
    [SerializeField] private LayerMask playerMask;

    [Tooltip("Time between player entering radius and explosion")]
    [SerializeField] private float triggerDelay = 0.5f;

    [Header("Damage")]
    [Tooltip("Damage dealt to the player")]
    [SerializeField] private float damage = 50f;

    [Tooltip("Radius of the explosion damage")]
    [SerializeField] private float explosionRadius = 5f;

    [Tooltip("Force applied to nearby rigidbodies")]
    [SerializeField] private float explosionForce = 500f;

    [Tooltip("Layers that can be damaged by explosion")]
    [SerializeField] private LayerMask damageMask;

    [Header("Mine Settings")]
    [Tooltip("Does the mine destroy itself after exploding?")]
    [SerializeField] private bool destroyOnExplode = true;

    [Tooltip("Can the mine be triggered multiple times? (if not destroyed)")]
    [SerializeField] private bool canRetrigger = false;

    [Tooltip("Cooldown between triggers if retriggerable")]
    [SerializeField] private float retriggerCooldown = 3f;

    [Header("Alert")]
    [Tooltip("How far enemies can hear the explosion")]
    [SerializeField] private float alertRadius = 30f;

    [Tooltip("Layer mask for enemies to alert")]
    [SerializeField] private LayerMask enemyMask;

    [Header("Visuals")]
    [Tooltip("Light on the mine that shows its status")]
    [SerializeField] private Light statusLight;

    [Tooltip("The mine model/mesh renderer")]
    [SerializeField] private Renderer mineRenderer;

    [SerializeField] private Color armedColor = Color.red;
    [SerializeField] private Color triggeredColor = new Color(1f, 0.5f, 0f);
    [SerializeField] private Color disabledColor = Color.cyan;
    [SerializeField] private Color safeColor = Color.green;

    [Header("Effects")]
    [Tooltip("Explosion effect prefab")]
    [SerializeField] private GameObject explosionEffectPrefab;

    [Header("Audio")]
    [SerializeField] private AudioSource mineAudio;
    [SerializeField] private AudioClip beepClip;
    [SerializeField] private AudioClip triggerClip;
    [SerializeField] private AudioClip explodeClip;
    [SerializeField] private AudioClip disableClip;
    [SerializeField] private AudioClip rearmClip;

    [Header("Beeping")]
    [Tooltip("Time between beeps when armed")]
    [SerializeField] private float beepInterval = 2f;

    [Tooltip("Time between beeps when triggered")]
    [SerializeField] private float triggeredBeepInterval = 0.15f;

    // State
    private enum MineState
    {
        Armed,
        Triggered,
        Exploded,
        Disabled,
        Rearming
    }

    private MineState _state = MineState.Armed;

    // Hacking
    private bool _isHacked = false;
    private float _hackTimer = 0f;
    private float _hackDuration = 0f;

    // Triggering
    private float _triggerTimer = 0f;
    private float _retriggerTimer = 0f;

    // Beeping
    private float _beepTimer = 0f;

    // Rearming
    private float _rearmTimer = 0f;
    private float _rearmDuration = 3f;

    public bool IsHacked => _isHacked;

    // ============================================================
    // UNITY CALLBACKS
    // ============================================================

    private void Start()
    {
        UpdateVisuals();
    }

    private void Update()
    {
        switch (_state)
        {
            case MineState.Armed:
                HandleArmed();
                break;

            case MineState.Triggered:
                HandleTriggered();
                break;

            case MineState.Disabled:
                HandleDisabled();
                break;

            case MineState.Rearming:
                HandleRearming();
                break;

            case MineState.Exploded:
                // Do nothing, mine is done
                break;
        }

        UpdateVisuals();
    }

    // ============================================================
    // STATES
    // ============================================================

    private void HandleArmed()
    {
        // Beep periodically
        _beepTimer += Time.deltaTime;
        if (_beepTimer >= beepInterval)
        {
            _beepTimer = 0f;
            PlayAudio(beepClip);
        }

        // Check for player in range
        if (DetectPlayer())
        {
            _state = MineState.Triggered;
            _triggerTimer = 0f;
            _beepTimer = 0f;

            PlayAudio(triggerClip);
            Debug.Log($"{name} triggered!");
        }
    }

    private void HandleTriggered()
    {
        _triggerTimer += Time.deltaTime;

        // Fast beeping
        _beepTimer += Time.deltaTime;
        if (_beepTimer >= triggeredBeepInterval)
        {
            _beepTimer = 0f;
            PlayAudio(beepClip);
        }

        // Explode after delay
        if (_triggerTimer >= triggerDelay)
        {
            Explode();
        }
    }

    private void HandleDisabled()
    {
        _hackTimer += Time.deltaTime;

        // Flicker light
        if (statusLight != null)
        {
            float flicker = Mathf.Sin(Time.time * 10f) > 0 ? 0.3f : 0.1f;
            statusLight.intensity = flicker;
        }

        if (_hackTimer >= _hackDuration)
        {
            StartRearming();
        }
    }

    private void HandleRearming()
    {
        _rearmTimer += Time.deltaTime;

        // Light slowly comes back
        if (statusLight != null)
        {
            float t = _rearmTimer / _rearmDuration;
            statusLight.intensity = Mathf.Lerp(0f, 1f, t);
            statusLight.color = Color.Lerp(disabledColor, armedColor, t);
        }

        if (_rearmTimer >= _rearmDuration)
        {
            FinishRearming();
        }
    }

    // ============================================================
    // DETECTION
    // ============================================================

    private bool DetectPlayer()
    {
        Collider[] colliders = Physics.OverlapSphere(
            transform.position,
            detectionRadius,
            playerMask);

        return colliders.Length > 0;
    }

    // ============================================================
    // EXPLOSION
    // ============================================================

    private void Explode()
    {
        _state = MineState.Exploded;
        Debug.Log($"{name} exploded!");

        // Damage everything in explosion radius
        Collider[] colliders = Physics.OverlapSphere(
            transform.position,
            explosionRadius,
            damageMask);

        foreach (Collider col in colliders)
        {
            // Calculate damage falloff based on distance
            float distance = Vector3.Distance(transform.position, col.transform.position);
            float damageMultiplier = 1f - (distance / explosionRadius);
            damageMultiplier = Mathf.Clamp01(damageMultiplier);
            float finalDamage = damage * damageMultiplier;

            // Damage player
            if (col.TryGetComponent<PlayerHealth>(out PlayerHealth playerHealth))
            {
                playerHealth.TakeDamage(finalDamage);
                Debug.Log($"Mine dealt {finalDamage} damage to player");
            }

            // Damage anything with IDamageable
            if (col.TryGetComponent<IDamageable>(out IDamageable damageable))
            {
                damageable.TakeDamage(finalDamage);
            }

            // Apply explosion force to rigidbodies
            if (col.TryGetComponent<Rigidbody>(out Rigidbody rb))
            {
                rb.AddExplosionForce(
                    explosionForce,
                    transform.position,
                    explosionRadius,
                    1f,
                    ForceMode.Impulse);
            }
        }

        // Alert nearby enemies
        AlertEnemies();

        // Spawn explosion effect
        SpawnExplosionEffect();

        // Play explosion sound
        PlayAudio(explodeClip);

        // Disable visuals
        if (mineRenderer != null)
            mineRenderer.enabled = false;

        if (statusLight != null)
            statusLight.enabled = false;

        if (destroyOnExplode)
        {
            // Delay destroy so audio can play
            Destroy(gameObject, 2f);
        }
        else if (canRetrigger)
        {
            // Reset for retriggering
            _retriggerTimer = retriggerCooldown;
            StartCoroutine(RetriggerCountdown());
        }
    }

    private System.Collections.IEnumerator RetriggerCountdown()
    {
        yield return new WaitForSeconds(retriggerCooldown);

        if (mineRenderer != null)
            mineRenderer.enabled = true;

        if (statusLight != null)
            statusLight.enabled = true;

        _state = MineState.Armed;
        _beepTimer = 0f;

        Debug.Log($"{name} re-armed!");
    }

    private void AlertEnemies()
    {
        Collider[] enemies = Physics.OverlapSphere(
            transform.position,
            alertRadius,
            enemyMask);

        foreach (Collider enemy in enemies)
        {
            if (enemy.TryGetComponent<IAlertable>(out IAlertable alertable))
            {
                alertable.Alert(transform.position);
            }
        }
    }

    private void SpawnExplosionEffect()
    {
        if (explosionEffectPrefab != null)
        {
            GameObject effect = Instantiate(
                explosionEffectPrefab,
                transform.position,
                Quaternion.identity);

            Destroy(effect, 3f);
        }
    }

    // ============================================================
    // IHackable
    // ============================================================

    /// <summary>
    /// Called when the mine is hit by the EMP gun.
    /// Temporarily disables the mine.
    /// </summary>
    public void Hack(float duration)
    {
        // Can't hack if already exploded
        if (_state == MineState.Exploded) return;

        _isHacked = true;
        _hackTimer = 0f;
        _hackDuration = duration;
        _state = MineState.Disabled;

        // Stop trigger if it was counting down
        _triggerTimer = 0f;
        _beepTimer = 0f;

        PlayAudio(disableClip);
        Debug.Log($"{name} disabled for {duration} seconds!");
    }

    private void StartRearming()
    {
        _state = MineState.Rearming;
        _rearmTimer = 0f;

        PlayAudio(rearmClip);
        Debug.Log($"{name} is rearming...");
    }

    private void FinishRearming()
    {
        _isHacked = false;
        _state = MineState.Armed;
        _beepTimer = 0f;

        if (statusLight != null)
        {
            statusLight.intensity = 1f;
            statusLight.color = armedColor;
        }

        Debug.Log($"{name} has rearmed!");
    }

    // ============================================================
    // VISUALS
    // ============================================================

    private void UpdateVisuals()
    {
        if (statusLight == null) return;

        switch (_state)
        {
            case MineState.Armed:
                statusLight.color = Color.Lerp(
                    statusLight.color, armedColor, Time.deltaTime * 5f);
                statusLight.intensity = 1f;
                break;

            case MineState.Triggered:
                // Flash between triggered color and white
                float flash = Mathf.Sin(Time.time * 20f) > 0 ? 1f : 0f;
                statusLight.color = Color.Lerp(triggeredColor, Color.white, flash);
                statusLight.intensity = 2f;
                break;

            case MineState.Disabled:
                statusLight.color = disabledColor;
                // Intensity handled in HandleDisabled
                break;

            case MineState.Rearming:
                // Handled in HandleRearming
                break;

            case MineState.Exploded:
                statusLight.enabled = false;
                break;
        }
    }

    // ============================================================
    // AUDIO
    // ============================================================

    private void PlayAudio(AudioClip clip)
    {
        if (mineAudio != null && clip != null)
        {
            mineAudio.clip = clip;
            mineAudio.Play();
        }
    }

    // ============================================================
    // GIZMOS
    // ============================================================

    private void OnDrawGizmosSelected()
    {
        // Detection radius
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        // Explosion radius
        Gizmos.color = new Color(1f, 0f, 0f, 0.15f);
        Gizmos.DrawWireSphere(transform.position, explosionRadius);

        // Alert radius
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.1f);
        Gizmos.DrawWireSphere(transform.position, alertRadius);
    }

    private void OnDrawGizmos()
    {
        // Always show detection radius in scene view
        switch (_state)
        {
            case MineState.Armed:
                Gizmos.color = Color.red;
                break;

            case MineState.Triggered:
                Gizmos.color = Color.yellow;
                break;

            case MineState.Disabled:
            case MineState.Rearming:
                Gizmos.color = Color.cyan;
                break;

            case MineState.Exploded:
                Gizmos.color = Color.gray;
                break;

            default:
                Gizmos.color = Color.red;
                break;
        }

        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}