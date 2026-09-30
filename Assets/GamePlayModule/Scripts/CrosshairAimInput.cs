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
    [SerializeField] private float bottomDeadzoneRatio = 0.4f;

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

        // Auto-assign player character if slot was missed in inspector
        if (playerCharacter == null)
        {
            playerCharacter = FindAnyObjectByType<PlayerCharacter>();
        }

        if (riderAim == null)
        {
            riderAim = FindAnyObjectByType<RiderAimController>();
        }
    }

    private void OnEnable()
    {
        _wasTouching = false;

        // Position crosshair safely in upper screen on start
        if (crosshairRect != null)
        {
            crosshairRect.position = new Vector3(Screen.width * 0.5f, Screen.height * 0.75f, 0f);
        }

        if (riderAim != null && _aimPointTransform != null)
        {
            riderAim.target = _aimPointTransform;
        }
    }

    private void Update()
    {
        bool isScreenTouching = IsInputActive();

        HandleScreenDrag(isScreenTouching);
        UpdateAimPointAndTarget();

        // Fire rapidly whenever screen is touched/dragged and reticle is above the deadzone
        if (_isInAimZone && isScreenTouching && playerCharacter != null)
        {
            playerCharacter.TryShoot(_currentAimWorldPoint);
        }
    }

    /// <summary>
    /// Checks touch input directly, falling back safely without relying exclusively on external service state.
    /// </summary>
    private bool IsInputActive()
    {
        if (_inputService != null && _inputService.IsTouching)
        {
            return true;
        }

        return Input.GetMouseButton(0) || Input.touchCount > 0;
    }

    private void HandleScreenDrag(bool isTouching)
    {
        if (!isTouching)
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

        // Convert UI rect screen coordinate accurately regardless of Canvas Render Mode
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(null, crosshairRect.position);

        float bottomThreshold = Screen.height * bottomDeadzoneRatio;
        _isInAimZone = screenPos.y > bottomThreshold;

        // If reticle enters bottom deadzone, drop target so rider returns to neutral riding posture
        if (!_isInAimZone)
        {
            if (riderAim != null) riderAim.target = null;
            return;
        }

        Ray ray = mainCamera.ScreenPointToRay(screenPos);

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