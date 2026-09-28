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
    private RiderAimController aimController;
    private Animator animator;
    void Awake()
    {
        animBridge = GetComponent<CharacterAnimationBridge>();
        weapon = GetComponentInChildren<Weapon>();
        aimController = GetComponent<RiderAimController>();
        animator = GetComponent<Animator>();
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
        // Debug fallback for editor testing
        if (Input.GetKey(KeyCode.F))
        {
            TryShoot(transform.position + transform.forward * 20f);
        }
    }

    /// <summary>
    /// Call this while holding/dragging the screen to continuously fire towards the crosshair's world point.
    /// When released, simply stop calling this method.
    /// </summary>
    public void TryShoot(Vector3 targetWorldPosition)
    {
        if(!enabled) return;
        if (weapon == null || weaponConfig == null) return;
        if (Time.time <= lastFireTime) return;

        if (animBridge != null)
        {
            animBridge.PlayShootAnimation();
        }

        weapon.Fire(targetWorldPosition);
        lastFireTime = Time.time + weaponConfig.fireRate;
    }

    private void HandleDeath()
    {
        // 1. Disable Aim Controller so it stops fighting the ragdoll physics
        if (aimController != null)
        {
            aimController.enabled = false;
        }
        transform.SetParent(null);


        if(animator != null)
        {
            animator.enabled = false;
        }


        // 2. Activate the ragdoll physics
        if (ragdoll != null)
        {
            ragdoll.SetRagdollActive(true);
        }

        enabled = false;
    }
}