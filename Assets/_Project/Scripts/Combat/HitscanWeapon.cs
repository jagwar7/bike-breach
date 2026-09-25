using UnityEngine;

namespace Project.Runtime.Combat
{
    public class HitscanWeapon : BaseWeapon
    {
        [Header("HITSCAN CONFIG")]
        [SerializeField] private int damage = 1;
        [SerializeField] private LayerMask hitMask;
        [SerializeField] private float range = 50f;

        public override void Fire(Vector3 targetPosition)
        {
            if (!CanFire) return;
            _lastFireTime = Time.time;

            PlayMuzzleVFX();

            Vector3 origin = muzzlePoint != null ? muzzlePoint.position : transform.position;
            Vector3 direction = (targetPosition - origin).normalized;

            if (Physics.Raycast(origin, direction, out RaycastHit hit, range, hitMask))
            {
                PlayerCharacter player = hit.collider.GetComponentInParent<PlayerCharacter>();
                if (player != null)
                {
                    player.TakeDamage(damage, origin);
                }
            }
        }
    }
}