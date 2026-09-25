using System;
using UnityEngine;

[RequireComponent(typeof(Health))]
[RequireComponent(typeof(RagdollController))]
public class EnemyCharacter : MonoBehaviour
{
    [Header("Architecture Channel")]
    [SerializeField] private TransformVariable targetChannel;

    [Header("Settings")]
    [SerializeField] private EnemyConfig config;

    public event Action<Vector3> OnShootRequested;
    public event Action<EnemyStateType> OnStateChanged;

    public EnemyStateMachine StateMachine { get; private set; }
    public EnemyIdleState IdleState { get; private set; }
    public EnemyEngageState EngageState { get; private set; }
    public EnemyDeadState DeadState { get; private set; }

    public Transform CurrentTarget { get; private set; }
    public EnemyConfig Config => config;

    private Health _ownHealth;
    private Health _targetHealth;

    private void Awake()
    {
        _ownHealth = GetComponent<Health>();

        StateMachine = new EnemyStateMachine();
        IdleState = new EnemyIdleState(this);
        EngageState = new EnemyEngageState(this);
        DeadState = new EnemyDeadState(this);

        StateMachine.OnStateChanged += (state) => OnStateChanged?.Invoke(state);

        StateMachine.Initialize(IdleState);
    }

    private void OnEnable()
    {
        if (_ownHealth != null)
        {
            _ownHealth.OnDeath += HandleSelfDeath;
        }

        if (targetChannel != null)
        {
            targetChannel.OnTransformChanged += HandleTargetChanged;

            if (targetChannel.Value != null)
            {
                HandleTargetChanged(targetChannel.Value);
            }
        }
    }

    private void OnDisable()
    {
        if (_ownHealth != null)
        {
            _ownHealth.OnDeath -= HandleSelfDeath;
        }

        if (targetChannel != null)
        {
            targetChannel.OnTransformChanged -= HandleTargetChanged;
        }

        ClearTarget();
    }

    private void Update()
    {
        StateMachine.Update();
    }

    private void HandleTargetChanged(Transform newTarget)
    {
        if (_targetHealth != null)
        {
            _targetHealth.OnDeath -= HandleTargetDied;
        }

        if (newTarget == null)
        {
            ClearTarget();
            return;
        }

        CurrentTarget = newTarget;
        _targetHealth = newTarget.GetComponent<Health>();

        if (_targetHealth != null)
        {
            _targetHealth.OnDeath += HandleTargetDied;
        }
    }

    private void HandleTargetDied()
    {
        ClearTarget();

        if (StateMachine != null && StateMachine.CurrentState != DeadState)
        {
            StateMachine.ChangeState(IdleState);
        }
    }

    public void ClearTarget()
    {
        if (_targetHealth != null)
        {
            _targetHealth.OnDeath -= HandleTargetDied;
        }

        CurrentTarget = null;
        _targetHealth = null;

        if (StateMachine != null && StateMachine.CurrentState == EngageState)
        {
            StateMachine.ChangeState(IdleState);
        }
    }

    private void HandleSelfDeath()
    {
        StateMachine.ChangeState(DeadState);
    }

    public bool HasValidTarget()
    {
        if (CurrentTarget == null || config == null)
            return false;


        if (_targetHealth != null && _targetHealth.CurrentHealth <= 0)
            return false;

        return true;
    }

    public void RequestShoot(Vector3 aimPosition)
    {
        OnShootRequested?.Invoke(aimPosition);
    }
}