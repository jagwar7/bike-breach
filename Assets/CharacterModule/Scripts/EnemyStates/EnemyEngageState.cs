using UnityEngine;

public class EnemyEngageState : IEnemyState
{
    public EnemyStateType StateType => EnemyStateType.Engage;
    
    private readonly EnemyCharacter _enemy;
    private float _nextFireTime;

    public EnemyEngageState(EnemyCharacter enemy)
    {
        _enemy = enemy;
    }

    public void Enter()
    {

        _nextFireTime = Time.time;
    }

    public void Update()
    {

        if (!_enemy.HasValidTarget())
        {
            _enemy.StateMachine.ChangeState(_enemy.IdleState);
            return;
        }


        float distance = Vector3.Distance(_enemy.transform.position, _enemy.CurrentTarget.position);
        if (distance > _enemy.Config.DetectionRange)
        {
            _enemy.StateMachine.ChangeState(_enemy.IdleState);
            return;
        }


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


        if (Time.time >= _nextFireTime)
        {
            _nextFireTime = Time.time + _enemy.Config.FireInterval;

            PlayerHead playerHead = _enemy.CurrentTarget.GetComponentInChildren<PlayerHead>();
            Debug.Log(playerHead.gameObject.name);

            Vector3 aimPosition = playerHead != null ? playerHead.transform.position : _enemy.CurrentTarget.position + (Vector3.up * 2.5f);
            _enemy.RequestShoot(aimPosition);

            
        }
    }

    public void Exit() { }
}