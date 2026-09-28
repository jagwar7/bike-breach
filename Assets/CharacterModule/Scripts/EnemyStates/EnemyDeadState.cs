using UnityEngine;

public class EnemyDeadState : IEnemyState
{
    public EnemyStateType StateType => EnemyStateType.Dead;

    private readonly EnemyCharacter _enemy;

    public EnemyDeadState(EnemyCharacter enemy)
    {
        _enemy = enemy;
    }

    public void Enter()
    {

        if (_enemy.TryGetComponent<RagdollController>(out var ragdoll))
        {
            ragdoll.SetRagdollActive(true);
        }



        _enemy.enabled = false;
    }

    public void Update() { }

    public void Exit() { }
}