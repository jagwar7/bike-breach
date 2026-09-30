using UnityEngine;

public enum CameraRotationAction
{
    EnableRotationFollow,
    DisableRotationFollow
}

[RequireComponent(typeof(Collider))]
public class CameraRotationZoneTrigger : MonoBehaviour
{
    [Header("Action")]
    [Tooltip("Choose whether this trigger starts or stops the camera from following player rotation.")]
    [SerializeField] private CameraRotationAction action = CameraRotationAction.DisableRotationFollow;

    [Header("Filter")]
    [SerializeField] private string playerTag = "Player";

    private bool _triggered = false;

    private void Awake()
    {
        // Ensure the collider is marked as a trigger
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_triggered) return;

        // Verify if it's the player or bike
        if (other.CompareTag(playerTag) || other.GetComponentInParent<PlayerCharacter>() != null)
        {
            _triggered = true;

            CameraFollow cam = FindAnyObjectByType<CameraFollow>();
            if (cam != null)
            {
                if (action == CameraRotationAction.EnableRotationFollow)
                {
                    cam.EnableRotationFollow();
                    Debug.Log("<color=green>[Camera] Rotation follow ENABLED.</color>");
                }
                else
                {
                    cam.DisableRotationFollow();
                    Debug.Log("<color=yellow>[Camera] Rotation follow DISABLED (Locked Orientation).</color>");
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        // Green box = Enables rotation follow, Orange box = Disables rotation follow
        Gizmos.color = action == CameraRotationAction.EnableRotationFollow 
            ? new Color(0f, 1f, 0f, 0.35f) 
            : new Color(1f, 0.5f, 0f, 0.35f);

        Collider col = GetComponent<Collider>();
        if (col is BoxCollider box)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.DrawWireCube(box.center, box.size);
        }
    }
}