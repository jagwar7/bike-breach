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
    [SerializeField] private float rotationSmoothSpeed = 10f;

    [Tooltip("Smoothing speed for the track frame transition when switching follow mode on/off.")]
    [SerializeField] private float orientationBlendSpeed = 3f;

    [Header("Rotation Tracking Mode")]
    [Tooltip("If true, camera follows the player's forward direction. If false, camera locks its orientation and only tracks position.")]
    [SerializeField] private bool followPlayerRotation = true;

    private Quaternion _currentTrackOrientation = Quaternion.identity;
    private Quaternion _lockedOrientation = Quaternion.identity;

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

        Quaternion desiredTrackOrientation;

        if (followPlayerRotation)
        {
            // 1. Flatten forward vector so camera never rolls with the bike
            Vector3 forwardFlat = Vector3.ProjectOnPlane(target.forward, Vector3.up).normalized;
            if (forwardFlat == Vector3.zero) forwardFlat = Vector3.forward;

            // Target orientation to aim for
            desiredTrackOrientation = Quaternion.LookRotation(forwardFlat, Vector3.up);
        }
        else
        {
            // Target orientation stays pinned to the locked heading
            desiredTrackOrientation = _lockedOrientation;
        }

        // 2. Smoothly slerp the tracking frame instead of snapping instantly
        _currentTrackOrientation = Quaternion.Slerp(
            _currentTrackOrientation, 
            desiredTrackOrientation, 
            orientationBlendSpeed * Time.deltaTime
        );

        // 3. Compute position behind bike using the smoothed track orientation
        Vector3 targetPosition = target.position + (_currentTrackOrientation * offset);
        transform.position = Vector3.Lerp(transform.position, targetPosition, positionSmoothSpeed * Time.deltaTime);

        // 4. Look ahead and above bike to keep it framed at the bottom of the screen
        Vector3 lookTarget = target.position + (_currentTrackOrientation * lookAheadOffset);
        Quaternion targetRotation = Quaternion.LookRotation((lookTarget - transform.position).normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSmoothSpeed * Time.deltaTime);
    }

    /// <summary>
    /// Call this to toggle rotation following on or off.
    /// </summary>
    public void SetRotationFollow(bool shouldFollow)
    {
        if (followPlayerRotation == shouldFollow) return;

        followPlayerRotation = shouldFollow;

        // When disabling rotation tracking, pin the current orientation
        if (!followPlayerRotation)
        {
            _lockedOrientation = _currentTrackOrientation;
        }
    }

    public void EnableRotationFollow() => SetRotationFollow(true);
    public void DisableRotationFollow() => SetRotationFollow(false);
}