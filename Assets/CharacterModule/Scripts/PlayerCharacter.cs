using System;
using UnityEngine;

[RequireComponent(typeof(Health))]
public class PlayerCharacter : MonoBehaviour
{
    [SerializeField] private SplineBikeRunner bikeRunner;
    private CharacterAnimationBridge animBridge;
    [SerializeField] private Weapon weaponL;
    [SerializeField] private Weapon weaponR;
    private WeaponConfig weaponConfigL;
    private WeaponConfig weaponConfigR;
    private float lastFireTime;
    
    private Health health;
    private RagdollController ragdoll; 
    private RiderAimController aimController;
    private Animator animator;

    void Awake()
    {
        animBridge    = GetComponent<CharacterAnimationBridge>();
        aimController = GetComponent<RiderAimController>();
        animator      = GetComponent<Animator>();

        if (weaponL != null)
        {
            weaponConfigL = weaponL.config;
        }

        if (weaponR != null)
        {
            weaponConfigR = weaponR.config;
        }

        health  = GetComponent<Health>();
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
        // if (Input.GetKey(KeyCode.F))
        // {
        //     TryShoot(transform.position + transform.forward * 20f);
        // }
    }

    /// <summary>
    /// Call this while holding/dragging the screen to continuously fire towards the crosshair's world point.
    /// When released, simply stop calling this method.
    /// </summary>
    public void TryShoot(Vector3 targetWorldPosition)
    {
        Debug.Log($"[SHOOT CHECK 1] TryShoot reached! Enabled: {enabled}, Time: {Time.time}, LastFire: {lastFireTime}");

        if (!enabled) return;
        if (Time.time <= lastFireTime) 
        {
            Debug.Log("[SHOOT CHECK 2] Blocked by fireRate / lastFireTime cooldown.");
            return;
        }

        if (weaponL == null && weaponR == null) 
        {
            Debug.LogError("[SHOOT CHECK 3] FAILED: Both weaponL and weaponR are NULL in the Inspector!");
            return;
        }

        if (animBridge != null)
        {
            animBridge.PlayShootAnimation();
        }

        if (weaponL != null)
        {
            Debug.Log("[SHOOT CHECK 4] Calling weaponL.Fire()");
            weaponL.Fire(targetWorldPosition);
        }

        if (weaponR != null)
        {
            Debug.Log("[SHOOT CHECK 4] Calling weaponR.Fire()");
            weaponR.Fire(targetWorldPosition);
        }

        float fireRate = 0.2f;
        if (weaponConfigL != null) fireRate = weaponConfigL.fireRate;
        else if (weaponConfigR != null) fireRate = weaponConfigR.fireRate;

        lastFireTime = Time.time + fireRate;
    }



    private void HandleDeath()
    {
        if (aimController != null)
        {
            aimController.enabled = false;
        }
        
        transform.SetParent(null);

        if (animator != null)
        {
            animator.enabled = false;
        }

        if (bikeRunner != null)
        {
            bikeRunner.OnPlayerDied();
        }

        if (ragdoll != null)
        {
            ragdoll.SetRagdollActive(true);
        }

        enabled = false;
    }
}