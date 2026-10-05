using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 2.5f, -4.5f);

    [Header("Screen Framing (Lower Third)")]
    [Tooltip("Target focal point relative to the bike. Increase Y to push the bike lower on screen; increase Z to look further ahead.")]
    [SerializeField] private Vector3 lookAheadOffset = new Vector3(0f, 2.8f, 3.5f);

    [Header("Smoothing")]
    [SerializeField] private float positionSmoothSpeed = 10f;
    [SerializeField] private float rotationSmoothSpeed = 5f;

    [Header("Trigger Angle Transition")]
    [Tooltip("Time in seconds to complete the angle shift toward the enemy. Increase to make it slower/smoother.")]
    [SerializeField] private float yawTransitionDuration = 1.2f;

    [Tooltip("Smoothing speed for follow-mode track alignment.")]
    [SerializeField] private float orientationBlendSpeed = 3f;

    [Header("Rotation Tracking Mode")]
    [Tooltip("If true, camera follows the player's forward direction. If false, camera locks its orientation and only tracks position.")]
    [SerializeField] private bool followPlayerRotation = true;

    private Quaternion _currentTrackOrientation = Quaternion.identity;
    private Quaternion _lockedOrientation = Quaternion.identity;

    // Angle offset state
    private float _currentYawOffset = 0f;
    private float _targetYawOffset = 0f;
    private float _yawVelocity = 0f;

    public bool FollowPlayerRotation => followPlayerRotation;

    private void Start()
    {
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

        // 1. Smoothly damp the yaw angle over yawTransitionDuration seconds
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

        // 2. Blend tracking frame
        _currentTrackOrientation = Quaternion.Slerp(
            _currentTrackOrientation, 
            desiredTrackOrientation, 
            orientationBlendSpeed * Time.deltaTime
        );

        // 3. Position tracking
        Vector3 targetPosition = target.position + (_currentTrackOrientation * offset);
        transform.position = Vector3.Lerp(transform.position, targetPosition, positionSmoothSpeed * Time.deltaTime);

        // 4. Focal point tracking
        Vector3 lookTarget = target.position + (_currentTrackOrientation * lookAheadOffset);
        Quaternion targetRotation = Quaternion.LookRotation((lookTarget - transform.position).normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSmoothSpeed * Time.deltaTime);
    }

    public void SetYawOffset(float angleInDegrees)
    {
        _targetYawOffset = angleInDegrees;
    }

    public void ResetYawOffset()
    {
        _targetYawOffset = 0f;
    }

    public void SetRotationFollow(bool shouldFollow)
    {
        if (followPlayerRotation == shouldFollow) return;

        followPlayerRotation = shouldFollow;

        if (!followPlayerRotation)
        {
            _lockedOrientation = _currentTrackOrientation;
        }
    }

    public void EnableRotationFollow() => SetRotationFollow(true);
    public void DisableRotationFollow() => SetRotationFollow(false);
}