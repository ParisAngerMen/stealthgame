using UnityEngine;
using UnityEngine.AI;

public class Pistol : MonoBehaviour
{
    [Header("Weapon Settings")]
    [Tooltip("Damage per shot")]
    [SerializeField] private float damage = 25f;

    [Tooltip("Max range of the shot")]
    [SerializeField] private float range = 100f;

    [Tooltip("Time between shots")]
    [SerializeField] private float fireRate = 0.5f;

    [Tooltip("Layers the bullet can hit")]
    [SerializeField] private LayerMask hitMask;

    [Header("Alert Settings")]
    [Tooltip("How far enemies can hear the shot")]
    [SerializeField] private float alertRadius = 30f;

    [Tooltip("Layer mask for enemies")]
    [SerializeField] private LayerMask enemyMask;

    [Header("Effects")]
    [Tooltip("Muzzle flash particle system")]
    [SerializeField] private ParticleSystem muzzleFlash;

    [Tooltip("Impact effect prefab")]
    [SerializeField] private GameObject impactEffectPrefab;

    [Tooltip("Audio source for gunshot")]
    [SerializeField] private AudioSource gunshotAudio;

    [Tooltip("Line renderer for bullet trail")]
    [SerializeField] private LineRenderer bulletTrail;

    [Tooltip("How long the bullet trail stays visible")]
    [SerializeField] private float trailDuration = 0.05f;

    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform muzzlePoint;

    // Private
    private float _fireTimer = 0f;
    private bool _canShoot = true;

    private void Update()
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
    /// Call this from the ThirdPersonController when attack is pressed while aiming.
    /// Returns true if the shot was fired.
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
        // Play effects
        //PlayMuzzleFlash();
        //PlayGunshotSound();

        // Raycast from center of screen
        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Ray ray = playerCamera.ScreenPointToRay(screenCenter);

        Vector3 hitPoint;
        bool hitSomething = Physics.Raycast(ray, out RaycastHit hit, range, hitMask);

        if (hitSomething)
        {
            hitPoint = hit.point;
            Debug.Log("hit");
            HandleHit(hit);
        }
        else
        {
            hitPoint = ray.GetPoint(range);
        }

    }

    private void HandleHit(RaycastHit hit)
    {
        // Check if we hit an enemy
        if (hit.collider.TryGetComponent<GuardAI>(out GuardAI guardAI))
        {
            guardAI.TakeDamage(damage, transform);
            Debug.Log($"Hit {hit.collider.name} for {damage} damage!");
        }

        else
        {
            Debug.Log($"Hit {hit.collider.name} for {damage} damage!");
        }

        // Spawn impact effect
        SpawnImpactEffect(hit.point, hit.normal);
    }

    /// <summary>
    /// Alerts all enemies within alertRadius to the position where the shot was fired.
    /// Enemies investigate the shot origin, NOT the player's current position.
    /// </summary>

    // ============================================================
    // EFFECTS
    // ============================================================

    private void PlayMuzzleFlash()
    {
        if (muzzleFlash != null)
            muzzleFlash.Play();
    }

    private void PlayGunshotSound()
    {
        if (gunshotAudio != null)
            gunshotAudio.Play();
    }

    private void SpawnImpactEffect(Vector3 position, Vector3 normal)
    {
        if (impactEffectPrefab != null)
        {
            GameObject impact = Instantiate(impactEffectPrefab, position, Quaternion.LookRotation(normal));
            Destroy(impact, 2f);
        }
    }

    private void ShowBulletTrail(Vector3 endPoint)
    {
        if (bulletTrail == null) return;

        bulletTrail.enabled = true;
        bulletTrail.SetPosition(0, muzzlePoint.position);
        bulletTrail.SetPosition(1, endPoint);

        CancelInvoke(nameof(HideBulletTrail));
        Invoke(nameof(HideBulletTrail), trailDuration);
    }

    private void HideBulletTrail()
    {
        if (bulletTrail != null)
            bulletTrail.enabled = false;
    }

    // ============================================================
    // GIZMOS
    // ============================================================

    private void OnDrawGizmosSelected()
    {
        if (muzzlePoint == null) return;

        // Alert radius
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.2f);
        Gizmos.DrawWireSphere(muzzlePoint.position, alertRadius);
    }
}