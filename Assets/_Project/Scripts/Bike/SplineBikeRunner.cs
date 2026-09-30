using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Splines;
using Unity.Mathematics;
using Project.Runtime.Core.Input;

    
    public enum BikeRunState
    {
        OnPipeline,
        TransitioningToShip,
        OnShipDeckCombat,
        ReachedFinishLine,
        Fallen,
        PlayerDead
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

        [Header("Death Behavior")]
        [Tooltip("Reference to the player child character. Will unparent on death.")]
        [SerializeField] private Transform playerCharacterTransform;

        [Tooltip("If true, the bike drops onto its physics collider and skids to a stop instead of instantly freezing.")]
        [SerializeField] private bool usePhysicsCrashOnDeath = true;

        [Header("Events")]
        [SerializeField] private UnityEvent onEnteredShipCombat;
        [SerializeField] private UnityEvent onFinishedRun;
        [SerializeField] private UnityEvent onPlayerDied;

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

            if (playerCharacterTransform == null)
            {
                // Auto-detect PLAYER_CH in children if not assigned via Inspector
                Transform found = transform.Find("PLAYER_CH");
                if (found != null)
                {
                    playerCharacterTransform = found;
                }
            }
        }

        private void Update()
        {
            if (_currentState == BikeRunState.Fallen || 
                _currentState == BikeRunState.ReachedFinishLine || 
                _currentState == BikeRunState.PlayerDead) return;

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

        /// <summary>
        /// Call this method when the player's health drops to 0.
        /// </summary>
        public void OnPlayerDied()
        {
            if (_currentState == BikeRunState.PlayerDead || _currentState == BikeRunState.Fallen) return;

            _currentState = BikeRunState.PlayerDead;
            _currentSpeed = 0f;

            // 1. Unparent player so ragdoll/death animation is decoupled from the bike
            if (playerCharacterTransform != null)
            {
                playerCharacterTransform.SetParent(null, true);
            }

            // 2. Tumble or skid the bike with physics
            if (usePhysicsCrashOnDeath)
            {
                _rb.isKinematic = false;
                _rb.useGravity = true;
                _rb.constraints = RigidbodyConstraints.None;

                float forwardVelocity = _currentState == BikeRunState.OnShipDeckCombat ? shipAutoSpeed * 0.8f : _currentSpeed * 0.8f;
                _rb.linearVelocity = transform.forward * forwardVelocity;
                _rb.AddTorque(transform.forward * 2f + transform.right * 1f, ForceMode.Impulse);
            }

            onPlayerDied?.Invoke();
            Debug.Log("<color=red>[Bike] Player died. Spline progression halted.</color>");
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

            // Balance control
            float steerInput = isTouching ? _inputService.MoveInput.x : 0f;
            if (Mathf.Abs(steerInput) > 0.05f)
            {
                _rollAngle += steerInput * pipeSteerSensitivity * Time.deltaTime;
            }
            else
            {
                _rollAngle = Mathf.MoveTowards(_rollAngle, 0f, autoCenterSpeed * Time.deltaTime);
            }

            if (Mathf.Abs(_rollAngle) >= fallAngleLimit)
            {
                TriggerPhysicsFall(forward, right, up);
                return;
            }

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
                shipSpline.Spline.Evaluate(0f, out float3 startLocalPos, out float3 startLocalTangent, out _);
                _knot0WorldPos = shipSpline.transform.TransformPoint(startLocalPos);

                Vector3 startForward = shipSpline.transform.TransformDirection(math.normalize(startLocalTangent));
                _knot0WorldRot = Quaternion.LookRotation(startForward, Vector3.up);
            }
            else
            {
                _knot0WorldPos = transform.position + (transform.forward * transitionDistance);
                _knot0WorldRot = Quaternion.LookRotation(transform.forward, Vector3.up);
            }
        }

        private void HandleTransitionToShip()
        {
            float step = (transitionSpeed / Mathf.Max(transitionDistance, 0.1f)) * Time.deltaTime;
            _transitionProgress = Mathf.Clamp01(_transitionProgress + step);

            transform.position = Vector3.Lerp(_transitionStartPos, _knot0WorldPos, _transitionProgress);
            transform.rotation = Quaternion.Slerp(_transitionStartRot, _knot0WorldRot, _transitionProgress);

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
