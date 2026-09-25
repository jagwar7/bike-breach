using UnityEngine;

[CreateAssetMenu(fileName = "NewWeaponConfig", menuName = "Combat/WeaponConfig")]
public class WeaponConfig : ScriptableObject
{
    public float damage = 20f;
    public float fireRate = 1.25f;
    public float bulletSpeed = 30f;
    public GameObject bulletPrefab;
    public ParticleSystem fireParticle; 

}
