using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Splines;
using Unity.Mathematics;
using Project.Runtime.Core.Input;

namespace Project.Runtime.Bike
{
    public enum BikeRunState
    {
        OnPipeline,
        TransitioningToShip,
        OnShipDeckCombat,
        ReachedFinishLine,
        Fallen
    }

    [RequireComponent(typeof(Rigidbody))]
    public class SplineBikeRunner : MonoBehaviour
    {
        [Header("Spline References")]
        [SerializeField] private SplineContainer pipelineSpline;
        [SerializeField] private SplineContainer shipSpline;
        [SerializeField] private float pipeRadius = 1.0f;

        [Header("Pipeline Movement (Hold to Drive)")]
        [SerializeField] private float pipeMaxSpeed = 10f;
        [SerializeField] private float pipeAcceleration = 25f;
        [SerializeField] private float pipeSteerSensitivity = 85f;
        [SerializeField] private float fallAngleLimit = 45f;
        [SerializeField] private float autoCenterSpeed = 25f;
        [SerializeField] private float curveCentrifugalFactor = 1.2f;

        [Header("Ship Transition (3 Meters)")]
        [SerializeField] private float transitionDistance = 3.0f;
        [SerializeField] private float transitionSpeed = 8f;

        [Header("Ship Combat Movement (Automatic Fixed Speed)")]
        [Tooltip("Constant speed while driving through the combat zone. Requires no player throttle.")]
        [SerializeField] private float shipAutoSpeed = 7f;

        [Header("Events")]
        [SerializeField] private UnityEvent onEnteredShipCombat;
        [SerializeField] private UnityEvent onFinishedRun;

        private IInputService _inputService;
        private Rigidbody _rb;
        private Collider _col;

        private BikeRunState _currentState = BikeRunState.OnPipeline;
        private float _currentDistance;
        private float _currentSpeed;
        private float _rollAngle;

        // Transition cache
        private Vector3 _transitionStartPos;
        private Quaternion _transitionStartRot;
        private Vector3 _knot0WorldPos;
        private Quaternion _knot0WorldRot;
        private float _transitionProgress;

        public BikeRunState CurrentState => _currentState;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _col = GetComponent<Collider>();

            _rb.isKinematic = true;
            _rb.useGravity = false;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;

            _inputService = FindAnyObjectByType<FloatingJoystickInputService>();
        }

        private void Update()
        {
            if (_currentState == BikeRunState.Fallen || _currentState == BikeRunState.ReachedFinishLine) return;

            switch (_currentState)
            {
                case BikeRunState.OnPipeline:
                    HandlePipelineMovement();
                    break;

                case BikeRunState.TransitioningToShip:
                    HandleTransitionToShip();
                    break;

                case BikeRunState.OnShipDeckCombat:
                    HandleShipDeckCombat();
                    break;
            }
        }

        // ==========================================
        // 1. PIPELINE PHASE (SPLINE 1)
        // ==========================================
        private void HandlePipelineMovement()
        {
            if (pipelineSpline == null) return;

            bool isTouching = _inputService != null && _inputService.IsTouching;
            float targetSpeed = isTouching ? pipeMaxSpeed : 0f;
            _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetSpeed, pipeAcceleration * Time.deltaTime);

            if (_currentSpeed <= 0.01f) return;

            float splineLength = pipelineSpline.CalculateLength();
            _currentDistance += _currentSpeed * Time.deltaTime;

            // AFTER REACHING THE END OF THE SPLINE 1
            if (_currentDistance >= splineLength)
            {
                StartShipTransition();
                return;
            }

            float t = Mathf.Clamp01(_currentDistance / splineLength);

            pipelineSpline.Spline.Evaluate(t, out float3 localPos, out float3 localTangent, out float3 localUp);
            Vector3 worldPos = pipelineSpline.transform.TransformPoint(localPos);
            Vector3 forward = pipelineSpline.transform.TransformDirection(math.normalize(localTangent));
            Vector3 up = pipelineSpline.transform.TransformDirection(math.normalize(localUp));
            Vector3 right = Vector3.Cross(up, forward).normalized;

            // Centrifugal push on curves
            float aheadT = Mathf.Clamp01((_currentDistance + 2f) / splineLength);
            pipelineSpline.Spline.Evaluate(aheadT, out _, out float3 aheadTangent, out _);
            Vector3 aheadForward = pipelineSpline.transform.TransformDirection(math.normalize(aheadTangent));
            float bendAngle = Vector3.SignedAngle(forward, aheadForward, up);
            _rollAngle -= bendAngle * curveCentrifugalFactor * Time.deltaTime;

            // LEFT RIGHT BALANCE CONTROL
            float steerInput = isTouching ? _inputService.MoveInput.x : 0f;
            if (Mathf.Abs(steerInput) > 0.05f)
            {
                _rollAngle += steerInput * pipeSteerSensitivity * Time.deltaTime;
            }
            else
            {
                _rollAngle = Mathf.MoveTowards(_rollAngle, 0f, autoCenterSpeed * Time.deltaTime);
            }

