using UnityEngine;
using Project.Runtime.Bike;

namespace Project.Runtime.Combat
{
    [RequireComponent(typeof(Collider))]
    public class PlayerCharacter : MonoBehaviour
    {
        [Header("HEALTH CONFIGURATION")]
        [SerializeField] private int maxHealth = 1;

        [Header("EXPLICIT VEHICLE REFERENCES (OPTIONAL)")]
        [SerializeField] private SplineBikeRunner bikeRunner;
        [SerializeField] private Rigidbody bikeRigidbody;

        private Collider _col;
        private int _currentHealth;
        private bool _isDead;

        public bool IsDead => _isDead;

        private void Awake()
        {
            _col = GetComponent<Collider>();
            _col.isTrigger = true;

            // RESOLVE REFERENCES AUTOMATICALLY IF NOT MANUALLY LINKED
            if (bikeRunner == null)
            {
                bikeRunner = GetComponentInParent<SplineBikeRunner>();
            }

            if (bikeRigidbody == null && bikeRunner != null)
            {
                bikeRigidbody = bikeRunner.GetComponent<Rigidbody>();
            }

            _currentHealth = maxHealth;
        }

        public void TakeDamage(int damageAmount, Vector3 shotOrigin)
        {
            if (_isDead) return;

            _currentHealth -= damageAmount;

            if (_currentHealth <= 0)
            {
                Die(shotOrigin);
            }
        }

        private void Die(Vector3 shotOrigin)
        {
            _isDead = true;

            // 1. DISABLE BIKE RUNNER PROGRESSION
            if (bikeRunner != null)
            {
                bikeRunner.enabled = false;
            }

            // 2. DETACH PLAYER RIDER FROM BIKE TRANSFORM HIERARCHY
            transform.SetParent(null);

            // 3. RETRIEVE OR ADD RIGIDBODY SAFELY TO AVOID DUPLICATES
            Rigidbody riderRb = GetComponent<Rigidbody>();
            if (riderRb == null)
            {
                riderRb = gameObject.AddComponent<Rigidbody>();
            }

            riderRb.isKinematic = false;
            riderRb.useGravity = true;
            riderRb.mass = 70f;
            _col.isTrigger = false;

            // 4. LAUNCH RIDER AWAY FROM ATTACK VECTOR
            Vector3 hitDir = (transform.position - shotOrigin).normalized;
            riderRb.linearVelocity = (hitDir * 6f) + (Vector3.up * 4f);
            riderRb.AddTorque(Random.insideUnitSphere * 12f, ForceMode.Impulse);

            // 5. UNLOCK BIKE RIGIDBODY TO TUMBLE AND CRASH SEPARATELY
            if (bikeRigidbody != null)
            {
                bikeRigidbody.constraints = RigidbodyConstraints.None;
                bikeRigidbody.isKinematic = false;
                bikeRigidbody.useGravity = true;
                bikeRigidbody.linearVelocity = (bikeRigidbody.transform.forward * 5f) + (Vector3.down * 2f);
                bikeRigidbody.AddTorque(Random.insideUnitSphere * 6f, ForceMode.Impulse);
            }
            else
            {
                Debug.LogWarning("[PlayerCharacter] Bike Rigidbody not found on parent!");
            }
        }
    }
}