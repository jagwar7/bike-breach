using System.Collections;
using UnityEngine;

public class EnemyIdleState : IEnemyState
{
    public EnemyStateType StateType => EnemyStateType.Idle;

    private readonly EnemyCharacter _enemy;
    private Coroutine _engageCoroutine;

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

        float distance = Vector3.Distance(_enemy.transform.position, _enemy.CurrentTarget.position);
        if (distance <= _enemy.Config.DetectionRange)
        {
            if (_engageCoroutine == null)
            {
                _engageCoroutine = _enemy.StartCoroutine(WaitAndEngage());
            }
        }
        else
        {
            if (_engageCoroutine != null)
            {
                _enemy.StopCoroutine(_engageCoroutine);
                _engageCoroutine = null;
            }
        }
    }

    private IEnumerator WaitAndEngage()
    {
        yield return new WaitForSeconds(2.5f);
        _engageCoroutine = null;
        _enemy.StateMachine.ChangeState(_enemy.EngageState);
    }

    public void Exit()
    {
        if (_engageCoroutine != null)
        {
            _enemy.StopCoroutine(_engageCoroutine);
            _engageCoroutine = null;
        }
    }
}