            // CHECK ROLLING ON PIPE
            if (Mathf.Abs(_rollAngle) >= fallAngleLimit)
            {
                TriggerPhysicsFall(forward, right, up);
                return;
            }

            // Lock to cylinder surface
            float rad = _rollAngle * Mathf.Deg2Rad;
            Vector3 surfaceNormal = (right * Mathf.Sin(rad) + up * Mathf.Cos(rad)).normalized;

            transform.position = worldPos + (surfaceNormal * pipeRadius);
            transform.rotation = Quaternion.LookRotation(forward, surfaceNormal);
        }

        // ==========================================
        // 2. 3-METER BRIDGE TO SPLINE 2 (KNOT 0)
        // ==========================================
        private void StartShipTransition()
        {
            _currentState = BikeRunState.TransitioningToShip;
            _transitionStartPos = transform.position;
            _transitionStartRot = transform.rotation;
            _transitionProgress = 0f;

            if (shipSpline != null)
            {
                // Sample Knot 0 (t = 0) of Spline 2
                shipSpline.Spline.Evaluate(0f, out float3 startLocalPos, out float3 startLocalTangent, out _);
                _knot0WorldPos = shipSpline.transform.TransformPoint(startLocalPos);

                Vector3 startForward = shipSpline.transform.TransformDirection(math.normalize(startLocalTangent));
                _knot0WorldRot = Quaternion.LookRotation(startForward, Vector3.up);
            }
            else
            {
                // Fallback straight vector if spline 2 is not linked
                _knot0WorldPos = transform.position + (transform.forward * transitionDistance);
                _knot0WorldRot = Quaternion.LookRotation(transform.forward, Vector3.up);
            }
        }

        private void HandleTransitionToShip()
        {
            float step = (transitionSpeed / Mathf.Max(transitionDistance, 0.1f)) * Time.deltaTime;
            _transitionProgress = Mathf.Clamp01(_transitionProgress + step);

            // Interpolate position and flatten orientation upright toward Knot 0
            transform.position = Vector3.Lerp(_transitionStartPos, _knot0WorldPos, _transitionProgress);
            transform.rotation = Quaternion.Slerp(_transitionStartRot, _knot0WorldRot, _transitionProgress);

            // Snapped to Knot 0: hand off completely to Spline 2
            if (_transitionProgress >= 1.0f)
            {
                _currentState = BikeRunState.OnShipDeckCombat;
                _currentDistance = 0f;
                _currentSpeed = shipAutoSpeed;

                onEnteredShipCombat?.Invoke();
                Debug.Log("<color=cyan>[Bike] Docked into Spline 2 (Knot 0). Entering Combat Mode.</color>");
            }
        }

        // ==========================================
        // 3. COMBAT ARENA PHASE (SPLINE 2)
        // ==========================================
        private void HandleShipDeckCombat()
        {
            if (shipSpline == null) return;

            float shipSplineLength = shipSpline.CalculateLength();
            
            // Automatic fixed-speed progression (no touch input required to move)
            _currentDistance += shipAutoSpeed * Time.deltaTime;

            if (_currentDistance >= shipSplineLength)
            {
                _currentState = BikeRunState.ReachedFinishLine;
                _currentSpeed = 0f;
                onFinishedRun?.Invoke();
                Debug.Log("<color=green>[Bike] Reached the finish line on ship deck!</color>");
                return;
            }

            float t = Mathf.Clamp01(_currentDistance / shipSplineLength);

            // Follow Spline 2 directly
            shipSpline.Spline.Evaluate(t, out float3 localPos, out float3 localTangent, out _);
            Vector3 worldPos = shipSpline.transform.TransformPoint(localPos);
            Vector3 forward = shipSpline.transform.TransformDirection(math.normalize(localTangent));

            transform.position = worldPos;
            transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
        }

        // ==========================================
        // PHYSICS FALL LOGIC
        // ==========================================
        private void TriggerPhysicsFall(Vector3 forward, Vector3 right, Vector3 up)
        {
            _currentState = BikeRunState.Fallen;

            _rb.constraints = RigidbodyConstraints.None;
            _rb.isKinematic = false;
            _rb.useGravity = true;
            _rb.angularDamping = 2.5f;

            if (_col != null && pipelineSpline != null)
            {
                Collider[] pipeColliders = pipelineSpline.GetComponentsInChildren<Collider>();
                foreach (Collider pCol in pipeColliders)
                {
                    Physics.IgnoreCollision(_col, pCol, true);
                }
            }

            float rad = _rollAngle * Mathf.Deg2Rad;
            Vector3 outwardDir = (right * Mathf.Sin(rad) + up * Mathf.Cos(rad)).normalized;
            _rb.linearVelocity = (forward * (_currentSpeed * 0.7f)) + (outwardDir * 2.0f) + (Vector3.down * 3.5f);

            float tumbleDirection = Mathf.Sign(_rollAngle);
            Vector3 controlledTumble = (forward * 1.5f * tumbleDirection) + (right * 1.0f);
            _rb.AddTorque(controlledTumble, ForceMode.Impulse);
        }
    }
}