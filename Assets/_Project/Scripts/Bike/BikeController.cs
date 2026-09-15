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

        private IInputService _inputService;
        private Rigidbody _rigidbody;

        private float _currentForwardSpeed;
        private float _currentRollAngle;
        private bool _isGrounded;
        private Vector3 _surfaceNormal = Vector3.up;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _rigidbody.useGravity = false;
            _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;

            _inputService = FindAnyObjectByType<FloatingJoystickInputService>();

            if (groundRayOrigin == null)
            {
                groundRayOrigin = transform;
            }
        }

        private void FixedUpdate()
        {
            if (_inputService == null || config == null) return;

            SampleSurfacePhysics();

            if (_isGrounded)
            {
                HandleForwardThrottle();
                HandleLateralSteering();
                ApplyRadialSurfaceForce();
            }
            else
            {
                // Fall off the pipe
                _rigidbody.AddForce(Physics.gravity * 2f, ForceMode.Acceleration);
            }
        }

        private void SampleSurfacePhysics()
        {
            float rayLength = 2.0f;
            if (Physics.Raycast(groundRayOrigin.position, -transform.up, out RaycastHit hit, rayLength, config.pipelineLayer))
            {
                _isGrounded = true;
                _surfaceNormal = hit.normal;
                _currentRollAngle = Vector3.Angle(Vector3.up, _surfaceNormal);

                if (_currentRollAngle > config.fallAngle)
                {
                    _isGrounded = false;
                }
            }
            else
            {
                _isGrounded = false;
            }
        }

        private void HandleForwardThrottle()
        {
            float throttleInput = Mathf.Clamp01(_inputService.MoveInput.y);

            if (_inputService.IsTouching && throttleInput > 0.05f)
            {
                _currentForwardSpeed = Mathf.MoveTowards(
                    _currentForwardSpeed,
                    config.maxForwardSpeed * throttleInput,
                    config.accelerationRate * Time.fixedDeltaTime
                );
            }
            else
            {
                _currentForwardSpeed = Mathf.MoveTowards(
                    _currentForwardSpeed,
                    0f,
                    config.decelerationRate * Time.fixedDeltaTime
                );
            }

            Vector3 forwardDir = Vector3.ProjectOnPlane(transform.forward, _surfaceNormal).normalized;
            _rigidbody.linearVelocity = forwardDir * _currentForwardSpeed;
        }

        private void HandleLateralSteering()
        {
            float steerInput = _inputService.MoveInput.x;
            float targetRollDelta = steerInput * config.lateralSteerSpeed * Time.fixedDeltaTime;

            // Apex auto-centering
            if (Mathf.Abs(steerInput) < 0.1f && _currentRollAngle < config.apexAssistanceAngle)
            {
                float crossSign = Vector3.Cross(Vector3.up, _surfaceNormal).z;
                targetRollDelta -= crossSign * config.apexSnapForce * Time.fixedDeltaTime;
            }

            Quaternion rollRotation = Quaternion.AngleAxis(targetRollDelta, transform.forward);
            _rigidbody.MoveRotation(_rigidbody.rotation * rollRotation);

            Quaternion normalAlignment = Quaternion.FromToRotation(transform.up, _surfaceNormal) * _rigidbody.rotation;
            _rigidbody.MoveRotation(Quaternion.Slerp(_rigidbody.rotation, normalAlignment, 15f * Time.fixedDeltaTime));
        }

        private void ApplyRadialSurfaceForce()
        {
            Vector3 inwardForce = -_surfaceNormal * (Physics.gravity.magnitude * config.radialSnapMultiplier);
            _rigidbody.AddForce(inwardForce, ForceMode.Acceleration);
        }

        #if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (groundRayOrigin == null) return;
            Gizmos.color = _isGrounded ? Color.green : Color.red;
            Gizmos.DrawRay(groundRayOrigin.position, -transform.up * 2.0f);
        }
        #endif
    }
}   