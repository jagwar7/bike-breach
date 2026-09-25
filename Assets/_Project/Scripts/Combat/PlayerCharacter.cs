using UnityEngine;
using Project.Runtime.Bike;

namespace Project.Runtime.Combat
{
    [RequireComponent(typeof(RagdollController))]
    public class PlayerCharacter : MonoBehaviour
    {
        [Header("HEALTH CONFIGURATION")]
        [SerializeField] private int maxHealth = 1;

        [Header("EXPLICIT VEHICLE REFERENCES (OPTIONAL)")]
        [SerializeField] private SplineBikeRunner bikeRunner;
        [SerializeField] private Rigidbody bikeRigidbody;

        private RagdollController _ragdoll;
        private int _currentHealth;
        private bool _isDead;

        public bool IsDead => _isDead;

        private void Awake()
        {
            _ragdoll = GetComponent<RagdollController>();

            // RESOLVE VEHICLE REFERENCES AUTOMATICALLY IF NOT MANUALLY LINKED
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

            // 1. DISABLE BIKE RUNNER SCRIPT / PROGRESSION
            if (bikeRunner != null)
            {
                bikeRunner.enabled = false;
            }

            // 2. UNLOCK BIKE RIGIDBODY TO TUMBLE AND CRASH SEPARATELY
            if (bikeRigidbody != null)
            {
                bikeRigidbody.constraints = RigidbodyConstraints.None;
                bikeRigidbody.isKinematic = false;
                bikeRigidbody.useGravity = true;
                bikeRigidbody.linearVelocity = (bikeRigidbody.transform.forward * 6f) + (Vector3.down * 2f);
                bikeRigidbody.AddTorque(Random.insideUnitSphere * 6f, ForceMode.Impulse);
            }
            else
            {
                Debug.LogWarning("[PlayerCharacter] Bike Rigidbody not found on parent!");
            }

            // 3. CALCULATE KNOCKBACK VECTOR AWAY FROM SHOT
            Vector3 hitDir = (transform.position - shotOrigin).normalized;
            hitDir.y = 0.4f; // Slight upward launch angle

            // 4. TRIGGER RAGDOLL COLLAPSE VIA RAGDOLL CONTROLLER
            // This unparents the transform and applies dynamic physics across all bones
            _ragdoll.TriggerRagdoll(null, hitDir.normalized, 22f);
        }
    }
}