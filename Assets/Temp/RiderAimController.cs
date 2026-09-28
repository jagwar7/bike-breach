using UnityEngine;

public class RiderAimController : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Bones")]
    [Tooltip("Spine bone to rotate.")]
    [SerializeField] private Transform spineBone;

    [Header("Subtle Spine Twist Limit")]
    [Tooltip("Maximum degrees the spine will turn left or right (keep around 15-25 deg).")]
    [SerializeField] private float maxSpineAngle = 20f;

    [Tooltip("Speed of turning.")]
    [SerializeField] private float turnSpeed = 15f;

    [Header("Rig Compensation")]
    [Tooltip("If the spine turns the wrong way (left instead of right), toggle this.")]
    [SerializeField] private bool invertYaw = true;

    public bool IsAimingRight { get; private set; } = true;

    private float _currentYaw;
    private Quaternion _initialLocalRotation;

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

    public void SetAimSideFromScreen(float screenX)
    {
        IsAimingRight = screenX >= (Screen.width * 0.5f);
    }

    private void LateUpdate()
    {
        if (spineBone == null || target == null) return;

        // 1. Calculate direction to the target on the horizontal plane
        Vector3 toTarget = target.position - spineBone.position;
        Vector3 flatTargetDir = Vector3.ProjectOnPlane(toTarget, Vector3.up).normalized;
        Vector3 flatForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;

        if (flatTargetDir.sqrMagnitude < 0.001f || flatForward.sqrMagnitude < 0.001f) return;

        // 2. Measure angle between character forward and target
        float targetYaw = Vector3.SignedAngle(flatForward, flatTargetDir, Vector3.up);

        // Mixamo 180-degree hip compensation
        if (invertYaw)
        {
            targetYaw = -targetYaw;
        }

        // 3. Clamp to subtle angle so it doesn't over-twist
        targetYaw = Mathf.Clamp(targetYaw, -maxSpineAngle, maxSpineAngle);

        // 4. Smoothly interpolate angle
        _currentYaw = Mathf.Lerp(_currentYaw, targetYaw, turnSpeed * Time.deltaTime);

        // 5. Apply the twist on top of the animator's current frame pose
        spineBone.localRotation = spineBone.localRotation * Quaternion.Euler(0f, _currentYaw, 0f);
    }
}