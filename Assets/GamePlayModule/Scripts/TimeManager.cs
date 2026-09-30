using System.Collections;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance { get; private set; }

    [Header("Slow Motion Settings")]
    [Range(0.05f, 1f)]
    [SerializeField] private float slowMotionScale = 0.5f;

    [Tooltip("Smoothing speed for transitioning between normal and slow time.")]
    [SerializeField] private float transitionSpeed = 5f;

    private float _targetScale = 1f;
    private float _fixedDeltaTimeDefault;
    private Coroutine _timeTransitionRoutine;

    public bool IsSlowMotion => Mathf.Approximately(_targetScale, slowMotionScale);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        _fixedDeltaTimeDefault = Time.fixedDeltaTime;
    }

    private void Update()
    {
        // Smoothly adjust timeScale
        if (!Mathf.Approximately(Time.timeScale, _targetScale))
        {
            Time.timeScale = Mathf.MoveTowards(Time.timeScale, _targetScale, transitionSpeed * Time.unscaledDeltaTime);
            // Crucial: keep fixedDeltaTime proportional to timeScale to prevent physics stutter
            Time.fixedDeltaTime = _fixedDeltaTimeDefault * Time.timeScale;
        }
    }

    /// <summary>
    /// Activates slow-motion (e.g. 0.5x timeScale).
    /// </summary>
    public void EnableSlowMotion(float customScale = -1f)
    {
        _targetScale = customScale > 0f ? customScale : slowMotionScale;
    }

    /// <summary>
    /// Restores standard 1.0x game speed.
    /// </summary>
    public void RestoreNormalTime()
    {
        _targetScale = 1f;
    }

    private void OnDisable()
    {
        // Ensure time scale is restored if object is disabled or destroyed
        Time.timeScale = 1f;
        Time.fixedDeltaTime = _fixedDeltaTimeDefault;
    }

    
}