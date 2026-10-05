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

    [Header("Slow Motion Trigger")]
    [SerializeField] private float slowMoDistanceThreshold = 15f;

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
    public bool _isSlowMoActive;
    private bool _isDead;
    public Animator _animator;

    private void Awake()
    {
        _ownHealth = GetComponent<Health>();
        _animator = GetComponent<Animator>();

        StateMachine = new EnemyStateMachine();
        IdleState = new EnemyIdleState(this);
        EngageState = new EnemyEngageState(this);
        DeadState = new EnemyDeadState(this);

        StateMachine.OnStateChanged += (state) => OnStateChanged?.Invoke(state);

        StateMachine.Initialize(IdleState);
        _isSlowMoActive = false;
        _isDead = false;
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

        // Safeguard: restore normal timescale if this enemy gets disabled mid-combat
        if (_isSlowMoActive && TimeManager.Instance != null)
        {
            _isSlowMoActive = false;
            // TimeManager.Instance.RestoreNormalTime();
        }

        ClearTarget();
    }

    private void Update()
    {
        StateMachine.Update();
        // HandleSlowMotionRange();
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

    private void HandleSlowMotionRange()
    {
        if (_isDead || !HasValidTarget() || TimeManager.Instance == null) return;

        float distance = Vector3.Distance(transform.position, CurrentTarget.position);

        if (distance <= slowMoDistanceThreshold && !_isSlowMoActive)
        {
            _isSlowMoActive = true;
            // TimeManager.Instance.EnableSlowMotion(0.5f);
        }
        else if (distance > slowMoDistanceThreshold && _isSlowMoActive)
        {
            _isSlowMoActive = false;
            // TimeManager.Instance.RestoreNormalTime();
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
        _isDead = true;

        // Restore normal game speed immediately when killed
        if (_isSlowMoActive && TimeManager.Instance != null)
        {
            _isSlowMoActive = false;
            // TimeManager.Instance.RestoreNormalTime();
        }

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
        Vector3 position = new Vector3(aimPosition.x, aimPosition.y, aimPosition.z + 0.5f);
        OnShootRequested?.Invoke(position);
    }
}