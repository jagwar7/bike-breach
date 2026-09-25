using UnityEngine;

public class EnemyShootingListner : MonoBehaviour
{
    [SerializeField] private EnemyCharacter enemy;
    [SerializeField] private CharacterAnimationBridge animationBridge;
    [SerializeField] private Weapon weapon;

    private void Awake()
    {
        if (enemy == null) enemy = GetComponent<EnemyCharacter>();
        if (animationBridge == null) animationBridge = GetComponent<CharacterAnimationBridge>();
        if (weapon == null) weapon = GetComponentInChildren<Weapon>();
    }

    private void OnEnable()
    {
        SubscribeEvents();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    private void SubscribeEvents()
    {
        if (enemy != null)
            enemy.OnShootRequested += HandleShootRequest;

        if (weapon != null && animationBridge != null)
            weapon.OnFired += animationBridge.PlayShootAnimation;
    }

    private void UnsubscribeEvents()
    {
        if (enemy != null)
            enemy.OnShootRequested -= HandleShootRequest;

        if (weapon != null && animationBridge != null)
            weapon.OnFired -= animationBridge.PlayShootAnimation;
    }

    private void HandleShootRequest(Vector3 targetPosition)
    {
        if (weapon != null)
        {
            weapon.Fire(targetPosition);
        }
    }
}