using UnityEngine;

public class RiderAimController : MonoBehaviour
{
    [Header("Bone Setup")]
    [Tooltip("Drag mixamorig:Spine here")]
    [SerializeField] private Transform spineBone;

    [Header("Aim Target")]
    public Transform target; // ENEMY_CH
    [SerializeField] private float turnSpeed = 12f;

    [Header("Angle Limits")]
    [SerializeField] private float maxHorizontalAngle = 85f;
    [SerializeField] private float maxPitchUpAngle = 35f;
    [SerializeField] private float maxPitchDownAngle = 20f;

    [Header("Hunch Correction")]
    [Tooltip("Straightens the hunched bike-riding pose upright. Adjust in Play mode.")]
    [SerializeField] private float uprightCorrection = -25f; // Pulls spine back upright

    private float currentYaw = 0f;
    private float currentPitch = 0f;

    private void LateUpdate()
    {
        if (target == null || spineBone == null) return;

        // 1. Vector from spine to target
        Vector3 dirToTarget = target.position - spineBone.position;
        if (dirToTarget.sqrMagnitude < 0.001f) return;

        // --- HORIZONTAL (YAW) ---
        Vector3 flatDir = dirToTarget;
        flatDir.y = 0f;

        float targetYaw = 0f;
        if (flatDir.sqrMagnitude > 0.001f)
        {
            targetYaw = Vector3.SignedAngle(transform.forward, flatDir, Vector3.up);
            targetYaw = Mathf.Clamp(targetYaw, -maxHorizontalAngle, maxHorizontalAngle);
        }

        // --- VERTICAL (PITCH) ---
        float horizontalDist = flatDir.magnitude;
        float heightDiff = dirToTarget.y;

        float rawPitch = Mathf.Atan2(heightDiff, horizontalDist) * Mathf.Rad2Deg;
        float targetPitch = Mathf.Clamp(rawPitch, -maxPitchDownAngle, maxPitchUpAngle);

        currentYaw = Mathf.Lerp(currentYaw, targetYaw, Time.deltaTime * turnSpeed);
        currentPitch = Mathf.Lerp(currentPitch, targetPitch, Time.deltaTime * turnSpeed);

        Quaternion uprightRot = Quaternion.AngleAxis(uprightCorrection, transform.right);

        Quaternion yawRot = Quaternion.AngleAxis(currentYaw, Vector3.up);

        Vector3 aimRightAxis = Vector3.Cross(Vector3.up, flatDir.normalized);
        Quaternion pitchRot = Quaternion.AngleAxis(currentPitch, aimRightAxis);

        spineBone.rotation = pitchRot * yawRot * uprightRot * spineBone.rotation;
    }
}