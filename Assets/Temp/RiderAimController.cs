using UnityEngine;

public class RiderAimController : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Spine Bone")]
    [Tooltip("Assign mixamorig:Spine, Spine1, or Spine2.")]
    [SerializeField] private Transform spineBone;

    [Header("Horizontal (Yaw) Settings")]
    [Tooltip("Maximum degrees the spine can rotate left/right.")]
    [SerializeField] private float maxTurnAngle = 60f;

    [Tooltip("Check this if moving the crosshair right makes the spine turn left.")]
    [SerializeField] private bool invertYawDirection = true;

    [Header("Vertical (Pitch) Settings")]
    [Tooltip("Maximum degrees the spine can tilt upward.")]
    [SerializeField] private float maxPitchUpAngle = 35f;

    [Tooltip("Maximum degrees the spine can tilt downward.")]
    [SerializeField] private float maxPitchDownAngle = 15f;

    [Tooltip("Check this if aiming up makes the spine pitch down.")]
    [SerializeField] private bool invertPitchDirection = false;

    [Header("Smoothing")]
    [Tooltip("Speed of aiming interpolation.")]
    [SerializeField] private float aimSpeed = 15f;

    private float _currentYawOffset;
    private float _currentPitchOffset;

    private void Awake()
    {
        if (spineBone == null)
        {
            Animator anim = GetComponentInChildren<Animator>();
            if (anim != null && anim.isHuman)
            {
                spineBone = anim.GetBoneTransform(HumanBodyBones.Chest) 
                         ?? anim.GetBoneTransform(HumanBodyBones.Spine);
            }
        }
    }

    private void LateUpdate()
    {
        if (spineBone == null) return;

        // Smoothly return to default posture if no target
        if (target == null)
        {
            _currentYawOffset = Mathf.Lerp(_currentYawOffset, 0f, aimSpeed * Time.deltaTime);
            _currentPitchOffset = Mathf.Lerp(_currentPitchOffset, 0f, aimSpeed * Time.deltaTime);

            if (Mathf.Abs(_currentYawOffset) > 0.01f || Mathf.Abs(_currentPitchOffset) > 0.01f)
            {
                ApplyAimRotations(_currentYawOffset, _currentPitchOffset, transform.forward);
            }
            return;
        }

        Vector3 toTarget = target.position - spineBone.position;
        if (toTarget.sqrMagnitude < 0.001f) return;


        Vector3 flatTarget = Vector3.ProjectOnPlane(toTarget, Vector3.up).normalized;
        Vector3 flatForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;

        float targetYaw = 0f;
        if (flatTarget.sqrMagnitude > 0.001f && flatForward.sqrMagnitude > 0.001f)
        {
            targetYaw = Vector3.SignedAngle(flatForward, flatTarget, Vector3.up);
            if (invertYawDirection) targetYaw = -targetYaw;
            targetYaw = Mathf.Clamp(targetYaw, -maxTurnAngle, maxTurnAngle);
        }


        float horizontalDistance = new Vector2(toTarget.x, toTarget.z).magnitude;
        float targetPitch = Mathf.Atan2(toTarget.y, horizontalDistance) * Mathf.Rad2Deg;

        if (invertPitchDirection) targetPitch = -targetPitch;

        // Clamp upward and downward tilt
        targetPitch = Mathf.Clamp(targetPitch, -maxPitchDownAngle, maxPitchUpAngle);


        _currentYawOffset = Mathf.Lerp(_currentYawOffset, targetYaw, aimSpeed * Time.deltaTime);
        _currentPitchOffset = Mathf.Lerp(_currentPitchOffset, targetPitch, aimSpeed * Time.deltaTime);


        ApplyAimRotations(_currentYawOffset, _currentPitchOffset, flatTarget.sqrMagnitude > 0.001f ? flatTarget : flatForward);
    }

    private void ApplyAimRotations(float yaw, float pitch, Vector3 forwardDir)
    {

        Quaternion yawRot = Quaternion.AngleAxis(yaw, Vector3.up);


        Vector3 pitchAxis = Vector3.Cross(Vector3.up, forwardDir).normalized;
        Quaternion pitchRot = Quaternion.AngleAxis(-pitch, pitchAxis);

        spineBone.rotation = pitchRot * yawRot * spineBone.rotation;
    }
}