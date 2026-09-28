using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class Weapon : MonoBehaviour
{
    [Header("WEAPON CONFIGURATION")]
    public WeaponConfig config;
    public Transform firePoint;

    public event Action OnFired;

    private float _nextFireTime;

    void Update()
    {
        // if (Input.GetKeyDown(KeyCode.Space))
        // {
        //     Vector3 target = firePoint.position + firePoint.forward * 20f;
        //     Fire(target);
        // }
    }

    public bool Fire(Vector3 targetPosition)
    {
        if (config == null || Time.time < _nextFireTime) return false;

        _nextFireTime = Time.time + config.fireRate;

        // 1. Calculate the exact firing direction and rotation toward target
        Vector3 direction = (targetPosition - firePoint.position).normalized;
        if (direction == Vector3.zero) return false;

        Quaternion targetAimRotation = Quaternion.LookRotation(direction);

        // Bullet mesh rotation (adjust the 90 offset if your bullet asset requires it)
        Quaternion bulletRotation = targetAimRotation * Quaternion.Euler(90f, 0f, 0f);

        GameObject bulletObj = Instantiate(config.bulletPrefab, firePoint.position, bulletRotation);
        
        if (bulletObj.TryGetComponent(out Bullet bullet))
        {
            bullet.Initialize(config.damage, config.bulletSpeed, direction);

            // 2. Base muzzle flash rotation on TARGET DIRECTION, not firePoint.rotation
            // If your particle faces standard forward (+Z), just use: targetAimRotation
            // If your particle mesh/cone is flipped backwards, apply the 180 flip to targetAimRotation:
            // Quaternion muzzleRotation = targetAimRotation * Quaternion.Euler(0f, 180f, 0f);

            Quaternion flippedRotation = firePoint.rotation * Quaternion.Euler(0f, 180f, 0f);
            ParticleSystem muzzleFlash = Instantiate(config.fireParticle, firePoint.position, flippedRotation);
            Debug.Log("PARTICLE EFFECT NAME: " + muzzleFlash.gameObject.name);

            muzzleFlash.Play();
            Destroy(muzzleFlash.gameObject, muzzleFlash.main.duration + muzzleFlash.main.startLifetime.constantMax);

            // NOTIFY THE LISTENERS
            OnFired?.Invoke();
            return true;
        }

        return false;
    }
}