using System;
using UnityEngine;


public class PlayerCharacter : MonoBehaviour
{
    private CharacterAnimationBridge animBridge;
    private Weapon weapon;
    private WeaponConfig weaponConfig;
    private float lastFireTime;

    void Awake()
    {
        animBridge = GetComponent<CharacterAnimationBridge>();
        weapon = GetComponentInChildren<Weapon>();
        if(weapon != null)
        {
            weaponConfig = weapon.config;
        }
    }


    void Start()
    {
        Debug.Log("PLAYER CHARACTER SPAWNNED FIRST");
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
        if(Time.time <= lastFireTime) return;

        animBridge.PlayShootAnimation();
        Vector3 targetPosition = transform.position + transform.forward * 20f;
        weapon.Fire(targetPosition);
        lastFireTime = Time.time + weaponConfig.fireRate;
    
    }
}