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

    private bool isCameraFocusOnEnemy;

    private CameraFollow _cameraFollow;
    private Health currentEnemyHealth;
    

    private void Awake()
    {
        _cameraFollow = FindAnyObjectByType<CameraFollow>();
        isCameraFocusOnEnemy = false;
        if(currentEnemyTarget != null)
        {
            currentEnemyHealth = currentEnemyTarget.GetComponent<Health>();
        }
    }

    void Update()
    {
        if(isCameraFocusOnEnemy && currentEnemyTarget != null && _cameraFollow != null)
        {
            if(currentEnemyHealth != null && currentEnemyHealth.IsDead)
            {
                Debug.Log("ENEMY DIED. RESETTING CAMERA YAW OFFSET");
                _cameraFollow.ResetYawOffset();
                isCameraFocusOnEnemy = false;
                return;
            }
        }
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag) && _cameraFollow != null)
        {
            Debug.Log($"<color=cyan>[CameraAngleTrigger] Player entered trigger. Setting yaw offset to {targetAngle} degrees.</color>");
            _cameraFollow.SetYawOffset(targetAngle);
            isCameraFocusOnEnemy = true;
            if(isTimerActive) TimeManager.Instance.EnableSlowMotion(0.75f);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (resetOnExit && other.CompareTag(playerTag) && _cameraFollow != null && currentEnemyHealth.IsDead == false && isCameraFocusOnEnemy)
        {
            _cameraFollow.ResetYawOffset();
        }
        if(isTimerActive) TimeManager.Instance.RestoreNormalTime();
    }
}