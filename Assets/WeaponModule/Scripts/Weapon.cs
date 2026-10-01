using System;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    [Header("WEAPON CONFIGURATION")]
    public WeaponConfig config;
    public Transform firePoint;

    public event Action OnFired;

    private float _nextFireTime;

    public bool Fire(Vector3 targetPosition)
    {
        if (config == null || firePoint == null || Time.time < _nextFireTime)
        {
            return false;
        }

        Vector3 direction = (targetPosition - firePoint.position).normalized;
        if (direction == Vector3.zero)
        {
            return false;
        }

        _nextFireTime = Time.time + config.fireRate;

        Quaternion targetAimRotation = Quaternion.LookRotation(direction);
        Quaternion bulletRotation = targetAimRotation * Quaternion.Euler(90f, 0f, 0f);

        GameObject bulletObj = Instantiate(config.bulletPrefab, firePoint.position, bulletRotation);

        if (bulletObj.TryGetComponent(out Bullet bullet))
        {
            bullet.Initialize(config.damage, config.bulletSpeed, direction);

            Quaternion flippedRotation = firePoint.rotation * Quaternion.Euler(0f, 180f, 0f);
            ParticleSystem muzzleFlash = Instantiate(config.fireParticle, firePoint.position, flippedRotation);

            muzzleFlash.Play();

            float lifeTime = muzzleFlash.main.duration + muzzleFlash.main.startLifetime.constantMax;
            Destroy(muzzleFlash.gameObject, lifeTime);

            OnFired?.Invoke();
            return true;
        }

        return false;
    }
}