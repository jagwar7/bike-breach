using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 2.5f, -4.5f);
    [SerializeField] private float positionSmoothSpeed = 10f;
    [SerializeField] private float rotationSmoothSpeed = 10f;

    private void LateUpdate()
    {
        if (target == null) return;

        // 1. Flatten the target's forward vector so the camera never rolls with the bike
        Vector3 forwardFlat = Vector3.ProjectOnPlane(target.forward, Vector3.up).normalized;
        if (forwardFlat == Vector3.zero) forwardFlat = Vector3.forward;

        // 2. Build camera orientation using strictly World Up (no roll/bank inheritance)
        Quaternion trackOrientation = Quaternion.LookRotation(forwardFlat, Vector3.up);

        // 3. Compute stable target position behind the bike
        Vector3 targetPosition = target.position + (trackOrientation * offset);
        transform.position = Vector3.Lerp(transform.position, targetPosition, positionSmoothSpeed * Time.deltaTime);

        // 4. Look ahead at the bike's upper center
        Vector3 lookTarget = target.position + (Vector3.up * 1.0f);
        Quaternion targetRotation = Quaternion.LookRotation((lookTarget - transform.position).normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSmoothSpeed * Time.deltaTime);
    }
}