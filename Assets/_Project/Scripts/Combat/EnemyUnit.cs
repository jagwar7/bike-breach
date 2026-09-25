using UnityEngine;

namespace Project.Runtime.Combat
{
    [RequireComponent(typeof(RagdollController))]
    public class EnemyUnit : MonoBehaviour
    {
        [Header("TARGET")]
        [SerializeField] private Transform _playerTransform;

        [Header("DETECTION & TIMING")]
        [SerializeField] private float triggerDistance = 15f;
        [SerializeField] private float attackDelay = 1.5f;
        [SerializeField] private float turnSpeed = 8f;

        [Header("REFERENCES")]
        [SerializeField] private Animator animator;
        [SerializeField] private BaseWeapon weapon;

        private RagdollController _ragdoll;
        private PlayerCharacter _player;
        private bool _isAlerted;
        private bool _hasFired;
        private bool _isDead;
        private float _timer;

        private static readonly int ShootHash = Animator.StringToHash("Shoot");

        private void Awake()
        {
            _ragdoll = GetComponent<RagdollController>();

            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            if (weapon == null)
            {
                weapon = GetComponentInChildren<BaseWeapon>();
            }
        }

        private void Start()
        {
            _player = _playerTransform.gameObject.GetComponent<PlayerCharacter>();
            Debug.Log(_player.gameObject.name);
            _timer = attackDelay;
        }

        private void Update()
        {
            if (_isDead || _player == null || _player.IsDead) return;

            float distance = Vector3.Distance(transform.position, _player.transform.position);

            // 1. DETECT PLAYER PROXIMITY
            if (!_isAlerted && distance <= triggerDistance)
            {
                _isAlerted = true;
            }

            if (_isAlerted && !_hasFired)
            {
                // 2. HORIZONTAL ROTATION TOWARDS PLAYER
                Vector3 lookDir = (_player.transform.position - transform.position).normalized;
                lookDir.y = 0f;
                if (lookDir != Vector3.zero)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), turnSpeed * Time.deltaTime);
                }

                _timer -= Time.deltaTime;

                // 3. TRIGGER SHOOT ANIMATION JUST BEFORE FIRING
                if (_timer <= 0.3f && animator != null)
                {
                    animator.SetTrigger(ShootHash);
                }

                // 4. EXECUTE WEAPON SHOT
                if (_timer <= 0f)
                {
                    _hasFired = true;

                    if (weapon != null)
                    {
                        weapon.Fire(_player.transform.position);
                    }
                }
            }
        }

        public void TakeHit(Collider hitBoneCollider, Vector3 shotDirection)
        {
            if (_isDead) return;
            _isDead = true;

            // DROP WEAPON INTO WORLD
            if (weapon != null)
            {
                weapon.Drop();
            }

            // COLLAPSE BODY TO RAGDOLL WITH FORCE ON SPECIFIC BONE
            _ragdoll.TriggerRagdoll(hitBoneCollider, shotDirection, 18f);

            Destroy(gameObject, 4f);
        }

        #if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, triggerDistance);
        }
        #endif
    }
}