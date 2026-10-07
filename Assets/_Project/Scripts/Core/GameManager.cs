using System;
using UnityEngine;

public enum GameState
{
    Init,
    Ready,      // Waiting for tap to play
    Playing,    // Active gameplay
    Won,        // Reached finish line
    Failed      // Fallen or player died
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("State")]
    [SerializeField] private GameState currentState = GameState.Init;

    public GameState CurrentState => currentState;

    public event Action<GameState> OnStateChanged;
    public event Action OnGameRetry;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // When the scene loads, level is prepared and we wait for tap
        SetState(GameState.Ready);
    }

    public void SetState(GameState newState)
    {
        if (currentState == newState) return;

        currentState = newState;
        Debug.Log($"<color=yellow>[GameManager] State changed to: {newState}</color>");

        OnStateChanged?.Invoke(currentState);
    }

    /// <summary>
    /// Call this when the player taps the screen on the "Tap To Play" screen.
    /// </summary>
    public void StartGame()
    {
        if (currentState == GameState.Ready)
        {
            SetState(GameState.Playing);
        }
    }

    /// <summary>
    /// Hooked to SplineBikeRunner.onFinishedRun
    /// </summary>
    public void TriggerVictory()
    {
        if (currentState != GameState.Playing) return;

        SetState(GameState.Won);
    }

    /// <summary>
    /// Hooked to SplineBikeRunner.onPlayerDied or bike fall
    /// </summary>
    public void TriggerDefeat()
    {
        if (currentState != GameState.Playing) return;

        SetState(GameState.Failed);
    }

    /// <summary>
    /// Reloads the current level prefab.
    /// </summary>
    public void RetryLevel()
    {
        OnGameRetry?.Invoke();

        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.RestartCurrentLevel();
        }

        SetState(GameState.Playing);
    }

    /// <summary>
    /// Swaps to the next level prefab.
    /// </summary>
    public void NextLevel()
    {
        SetState(GameState.Ready);
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.LoadNextLevel();
        }
    }

    public void CompleteLevel()
    {
        SetState(GameState.Won);
    }


    private void OnDestroy()
    {
        if (Instance == this)
        {
            OnStateChanged = null; // Clear all listeners to prevent stale references
            Instance = null;
        }
    }
}