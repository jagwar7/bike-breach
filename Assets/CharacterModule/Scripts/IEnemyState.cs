public interface IEnemyState
{
    EnemyStateType StateType { get; }
    void Enter();
    void Update();
    void Exit();
}