using System;




public class EnemyStateMachine
{
    public IEnemyState CurrentState { get; private set; }
    public event Action<EnemyStateType> OnStateChanged;

    public void Initialize(IEnemyState startingState)
    {
        CurrentState = startingState;
        CurrentState?.Enter();
    }

    public void ChangeState(IEnemyState newState)
    {
        if (CurrentState == newState) return;

        CurrentState?.Exit();
        CurrentState = newState;
        CurrentState?.Enter();
    }

    public void Update()
    {
        CurrentState?.Update();
    }
}