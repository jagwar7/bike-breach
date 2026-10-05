using UnityEngine;

public class EnemyEngageState : IEnemyState
{
    public EnemyStateType StateType => EnemyStateType.Engage;
    
    private readonly EnemyCharacter _enemy;
    private float _nextFireTime;
    private bool _hasStartedShooting;

    // Time in seconds the enemy tracks/aims before firing the first shot
    private const float AimDelay = 1.5f;

    // Cache animator parameter hash for performance
    // private static readonly int ShootingParam = _animator.StringToHash("Shoot");

    public EnemyEngageState(EnemyCharacter enemy)
    {
        _enemy = enemy;
    }

    public void Enter()
    {
        _hasStartedShooting = false;

        // Schedule the first shot after the aim delay
        _nextFireTime = Time.time + AimDelay;
        _enemy._animator.SetTrigger("Shoot"); 
    }

    public void Update()
    {
        if (!_enemy.HasValidTarget())
        {
            _enemy.StateMachine.ChangeState(_enemy.IdleState);
            return;
        }

        // 1. Always rotate to track and face the player while aiming or shooting
        Vector3 direction = (_enemy.CurrentTarget.position - _enemy.transform.position).normalized;
        direction.y = 0f;

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            
            _enemy.transform.rotation = Quaternion.Slerp(
                _enemy.transform.rotation,
                targetRotation,
                Time.deltaTime * _enemy.Config.TurnSpeed
            );
        }

        // 2. Wait until the delay expires
        if (Time.time < _nextFireTime)
        {
            return;
        }

        // 3. Trigger the shooting animation on the first shot
        if (!_hasStartedShooting)
        {
            _hasStartedShooting = true;

            if (_enemy._animator != null)
            {
                // If 'shooting' is a Trigger:
                // _enemy._animator.SetTrigger("Shoot");

                // If 'shooting' is a Bool instead, use:
                // _enemy.Animator.SetBool(ShootingParam, true);
            }
        }

        // 4. Fire bullet and schedule the next shot
        _nextFireTime = Time.time + _enemy.Config.FireInterval;

        PlayerHead playerHead = _enemy.CurrentTarget.GetComponentInChildren<PlayerHead>();
        Vector3 aimPosition = playerHead != null 
            ? playerHead.transform.position 
            : _enemy.CurrentTarget.position + (Vector3.up * 2.5f);

        _enemy.RequestShoot(aimPosition);
    }

    public void Exit()
    {
        _hasStartedShooting = false;

        // If 'shooting' is a Bool, reset it when the enemy loses the target or dies
        if (_enemy._animator != null)
        {
            _enemy._animator.ResetTrigger("Shoot");
            // _enemy.Animator.SetBool(ShootingParam, false);
        }
    }
}