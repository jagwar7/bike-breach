using UnityEngine;

namespace Project.Runtime.Combat
{
    public class DummyEnemy : MonoBehaviour
    {
        [Header("DETECTION & ATTACK SETTINGS")]
        [SerializeField] private float triggerDistance = 14f;
        [SerializeField] private float attackCountdown = 2.0f;

        [Header("VISUAL FEEDBACK")]
        [SerializeField] private MeshRenderer enemyRenderer;
        [SerializeField] private Color alertedColor = Color.yellow;
        [SerializeField] private Color attackingColor = Color.magenta;

        private PlayerCharacter _targetPlayer;
        private bool _isAlerted;
        private bool _isDead;
        private float _timer;

        private void Awake()
        {
            // CACHE RENDERER IF NOT ASSIGNED MANUALLY
            if (enemyRenderer == null)
            {
                enemyRenderer = GetComponent<MeshRenderer>();
            }
        }

        private void Start()
        {
            // LOCATE THE ACTIVE RIDER CAPSULE IN SCENE
            _targetPlayer = FindAnyObjectByType<PlayerCharacter>();
            _timer = attackCountdown;
        }

        private void Update()
        {
            // HALT LOGIC IF DEAD OR TARGET IS INVALID
            if (_isDead || _targetPlayer == null || _targetPlayer.IsDead) return;

            float distanceToPlayer = Vector3.Distance(transform.position, _targetPlayer.transform.position);

            // 1. DETECT BIKE/PLAYER WITHIN PROXIMITY
            if (!_isAlerted && distanceToPlayer <= triggerDistance)
            {
                _isAlerted = true;
                if (enemyRenderer != null)
                {
                    enemyRenderer.material.color = alertedColor;
                }
            }

            // 2. TRACK TARGET AND TICK COUNTDOWN
            if (_isAlerted)
            {
                // FACE HORIZONTALLY TOWARDS THE PLAYER
                Vector3 lookDir = (_targetPlayer.transform.position - transform.position).normalized;
                lookDir.y = 0f;
                if (lookDir != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(lookDir);
                }

                _timer -= Time.deltaTime;

                // 3. EXECUTE ATTACK WHEN COUNTDOWN HITS ZERO
                if (_timer <= 0f)
                {
                    ShootAtPlayer();
                }
            }
        }

        private void ShootAtPlayer()
        {
            _isDead = true;

            if (enemyRenderer != null)
            {
                enemyRenderer.material.color = attackingColor;
            }

            // DAMAGE THE RIDER DIRECTLY AND TRIGGER BIKE CRASH
            _targetPlayer.TakeDamage(1, transform.position);
        }

        public void TakeHit()
        {
            // PREVENT RE-TRIGGERING IF ALREADY HIT
            if (_isDead) return;

            _isDead = true;

            // DISABLE COLLIDER TO PREVENT FURTHER RAYCAST HITS
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            // LAUNCH DUMMY BACKWARDS AND CLEAN UP
            Rigidbody rb = gameObject.AddComponent<Rigidbody>();
            rb.linearVelocity = (Vector3.up * 4f) + (-transform.forward * 3f);
            rb.AddTorque(Random.insideUnitSphere * 10f, ForceMode.Impulse);

            Destroy(gameObject, 1.5f);
        }

        #if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // VISUALIZE ENGAGEMENT RANGE IN SCENE VIEW
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, triggerDistance);
        }
        #endif
    }
}