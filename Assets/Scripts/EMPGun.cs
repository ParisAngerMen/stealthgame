using UnityEngine;

public class EMPGun : MonoBehaviour
{
    [Header("Weapon Settings")]
    [Tooltip("Max range of the shot")]
    [SerializeField] private float range = 50f;

    [Tooltip("Time between shots")]
    [SerializeField] private float fireRate = 2f;

    [Tooltip("How long the target stays disabled")]
    [SerializeField] private float disableDuration = 10f;

    [Tooltip("Layers the gun can hit")]
    [SerializeField] private LayerMask hitMask;

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
    /// Call this from the controller when attacking while aiming.
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
        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Ray ray = playerCamera.ScreenPointToRay(screenCenter);

        if (Physics.Raycast(ray, out RaycastHit hit, range, hitMask))
        {
            // Check if we hit something hackable
            if (hit.collider.TryGetComponent<IHackable>(out IHackable hackable))
            {
                hackable.Hack(disableDuration);
                Debug.Log($"Hacked {hit.collider.name} for {disableDuration} seconds!");
            }
            else
            {
                Debug.Log($"Hit {hit.collider.name} but it's not hackable");
            }
        }
        else
        {
            Debug.Log("Shot missed");
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (muzzlePoint == null) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(muzzlePoint.position, muzzlePoint.forward * range);
    }
}