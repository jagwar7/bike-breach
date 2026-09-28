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
    [Tooltip("Layers to collide with (Set to 'Everything' to aim at any object/surface).")]
    [SerializeField] private LayerMask aimLayers = ~0;
    [SerializeField] private float rayDistance = 100f;

    [Header("Sensitivity")]
    [Tooltip("Drag multiplier for moving the reticle.")]
    [SerializeField] private float dragSensitivity = 1f;

    private IInputService _inputService;
    private Vector3 _currentAimWorldPoint;
    private Vector2 _lastTouchPosition;
    private bool _wasTouching;

    // Invisible anchor object representing the crosshair's world point
    private Transform _aimPointTransform;

    private void Awake()
    {
        _inputService = FindAnyObjectByType<FloatingJoystickInputService>();

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        // Create an invisible target for RiderAimController to lock onto
        GameObject aimObj = new GameObject("Crosshair_WorldAimPoint");
        _aimPointTransform = aimObj.transform;
    }

    private void OnEnable()
    {
        _wasTouching = false;

        // Center crosshair when combat begins
        if (crosshairRect != null)
        {
            crosshairRect.position = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
        }

        // Direct RiderAimController to our world-aim point
        if (riderAim != null && _aimPointTransform != null)
        {
            riderAim.target = _aimPointTransform;
        }
    }

    private void Update()
    {
        HandleScreenDrag();
        UpdateAimPointAndTarget();

        // While touching/dragging anywhere, fire rapidly at the exact world point
        if (_inputService != null && _inputService.IsTouching && playerCharacter != null)
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

            // Keep within screen bounds
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

        // 1. Tell RiderAimController which half of the screen the reticle is on
        if (riderAim != null)
        {
            riderAim.SetAimSideFromScreen(crosshairRect.position.x);
        }

        // 2. Cast ray as normal
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