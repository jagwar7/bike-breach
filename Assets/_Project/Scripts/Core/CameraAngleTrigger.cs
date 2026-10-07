using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CameraAngleTrigger : MonoBehaviour
{
    [Tooltip("Angle in degrees to look toward while in this trigger. E.g., 45 for right, -45 for left.")]
    [SerializeField] private float targetAngle = 45f;

    [Tooltip("If true, resets the angle to 0 when exiting the trigger area.")]
    [SerializeField] private bool resetOnExit = true;

    [Tooltip("Tag used to identify the player.")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private Transform currentEnemyTarget;
    [SerializeField] private bool isTimerActive;

    private bool _isCameraFocusOnEnemy;
    private CameraFollow _cameraFollow;
    private Health _currentEnemyHealth;

    private void Awake()
    {
        _cameraFollow = FindAnyObjectByType<CameraFollow>();
        _isCameraFocusOnEnemy = false;

        if (currentEnemyTarget != null)
        {
            _currentEnemyHealth = currentEnemyTarget.GetComponent<Health>();
        }
    }

    private void OnEnable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameRetry -= ForceInstantReset;
            GameManager.Instance.OnGameRetry += ForceInstantReset;

            GameManager.Instance.OnStateChanged -= HandleGameStateChanged;
            GameManager.Instance.OnStateChanged += HandleGameStateChanged;
        }
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameRetry -= ForceInstantReset;
            GameManager.Instance.OnStateChanged -= HandleGameStateChanged;
        }

        if (_isCameraFocusOnEnemy && isTimerActive && TimeManager.Instance != null)
        {
            TimeManager.Instance.RestoreNormalTime();
        }
    }

    private void HandleGameStateChanged(GameState state)
    {
        if (state == GameState.Failed || state == GameState.Ready || state == GameState.Playing)
        {
            ForceInstantReset();
        }
    }

    private void Update()
    {
        if (!_isCameraFocusOnEnemy) return;

        if (_currentEnemyHealth != null && _currentEnemyHealth.IsDead)
        {
            if (_cameraFollow != null)
            {
                _cameraFollow.ResetYawOffset();
            }
            _isCameraFocusOnEnemy = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Ignore dead player or non-playing states
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;

        if (other.CompareTag(playerTag) || other.GetComponentInParent<SplineBikeRunner>() != null)
        {
            if (_cameraFollow == null) _cameraFollow = FindAnyObjectByType<CameraFollow>();

            if (_cameraFollow != null)
            {
                _cameraFollow.SetYawOffset(targetAngle);
                _isCameraFocusOnEnemy = true;
            }

            if (isTimerActive && TimeManager.Instance != null)
            {
                TimeManager.Instance.EnableSlowMotion(0.75f);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag) && other.GetComponentInParent<SplineBikeRunner>() == null) return;

        if (resetOnExit && _isCameraFocusOnEnemy)
        {
            if (_cameraFollow != null)
            {
                _cameraFollow.ResetYawOffset();
            }
        }

        if (isTimerActive && TimeManager.Instance != null)
        {
            TimeManager.Instance.RestoreNormalTime();
        }

        _isCameraFocusOnEnemy = false;
    }

    public void ForceInstantReset()
    {
        _isCameraFocusOnEnemy = false;

        if (_cameraFollow == null)
            _cameraFollow = FindAnyObjectByType<CameraFollow>();

        if (_cameraFollow != null)
        {
            _cameraFollow.SnapYawOffsetToZero();
        }

        if (isTimerActive && TimeManager.Instance != null)
        {
            TimeManager.Instance.RestoreNormalTime();
        }
    }
}