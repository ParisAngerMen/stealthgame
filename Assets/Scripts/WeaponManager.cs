using UnityEngine;
using StarterAssets;
using UnityEngine.InputSystem;

public class WeaponManager : MonoBehaviour
{
    [Header("Weapons")]
    [SerializeField] private Pistol pistol;
    [SerializeField] private SniperRifle sniperRifle;
    [SerializeField] private EMPGun empGun;

    [Header("References")]
    [SerializeField] private StarterAssetsInputs input;

    private enum WeaponType
    {
        Pistol,
        Sniper,
        EMP
    }

    private WeaponType _currentWeapon = WeaponType.Pistol;

    public bool IsScoped => _currentWeapon == WeaponType.Sniper &&
                            sniperRifle != null &&
                            sniperRifle.IsScoped;

    private void Update()
    {
        HandleWeaponSwitch();
    }

    private void HandleWeaponSwitch()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.digit1Key.wasPressedThisFrame)
            SwitchWeapon(WeaponType.Pistol);

        if (Keyboard.current.digit2Key.wasPressedThisFrame)
            SwitchWeapon(WeaponType.EMP);


        if (Keyboard.current.digit3Key.wasPressedThisFrame)
            SwitchWeapon(WeaponType.Sniper);

    }

    private void SwitchWeapon(WeaponType newWeapon)
    {
        // Clean up current weapon
        if (_currentWeapon == WeaponType.Sniper && sniperRifle != null)
            sniperRifle.SetScoped(false);

        _currentWeapon = newWeapon;

        // Enable/disable weapon objects
        if (pistol != null)
            pistol.gameObject.SetActive(_currentWeapon == WeaponType.Pistol);

        if (sniperRifle != null)
            sniperRifle.gameObject.SetActive(_currentWeapon == WeaponType.Sniper);

        if (empGun != null)
            empGun.gameObject.SetActive(_currentWeapon == WeaponType.EMP);

        Debug.Log($"Switched to: {_currentWeapon}");
    }

    public void TryShoot()
    {
        switch (_currentWeapon)
        {
            case WeaponType.Pistol:
                if (pistol != null) pistol.TryShoot();
                break;

            case WeaponType.Sniper:
                if (sniperRifle != null) sniperRifle.TryShoot();
                break;

            case WeaponType.EMP:
                if (empGun != null) empGun.TryShoot();
                break;
        }
    }

    public void SetAiming(bool aiming)
    {
        switch (_currentWeapon)
        {
            case WeaponType.Sniper:
                if (sniperRifle != null) sniperRifle.SetScoped(aiming);
                break;
        }
    }

    public void SetHoldBreath(bool holding)
    {
        if (_currentWeapon == WeaponType.Sniper && sniperRifle != null)
        {
            if (holding)
                sniperRifle.StartHoldingBreath();
            else
                sniperRifle.StopHoldingBreath();
        }
    }

    public void CycleZoom()
    {
        if (_currentWeapon == WeaponType.Sniper && sniperRifle != null)
        {
            sniperRifle.CycleZoom();
        }
    }
}