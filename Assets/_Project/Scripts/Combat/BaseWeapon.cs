using UnityEngine;

namespace Project.Runtime.Combat
{
    public abstract class BaseWeapon : MonoBehaviour
    {
        [Header("WEAPON BASE SETTINGS")]
        [SerializeField] protected Transform muzzlePoint;
        // [SerializeField] protected ParticleSystem muzzleFlash;
        [SerializeField] protected float fireCooldown = 0.5f;

        protected float _lastFireTime;

        public virtual bool CanFire => Time.time >= _lastFireTime + fireCooldown;

        public abstract void Fire(Vector3 targetPosition);

        public virtual void Drop()
        {
            // UNPARENT FROM HAND BONE
            transform.SetParent(null);

            // ENABLE COLLIDER SO IT DOES NOT FALL THROUGH THE GROUND
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.enabled = true;
            }

            // ADD OR CONFIGURE RIGIDBODY
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
            }

            rb.isKinematic = false;
            rb.useGravity = true;
            rb.mass = 1.5f;
            rb.linearVelocity = (Vector3.up * 2f) + (Random.insideUnitSphere * 1.5f);
        }

        protected virtual void PlayMuzzleVFX()
        {
            // if (muzzleFlash != null)
            // {
            //     muzzleFlash.Play();
            // }
        }
    }
}