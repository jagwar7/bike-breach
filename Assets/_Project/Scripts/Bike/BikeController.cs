using UnityEngine;
using Project.Runtime.Core.Input;
using Project.Runtime.Configs;

namespace Project.Runtime.Bike
{
    [RequireComponent(typeof(Rigidbody))]
    public class BikeController : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private BikeConfigSO config;
        [SerializeField] private Transform groundRayOrigin;
        
        [Header("Tuning Overrides")]
        [SerializeField] private float steerTorqueSpeed = 90f;
        [SerializeField] private float rayDistance = 1.2f;
        [SerializeField] private float fallAngleThreshold = 45f;
        [SerializeField] private float alignmentSpeed = 12f;

        private IInputService _inputService;
        private Rigidbody _rb;
        private bool _isGrounded;
        private Vector3 _surfaceNormal = Vector3.up;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            
            _inputService = FindAnyObjectByType<FloatingJoystickInputService>();

            if (groundRayOrigin == null)
            {
                groundRayOrigin = transform;
            }
        }

        private void FixedUpdate()
        {
            if (_inputService == null || config == null) return;

            CheckSurfaceGrounding();

            if (_isGrounded)
            {
                ApplyDriveAndSteer();
            }
        }

        private void CheckSurfaceGrounding()
        {
            if (Physics.Raycast(groundRayOrigin.position, -transform.up, out RaycastHit hit, rayDistance, config.pipelineLayer))
            {
                _surfaceNormal = hit.normal;
                float slopeAngle = Vector3.Angle(Vector3.up, _surfaceNormal);

                if (slopeAngle <= fallAngleThreshold)
                {
                    _isGrounded = true;
                    return;
                }
            }

            _isGrounded = false;
        }

        private void ApplyDriveAndSteer()
        {
            Vector2 input = _inputService.MoveInput;
            float throttle = Mathf.Clamp01(input.y);
            float steer = input.x;

            // 1. Single combined rotation step (Yaw + Normal Alignment)
            float turnAmount = steer * steerTorqueSpeed * Time.fixedDeltaTime;
            Quaternion yawRotation = Quaternion.AngleAxis(turnAmount, transform.up);
            Quaternion orientedRotation = yawRotation * _rb.rotation;

            // Align local up to hit surface normal
            Quaternion normalAlignment = Quaternion.FromToRotation(orientedRotation * Vector3.up, _surfaceNormal);
            Quaternion targetFinalRotation = normalAlignment * orientedRotation;

            _rb.MoveRotation(Quaternion.Slerp(_rb.rotation, targetFinalRotation, alignmentSpeed * Time.fixedDeltaTime));

            // 2. Drive along the forward tangent
            Vector3 forwardTangent = Vector3.ProjectOnPlane(transform.forward, _surfaceNormal).normalized;

            if (_inputService.IsTouching && throttle > 0.05f)
            {
                Vector3 targetVelocity = forwardTangent * (config.maxForwardSpeed * throttle);
                // Keep existing vertical velocity for smooth gravity contact
                targetVelocity.y = _rb.linearVelocity.y;
                _rb.linearVelocity = Vector3.MoveTowards(_rb.linearVelocity, targetVelocity, config.accelerationRate * Time.fixedDeltaTime);
            }
            else
            {
                Vector3 targetVelocity = Vector3.zero;
                targetVelocity.y = _rb.linearVelocity.y;
                _rb.linearVelocity = Vector3.MoveTowards(_rb.linearVelocity, targetVelocity, config.decelerationRate * Time.fixedDeltaTime);
            }
        }

        #if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (groundRayOrigin == null) return;
            Gizmos.color = _isGrounded ? Color.green : Color.red;
            Gizmos.DrawRay(groundRayOrigin.position, -transform.up * rayDistance);
        }
        #endif
    }
}