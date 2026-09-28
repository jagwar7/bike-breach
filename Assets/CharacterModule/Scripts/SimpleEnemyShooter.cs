using UnityEngine;

public class SimpleEnemyShooter : MonoBehaviour
{
    [SerializeField] private Transform playerTarget;
    [SerializeField] private Weapon weapon;
    [SerializeField] private float detectionRange = 30f;
    [SerializeField] private float turnSpeed = 10f;

    private void Awake()
    {
        if (weapon == null)
            weapon = GetComponentInChildren<Weapon>();
    }

    private void Update()
    {
        if (playerTarget == null || weapon == null) return;

        float distance = Vector3.Distance(transform.position, playerTarget.position);
        if (distance > detectionRange) return;


        Vector3 dir = playerTarget.position - transform.position;
        dir.y = 0f;
        if (dir != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * turnSpeed);
        }


        Vector3 aimPoint = playerTarget.position + Vector3.up * 1f;
        weapon.Fire(aimPoint);
    }
}