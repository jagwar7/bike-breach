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
    [Tooltip("Layers to hit when not aiming at an enemy (ground, containers, walls).")]
    [SerializeField] private LayerMask aimLayers = ~0;
    [SerializeField] private float rayDistance = 100f;

    [Header("Screen Space Aim Assist")]
    [Tooltip("Leave at 0 to automatically use the crosshair UI radius. Set > 0 to manually define pixel radius.")]
    [SerializeField] private float customScreenRadius = 0f;

    [Header("Bottom Deadzone")]
    [Tooltip("Screen height fraction (0.3 = bottom 30%) where aiming/shooting is disabled to avoid firing backwards.")]
    [Range(0f, 0.5f)]
    [SerializeField] private float bottomDeadzoneRatio = 0.4f;

    [Header("Sensitivity")]
    [Tooltip("Drag multiplier for moving the reticle.")]
    [SerializeField] private float dragSensitivity = 1f;

    [Header("Raycast Debug")]
    [SerializeField] private Transform weaponMuzzle;

    private IInputService _inputService;
    private Vector3 _currentAimWorldPoint;
    private Vector2 _lastTouchPosition;
    private bool _wasTouching;
    private bool _isInAimZone;

    private Transform _aimPointTransform;

    public Vector3 CurrentAimWorldPoint => _currentAimWorldPoint;

    private void Awake()
    {
        _inputService = FindAnyObjectByType<FloatingJoystickInputService>();

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        GameObject aimObj = new GameObject("Crosshair_WorldAimPoint");
        _aimPointTransform = aimObj.transform;

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

        if (_isInAimZone && isScreenTouching && playerCharacter != null)
        {
            playerCharacter.TryShoot(_currentAimWorldPoint);
        }
    }

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

        Canvas canvas = crosshairRect.GetComponentInParent<Canvas>();
        Camera uiCam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) 
            ? canvas.worldCamera 
            : null;

        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(uiCam, crosshairRect.position);

        float bottomThreshold = Screen.height * bottomDeadzoneRatio;
        _isInAimZone = screenPos.y > bottomThreshold;

        if (!_isInAimZone)
        {
            if (riderAim != null) riderAim.target = null;
            return;
        }

        // 1. Calculate the visible reticle radius in real screen pixels
        float reticlePixelRadius = customScreenRadius > 0f 
            ? customScreenRadius 
            : (crosshairRect.rect.width * 0.5f * (canvas != null ? canvas.scaleFactor : 1f));

        // 2. Check if any enemy falls inside the 2D crosshair circle
        Transform lockedEnemy = FindEnemyInsideReticle(screenPos, reticlePixelRadius);

        if (lockedEnemy != null)
        {
            // Snap to the center of the enemy's main collider/torso
            Collider enemyCol = lockedEnemy.GetComponentInChildren<Collider>();
            _currentAimWorldPoint = (enemyCol != null) ? enemyCol.bounds.center : lockedEnemy.position + Vector3.up * 1.0f;
        }
        else
        {
            // 3. Fallback: Raycast against ground/environment if reticle is empty
            Ray ray = mainCamera.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out RaycastHit hit, rayDistance, aimLayers))
            {
                _currentAimWorldPoint = hit.point;
            }
            else
            {
                _currentAimWorldPoint = ray.GetPoint(rayDistance);
            }
        }

        // Draw debug line in Scene view
        Vector3 origin = weaponMuzzle != null 
            ? weaponMuzzle.position 
            : (playerCharacter != null ? playerCharacter.transform.position + Vector3.up * 1.2f : transform.position);

        Debug.DrawLine(origin, _currentAimWorldPoint, Color.red);

        // Update target transform for IK / aiming
        if (_aimPointTransform != null)
        {
            _aimPointTransform.position = _currentAimWorldPoint;

            if (riderAim != null && riderAim.target != _aimPointTransform)
            {
                riderAim.target = _aimPointTransform;
            }
        }
    }

    /// <summary>
    /// Searches for any living enemy whose chest coordinate falls within the crosshair circle on screen.
    /// </summary>
    private Transform FindEnemyInsideReticle(Vector2 reticleCenter, float radiusInPixels)
    {
        Transform closestEnemy = null;
        float closestDist = float.MaxValue;

        Health[] allHealths = FindObjectsOfType<Health>();

        for (int i = 0; i < allHealths.Length; i++)
        {
            Health health = allHealths[i];

            // Ignore player character or destroyed/disabled entities
            if (!health.enabled || health.GetComponent<PlayerCharacter>() != null || health.GetComponentInParent<PlayerCharacter>() != null)
            {
                continue;
            }

            // Estimate torso height
            Vector3 worldPos = health.transform.position + Vector3.up * 1.0f;
            Vector3 screenPoint = mainCamera.WorldToScreenPoint(worldPos);

            // Ignore objects behind camera
            if (screenPoint.z <= 0) continue;

            float dist = Vector2.Distance(reticleCenter, (Vector2)screenPoint);

            // Target is inside the UI circle radius
            if (dist <= radiusInPixels && dist < closestDist)
            {
                closestDist = dist;
                closestEnemy = health.transform;
            }
        }

        return closestEnemy;
    }

    private void OnDestroy()
    {
        if (_aimPointTransform != null)
        {
            Destroy(_aimPointTransform.gameObject);
        }
    }
}