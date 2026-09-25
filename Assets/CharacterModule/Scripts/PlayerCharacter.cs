using System;
using UnityEngine;

[RequireComponent(typeof(Health))]
public class PlayerCharacter : MonoBehaviour
{
    private CharacterAnimationBridge animBridge;
    private Weapon weapon;
    private WeaponConfig weaponConfig;
    private float lastFireTime;
    
    private Health health;
    private RagdollController ragdoll; 

    void Awake()
    {
        animBridge = GetComponent<CharacterAnimationBridge>();
        weapon = GetComponentInChildren<Weapon>();
        if (weapon != null)
        {
            weaponConfig = weapon.config;
        }

        health = GetComponent<Health>();
        ragdoll = GetComponent<RagdollController>();
    }

    void OnEnable()
    {
        if (health != null)
        {
            health.OnDeath += HandleDeath;
        }
    }

    void OnDisable()
    {
        if (health != null)
        {
            health.OnDeath -= HandleDeath;
        }
    }

    void Start()
    {
        Debug.Log("PLAYER CHARACTER SPAWNED FIRST");
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.F))
        {
            Shoot();
        }
    }

    void Shoot()
    {
        if (Time.time <= lastFireTime) return;

        animBridge.PlayShootAnimation();
        Vector3 targetPosition = transform.position + transform.forward * 20f;
        weapon.Fire(targetPosition);
        lastFireTime = Time.time + weaponConfig.fireRate;
    }

    private void HandleDeath()
    {
        transform.SetParent(null);

        if (ragdoll != null)
        {
            ragdoll.SetRagdollActive(true);
        }

        enabled = false;
    }
}