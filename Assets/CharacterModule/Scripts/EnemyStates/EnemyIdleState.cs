using UnityEngine;

public class EnemyIdleState : IEnemyState
{
    public EnemyStateType StateType => EnemyStateType.Idle;

    private readonly EnemyCharacter _enemy;

    public EnemyIdleState(EnemyCharacter enemy)
    {
        _enemy = enemy;
    }

    public void Enter()
    {
        Debug.Log("ENTERED IN IDLE STATE");
    }

    public void Update()
    {
        
        if (!_enemy.HasValidTarget()) return;
        // if (_enemy.HasValidTarget())
        {
            // Debug.Log("VALID TARGET FOUND: " + _enemy.)
        }

        Debug.Log("VALID TARGET FOUND");

        float distance = Vector3.Distance(_enemy.transform.position, _enemy.CurrentTarget.position);
        if (distance <= _enemy.Config.DetectionRange)
        {
            _enemy.StateMachine.ChangeState(_enemy.EngageState);
        }
    }

    public void Exit() { }
}