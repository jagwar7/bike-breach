using UnityEngine;

public class RagdollController : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private Animator characterAnimator;
    [SerializeField] private Collider mainRootCollider; 
    [SerializeField] private Rigidbody mainRootRigidbody; 

    [Header("Impulse Settings")]
    [SerializeField] private float defaultImpulseMultiplier = 25f;

    private Health _health;
    private Rigidbody[] _ragdollRigidbodies;
    private Collider[] _ragdollColliders;

    private void Awake()
    {
        if (characterAnimator == null)
            characterAnimator = GetComponent<Animator>();

        if (mainRootCollider == null)
            mainRootCollider = GetComponent<Collider>();

        if (mainRootRigidbody == null)
            mainRootRigidbody = GetComponent<Rigidbody>();

        _health = GetComponent<Health>();

        _ragdollRigidbodies = GetComponentsInChildren<Rigidbody>();
        _ragdollColliders = GetComponentsInChildren<Collider>();

        SetRagdollActive(false);
    }

    private void OnEnable()
    {
        if (_health == null)
            _health = GetComponent<Health>();

        if (_health != null)
        {
            _health.OnDeathWithForce += HandleDeathWithForce;
            _health.OnDeath += HandleSimpleDeath;
        }
    }

    private void OnDisable()
    {
        if (_health != null)
        {
            _health.OnDeathWithForce -= HandleDeathWithForce;
            _health.OnDeath -= HandleSimpleDeath;
        }
    }

    public void SetRagdollActive(bool active)
    {
        // 1. Toggle Animator
        if (characterAnimator != null)
            characterAnimator.enabled = !active;

        // 2. Toggle Root Collider & Rigidbody
        if (mainRootCollider != null)
            mainRootCollider.enabled = !active;

        if (mainRootRigidbody != null)
            mainRootRigidbody.isKinematic = active;

        // 3. Configure Bone Rigidbodies
        foreach (var rb in _ragdollRigidbodies)
        {
            if (rb.gameObject == gameObject) continue;
            rb.isKinematic = !active;
        }

        // 4. Keep bone colliders active so bullets register hits
        foreach (var col in _ragdollColliders)
        {
            if (col.gameObject == gameObject) continue;
            col.enabled = true;
        }
    }

    private void HandleSimpleDeath()
    {
        SetRagdollActive(true);
    }

    private void HandleDeathWithForce(Vector3 hitDirection, Vector3 hitPoint)
    {
        SetRagdollActive(true);

        if (hitDirection == Vector3.zero || _ragdollRigidbodies.Length == 0)
            return;


        Vector3 pushDirection = (hitDirection.normalized + Vector3.up * 0.2f).normalized;
        Vector3 impulse = pushDirection * defaultImpulseMultiplier;


        foreach (var rb in _ragdollRigidbodies)
        {
            if (rb.gameObject == gameObject) continue;
            rb.AddForce(impulse, ForceMode.Impulse);
        }


        if (hitPoint != Vector3.zero)
        {
            foreach (var rb in _ragdollRigidbodies)
            {
                if (rb.gameObject == gameObject) continue;
                rb.AddForceAtPosition(impulse * 0.5f, hitPoint, ForceMode.Impulse);
            }
        }
    }
}