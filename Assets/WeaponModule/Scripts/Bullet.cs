using UnityEngine;

public class Bullet : MonoBehaviour
{
    private float _damage;
    private float _speed;
    private Vector3 _direction;
    private bool hasHit;
    private Collider bulletCollider;

    private void Awake()
    {
        bulletCollider = GetComponent<Collider>();
    }

    public void Initialize(float damage, float speed, Vector3 direction)
    {
        /// <summary>
        /// ON BULLET SPAWN ASSIGN BULLET CONFIGS
        /// <summary>
        _damage = damage;
        _speed = speed;
        _direction = direction;
        hasHit = false;
        Destroy(gameObject, 3f);
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
        if (hasHit) return;

        Health targetHealth = other.GetComponentInParent<Health>();
        
        Debug.Log("OTHER GAME OBJECT NAME: " + other.gameObject.name);

        if (targetHealth != null)
        {
            hasHit = true;

            if (bulletCollider != null) 
                bulletCollider.enabled = false;

            targetHealth.TakeDamage(_damage, _direction, transform.position);
        }

        Destroy(gameObject);
    }
}