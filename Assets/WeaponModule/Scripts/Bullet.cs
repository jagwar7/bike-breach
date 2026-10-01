using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Bullet : MonoBehaviour
{
    private const float Lifetime = 3f;

    private float _damage;
    private float _speed;
    private Vector3 _direction;
    private bool _hasHit;
    private Collider _bulletCollider;

    private void Awake()
    {
        _bulletCollider = GetComponent<Collider>();
    }

    /// <summary>
    /// Initializes bullet parameters and schedules auto-destruction.
    /// </summary>
    public void Initialize(float damage, float speed, Vector3 direction)
    {
        _damage = damage;
        _speed = speed;
        _direction = direction.normalized;
        _hasHit = false;

        Destroy(gameObject, Lifetime);
    }

    private void Update()
    {
        transform.position += _direction * (_speed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_hasHit)
        {
            return;
        }
        int targetLayer = other.gameObject.layer;
        if(targetLayer == 8) return;
        if (targetLayer <= 7)
        {
            Destroy(gameObject);
            return;
        }

        _hasHit = true;

        if (_bulletCollider != null)
        {
            _bulletCollider.enabled = false;
        }

        if (other.TryGetComponent(out Health targetHealth) || other.GetComponentInParent<Health>() is { } parentHealth && (targetHealth = parentHealth) != null)
        {
            targetHealth.TakeDamage(_damage, _direction, transform.position);
        }

        Destroy(gameObject);
    }
}