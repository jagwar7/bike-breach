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

    public void BindBikeRunner(SplineBikeRunner runner)
    {
        bikeRunner = runner;
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

    /// <summary>
    /// Call this while holding/dragging the screen to continuously fire towards the crosshair's world point.
    /// When released, simply stop calling this method.
    /// </summary>
    public void TryShoot(Vector3 targetWorldPosition)
    {
        if (!enabled) return;
        if (Time.time <= lastFireTime) 
        {
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
            weaponL.Fire(targetWorldPosition);
        }

        if (weaponR != null)
        {
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
        
        transform.SetParent(null, true);

        if (animator != null)
        {
            animator.enabled = false;
        }

        // Inform the bike runner to halt spline traversal and trigger physics crash
        if (bikeRunner != null)
        {
            bikeRunner.OnPlayerDied();
        }
        else
        {
            var fallbackRunner = FindAnyObjectByType<SplineBikeRunner>();
            if (fallbackRunner != null)
            {
                fallbackRunner.OnPlayerDied();
            }
        }

        if (ragdoll != null)
        {
            ragdoll.SetRagdollActive(true);
        }

        CameraFollow cam = FindAnyObjectByType<CameraFollow>();
        if (cam != null)
        {
            cam.SnapYawOffsetToZero();
        }


        if (GameManager.Instance != null)
        {
            GameManager.Instance.TriggerDefeat();
        }

        enabled = false;
    }
}