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
    [Header("Wheel Settings")]
    [SerializeField] private Transform frontWheel;
    [SerializeField] private Transform rearWheel;
    [Tooltip("Multiplier to match visual wheel speed to bike movement")]
    [SerializeField] private float wheelSpinSpeedMultiplier = 350f;

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
    [SerializeField] private float shipAutoSpeed = 15f;

    [Header("Death Behavior")]
    [SerializeField] private Transform playerCharacterTransform;
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
            Transform found = transform.Find("PLAYER_CH");
            if (found != null)
            {
                playerCharacterTransform = found;
            }
        }
    }

    private void Update()
    {
        // IF GAME HASNT STARTED, DO NOT PROGRESS BIKE
        if(GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing) return;


        // Stop all movement and wheel spinning if fallen, dead, or completed
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
        RotateWheels();
    }

    private void RotateWheels()
    {
        if (_currentSpeed <= 0.01f) return;

        float rotationStep = _currentSpeed * wheelSpinSpeedMultiplier * Time.deltaTime;

        // Use Vector3.left (or -Vector3.right) to reverse the spin direction
        if (frontWheel != null)
        {
            frontWheel.Rotate(Vector3.left, rotationStep, Space.Self);
        }

        if (rearWheel != null)
        {
            rearWheel.Rotate(Vector3.left, rotationStep, Space.Self);
        }
    }

    public void OnPlayerDied()
    {
        if (_currentState == BikeRunState.PlayerDead || _currentState == BikeRunState.Fallen) return;

        _currentState = BikeRunState.PlayerDead;
        _currentSpeed = 0f;

        if (playerCharacterTransform != null)
        {
            playerCharacterTransform.SetParent(null, true);
        }

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

        float aheadT = Mathf.Clamp01((_currentDistance + 2f) / splineLength);
        pipelineSpline.Spline.Evaluate(aheadT, out _, out float3 aheadTangent, out _);
        Vector3 aheadForward = pipelineSpline.transform.TransformDirection(math.normalize(aheadTangent));
        float bendAngle = Vector3.SignedAngle(forward, aheadForward, up);
        _rollAngle -= bendAngle * curveCentrifugalFactor * Time.deltaTime;

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

    private void StartShipTransition()
    {
        _currentState = BikeRunState.TransitioningToShip;
        _transitionStartPos = transform.position;
        _transitionStartRot = transform.rotation;
        _transitionProgress = 0f;
        _currentSpeed = transitionSpeed;

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

    private void HandleShipDeckCombat()
    {
        if (shipSpline == null) return;

        _currentSpeed = shipAutoSpeed;
        float shipSplineLength = shipSpline.CalculateLength();
        _currentDistance += _currentSpeed * Time.deltaTime;

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

        if(GameManager.Instance != null)
        {
            GameManager.Instance.TriggerDefeat();
        }
    }


    /// <summary>
    /// Called by LevelManager to assign new splines and reset the bike state.
    /// </summary>
    public void InitializeLevel(SplineContainer pipe, SplineContainer ship)
    {
        pipelineSpline = pipe;
        shipSpline = ship;

        // Reset progression variables
        _currentState = BikeRunState.OnPipeline;
        _currentDistance = 0f;
        _currentSpeed = 0f;
        _rollAngle = 0f;
        _transitionProgress = 0f;

        // Reset Rigidbody physics to Kinematic
        if (_rb == null) _rb = GetComponent<Rigidbody>();
        _rb.isKinematic = true;
        _rb.useGravity = false;
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        _rb.constraints = RigidbodyConstraints.None;

        // Re-parent player character if it was unparented during death
        if (playerCharacterTransform != null && playerCharacterTransform.parent != transform)
        {
            playerCharacterTransform.SetParent(transform, true);
            playerCharacterTransform.localPosition = Vector3.zero;
            playerCharacterTransform.localRotation = Quaternion.identity;
        }

        // Align bike directly to start of pipeline spline if available
        if (pipelineSpline != null)
        {
            pipelineSpline.Spline.Evaluate(0f, out float3 startPos, out float3 startTangent, out float3 startUp);
            Vector3 worldPos = pipelineSpline.transform.TransformPoint(startPos);
            Vector3 forward = pipelineSpline.transform.TransformDirection(math.normalize(startTangent));
            Vector3 up = pipelineSpline.transform.TransformDirection(math.normalize(startUp));

            transform.position = worldPos + (up * pipeRadius);
            transform.rotation = Quaternion.LookRotation(forward, up);
        }
    }



    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.GetComponent<CameraAngleTrigger>() != null)
        {
            shipAutoSpeed-=8;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if(other.gameObject.GetComponent<CameraAngleTrigger>() != null)
        {
            shipAutoSpeed+=8;
        }
    }


}