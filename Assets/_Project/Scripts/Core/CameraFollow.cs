using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;

    [Header("Gameplay Framing")]
    [SerializeField] private Vector3 playOffset;
    [Tooltip("Target focal point relative to the bike. Increase Y to push the bike lower on screen; increase Z to look further ahead.")]
    [SerializeField] private Vector3 lookAheadOffset;

    [Header("Intro (Ready State) Framing")]
    [Tooltip("Camera offset before game starts. Placed to the right and slightly forward/up to showcase the bike.")]
    [SerializeField] private Vector3 introOffset;
    [SerializeField] private Vector3 introLookAtOffset;

    [Header("Intro Blend Settings")]
    [Tooltip("Time in seconds to transition from the side intro angle to the driving position.")]
    [SerializeField] private float introTransitionDuration = 1.6f;

    [Header("Smoothing")]
    [SerializeField] private float positionSmoothSpeed = 10f;
    [SerializeField] private float rotationSmoothSpeed = 5f;

    [Header("Trigger Angle Transition")]
    [Tooltip("Time in seconds to complete the angle shift toward the enemy.")]
    [SerializeField] private float yawTransitionDuration = 1.2f;

    [Tooltip("Smoothing speed for follow-mode track alignment.")]
    [SerializeField] private float orientationBlendSpeed = 3f;

    [Header("Rotation Tracking Mode")]
    [SerializeField] private bool followPlayerRotation = true;
    

    private Quaternion _currentTrackOrientation = Quaternion.identity;
    private Quaternion _lockedOrientation = Quaternion.identity;

    // Angle offset state
    private float _currentYawOffset = 0f;
    private float _targetYawOffset = 0f;
    private float _yawVelocity = 0f;


    private Vector3 _activeOffset;
    private Vector3 _activeLookTargetOffset;
    private bool _isIntroActive = true;
    private float _introBlendTimer = 0f;

    public bool FollowPlayerRotation => followPlayerRotation;

    private void Awake()
    {
        InitializeOrientation();
    }

    private void Start()
    {
        InitializeOrientation();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged -= HandleGameStateChanged;
            GameManager.Instance.OnStateChanged += HandleGameStateChanged;
            GameManager.Instance.OnGameRetry -= OnRetryTriggered;
            GameManager.Instance.OnGameRetry += OnRetryTriggered;

            HandleGameStateChanged(GameManager.Instance.CurrentState);
        }
        else
        {
            SnapToIntro();
        }
    }

    private void OnEnable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged -= HandleGameStateChanged;
            GameManager.Instance.OnStateChanged += HandleGameStateChanged;
            GameManager.Instance.OnGameRetry -= OnRetryTriggered;
            GameManager.Instance.OnGameRetry += OnRetryTriggered;
        }
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged -= HandleGameStateChanged;
            GameManager.Instance.OnGameRetry -= OnRetryTriggered;
        }
    }

    private void HandleGameStateChanged(GameState state)
    {
        if (state == GameState.Ready)
        {
            SnapYawOffsetToZero();
            _isIntroActive = true;
            _introBlendTimer = 0f;
            SnapToIntro();
        }
        else if (state == GameState.Playing)
        {
            _isIntroActive = false;
        }
    }

    private void OnRetryTriggered()
    {
        SnapYawOffsetToZero();

        _isIntroActive = false;
        _introBlendTimer = introTransitionDuration;
        _activeOffset = playOffset;
        _activeLookTargetOffset = lookAheadOffset;

        if (target != null)
        {
            Vector3 snapPos = target.position + (_currentTrackOrientation * playOffset);
            transform.position = snapPos;

            Vector3 lookTarget = target.position + (_currentTrackOrientation * lookAheadOffset);
            Vector3 dir = (lookTarget - snapPos).normalized;
            if (dir != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
            }
        }
    }

    public void SnapYawOffsetToZero()
    {
        _targetYawOffset = 0f;
        _currentYawOffset = 0f;
        _yawVelocity = 0f;

        if (target != null)
        {
            Vector3 forwardFlat = Vector3.ProjectOnPlane(target.forward, Vector3.up).normalized;
            if (forwardFlat == Vector3.zero) forwardFlat = Vector3.forward;
            _currentTrackOrientation = Quaternion.LookRotation(forwardFlat, Vector3.up);
            _lockedOrientation = _currentTrackOrientation;
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // Framing offsets
        if (_isIntroActive)
        {
            _activeOffset = introOffset;
            _activeLookTargetOffset = introLookAtOffset;
        }
        else
        {
            if (_introBlendTimer < introTransitionDuration)
            {
                _introBlendTimer += Time.deltaTime;
                float t = Mathf.Clamp01(_introBlendTimer / introTransitionDuration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                _activeOffset = Vector3.Lerp(introOffset, playOffset, smoothT);
                _activeLookTargetOffset = Vector3.Lerp(introLookAtOffset, lookAheadOffset, smoothT);
            }
            else
            {
                _activeOffset = playOffset;
                _activeLookTargetOffset = lookAheadOffset;
            }
        }

        // Yaw damping
        _currentYawOffset = Mathf.SmoothDampAngle(
            _currentYawOffset,
            _targetYawOffset,
            ref _yawVelocity,
            yawTransitionDuration
        );

        Quaternion yawRotation = Quaternion.Euler(0f, _currentYawOffset, 0f);
        Quaternion desiredTrackOrientation;

        if (followPlayerRotation)
        {
            Vector3 forwardFlat = Vector3.ProjectOnPlane(target.forward, Vector3.up).normalized;
            if (forwardFlat == Vector3.zero) forwardFlat = Vector3.forward;

            desiredTrackOrientation = Quaternion.LookRotation(forwardFlat, Vector3.up) * yawRotation;
        }
        else
        {
            desiredTrackOrientation = _lockedOrientation * yawRotation;
        }

        // Blend tracking
        _currentTrackOrientation = Quaternion.Slerp(
            _currentTrackOrientation,
            desiredTrackOrientation,
            orientationBlendSpeed * Time.deltaTime
        );

        // Position tracking
        Vector3 targetPosition = target.position + (_currentTrackOrientation * _activeOffset);
        float posSpeed = (_introBlendTimer < introTransitionDuration && !_isIntroActive) 
            ? (positionSmoothSpeed * 0.7f) 
            : positionSmoothSpeed;
            
        transform.position = Vector3.Lerp(transform.position, targetPosition, posSpeed * Time.deltaTime);

        // Focal tracking
        Vector3 lookTarget = target.position + (_currentTrackOrientation * _activeLookTargetOffset);
        Vector3 lookDirection = (lookTarget - transform.position).normalized;
        
        if (lookDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection, Vector3.up);
            float rotSpeed = (_introBlendTimer < introTransitionDuration && !_isIntroActive) 
                ? (rotationSmoothSpeed * 0.8f) 
                : rotationSmoothSpeed;
                
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotSpeed * Time.deltaTime);
        }
    }

    private void InitializeOrientation()
    {
        if (target == null) return;
        Vector3 forwardFlat = Vector3.ProjectOnPlane(target.forward, Vector3.up).normalized;
        if (forwardFlat == Vector3.zero) forwardFlat = Vector3.forward;
        _currentTrackOrientation = Quaternion.LookRotation(forwardFlat, Vector3.up);
        _lockedOrientation = _currentTrackOrientation;
    }

    private void SnapToIntro()
    {
        if (target == null) return;
        InitializeOrientation();
        _activeOffset = introOffset;
        _activeLookTargetOffset = introLookAtOffset;

        Vector3 snapPos = target.position + (_currentTrackOrientation * introOffset);
        transform.position = snapPos;

        Vector3 lookTarget = target.position + (_currentTrackOrientation * introLookAtOffset);
        Vector3 dir = (lookTarget - snapPos).normalized;
        if (dir != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
        }
    }

    public void SetYawOffset(float angleInDegrees) => _targetYawOffset = angleInDegrees;
    
    public void ResetYawOffset() => _targetYawOffset = 0f;

    public void SetRotationFollow(bool shouldFollow)
    {
        if (followPlayerRotation == shouldFollow) return;
        followPlayerRotation = shouldFollow;
        if (!followPlayerRotation) _lockedOrientation = _currentTrackOrientation;
    }

    public void EnableRotationFollow() => SetRotationFollow(true);
    public void DisableRotationFollow() => SetRotationFollow(false);
}