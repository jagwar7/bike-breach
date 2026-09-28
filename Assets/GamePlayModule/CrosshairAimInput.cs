using UnityEngine;
using Project.Runtime.Core.Input;

public class CrosshairAimInput : MonoBehaviour
{
    [Header("Player References")]
    [SerializeField] private RiderAimController riderAim;
    [SerializeField] private PlayerCharacter playerCharacter;

    [Header("UI Crosshair")]
    [SerializeField] private RectTransform crosshairRect;
    [SerializeField] private Camera mainCamera;

    [Header("Targeting Settings")]
    [Tooltip("Set to 'Everything' so the player aims at whatever the cursor touches.")]
    [SerializeField] private LayerMask aimLayers = ~0;
    [SerializeField] private float rayDistance = 100f;

    [Header("Bottom Deadzone")]
    [Tooltip("Screen height fraction (0.3 = bottom 30%) where aiming/shooting is disabled to avoid firing backwards.")]
    [Range(0f, 0.5f)]
    [SerializeField] private float bottomDeadzoneRatio = 0.3f;

    [Header("Sensitivity")]
    [Tooltip("Drag multiplier for moving the reticle.")]
    [SerializeField] private float dragSensitivity = 1f;

    private IInputService _inputService;
    private Vector3 _currentAimWorldPoint;
    private Vector2 _lastTouchPosition;
    private bool _wasTouching;
    private bool _isInAimZone;

    private Transform _aimPointTransform;

    private void Awake()
    {
        _inputService = FindAnyObjectByType<FloatingJoystickInputService>();

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        GameObject aimObj = new GameObject("Crosshair_WorldAimPoint");
        _aimPointTransform = aimObj.transform;
    }

    private void OnEnable()
    {
        _wasTouching = false;

        // Position crosshair safely above the bottom deadzone on entry
        if (crosshairRect != null)
        {
            crosshairRect.position = new Vector3(Screen.width * 0.5f, Screen.height * 0.55f, 0f);
        }

        if (riderAim != null && _aimPointTransform != null)
        {
            riderAim.target = _aimPointTransform;
        }
    }

    private void Update()
    {
        HandleScreenDrag();
        UpdateAimPointAndTarget();

        // Fire rapidly whenever touching and aiming above the deadzone
        if (_isInAimZone && _inputService != null && _inputService.IsTouching && playerCharacter != null)
        {
            playerCharacter.TryShoot(_currentAimWorldPoint);
        }
    }

    private void HandleScreenDrag()
    {
        if (_inputService == null || !_inputService.IsTouching)
        {
            _wasTouching = false;
            return;
        }

        Vector2 currentTouchPosition = GetCurrentScreenPosition();

        if (!_wasTouching)
        {
            _lastTouchPosition = currentTouchPosition;
            _wasTouching = true;
            return;
        }

        Vector2 delta = (currentTouchPosition - _lastTouchPosition) * dragSensitivity;
        _lastTouchPosition = currentTouchPosition;

        if (delta.sqrMagnitude > 0.001f && crosshairRect != null)
        {
            crosshairRect.position += (Vector3)delta;

            Vector3 pos = crosshairRect.position;
            pos.x = Mathf.Clamp(pos.x, 0f, Screen.width);
            pos.y = Mathf.Clamp(pos.y, 0f, Screen.height);
            crosshairRect.position = pos;
        }
    }

    private Vector2 GetCurrentScreenPosition()
    {
        if (Input.touchCount > 0)
        {
            return Input.GetTouch(0).position;
        }

        return Input.mousePosition;
    }

    private void UpdateAimPointAndTarget()
    {
        if (crosshairRect == null || mainCamera == null) return;

        // Verify cursor is above bottom deadzone
        float bottomThreshold = Screen.height * bottomDeadzoneRatio;
        _isInAimZone = crosshairRect.position.y > bottomThreshold;

        // If in deadzone, drop the target so the torso returns to neutral
        if (!_isInAimZone)
        {
            if (riderAim != null) riderAim.target = null;
            return;
        }

        Ray ray = mainCamera.ScreenPointToRay(crosshairRect.position);

        if (Physics.Raycast(ray, out RaycastHit hit, rayDistance, aimLayers))
        {
            _currentAimWorldPoint = hit.point;
        }
        else
        {
            _currentAimWorldPoint = ray.GetPoint(rayDistance);
        }

        if (_aimPointTransform != null)
        {
            _aimPointTransform.position = _currentAimWorldPoint;

            if (riderAim != null && riderAim.target != _aimPointTransform)
            {
                riderAim.target = _aimPointTransform;
            }
        }
    }

    private void OnDestroy()
    {
        if (_aimPointTransform != null)
        {
            Destroy(_aimPointTransform.gameObject);
        }
    }
}