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

        /// <summary>
        /// 1. IF BULLET COLLIDED ONCE IT WILL NEVER COLLIDE
        /// 2. GET HEALTH SCRIPT FROM TARAGET OBJECT
        /// 3. IF TARGET OBJECT FOUND THEN CONFIRM HIT (HASHIT)
        /// 4. CALL THE DAMAGE FUNCTION OF HEATLH SCRIPT
        /// 4. DESTROY GAMEOBJECT
        /// <summary>
        if (_hasHit) return;

        if (other.GetComponent<Bullet>() != null) return;

        if (other.GetComponentInParent<PlayerCharacter>() != null || other.name.Contains("Bike")) return;

        Health targetHealth = other.GetComponentInParent<Health>();

        Debug.Log("OTHER GAME OBJECT NAME: " + other.gameObject.name);

        if (targetHealth != null)
        {
            _hasHit = true;

            if (_bulletCollider != null) 
                _bulletCollider.enabled = false;

            targetHealth.TakeDamage(_damage, _direction, transform.position);
            Destroy(gameObject);
            return;
        }

        Destroy(gameObject);
    }
}