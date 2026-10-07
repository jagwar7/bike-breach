using System;
using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("Level Prefabs")]
    [Tooltip("Add all your level prefabs here in order (Level_01, Level_02, etc.).")]
    [SerializeField] private List<LevelData> levelPrefabs = new List<LevelData>();

    [Header("Scene References")]
    [SerializeField] private SplineBikeRunner bikeRunner;

    public LevelData CurrentLevelInstance { get; private set; }
    public int CurrentLevelIndex { get; private set; }

    public event Action<LevelData> OnLevelLoaded;

    private const string LevelPrefKey = "SavedLevelIndex";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Auto-find runner if not assigned in Inspector
        if (bikeRunner == null)
        {
            bikeRunner = FindAnyObjectByType<SplineBikeRunner>();
        }
    }

    private void Start()
    {
        // 0 on first launch, or saved index from previous play
        CurrentLevelIndex = PlayerPrefs.GetInt(LevelPrefKey, 0);

        LoadCurrentLevel();
    }

    /// <summary>
    /// Spawns the current level prefab and binds the bike to its splines.
    /// </summary>
    public void LoadCurrentLevel()
    {
        if (levelPrefabs == null || levelPrefabs.Count == 0)
        {
            Debug.LogError("[LevelManager] No level prefabs assigned in Inspector!");
            return;
        }

        // Clean up previous level instance if one exists
        if (CurrentLevelInstance != null)
        {
            Destroy(CurrentLevelInstance.gameObject);
        }

        // Loop back through levels if player surpasses the total amount
        int prefabIndex = CurrentLevelIndex % levelPrefabs.Count;
        LevelData prefabToSpawn = levelPrefabs[prefabIndex];

        // Instantiate at world origin
        CurrentLevelInstance = Instantiate(prefabToSpawn, Vector3.zero, Quaternion.identity);

        // Bind the dual splines to the runner
        SetupPlayerForLevel(CurrentLevelInstance);

        OnLevelLoaded?.Invoke(CurrentLevelInstance);
        Debug.Log($"<color=green>[LevelManager] Loaded Level {CurrentLevelIndex + 1} (Prefab Index: {prefabIndex})</color>");
    }

    private void SetupPlayerForLevel(LevelData level)
    {
        if (bikeRunner == null || level == null) return;

        // Teleport to spawn point if designated
        if (level.SpawnPoint != null)
        {
            bikeRunner.transform.position = level.SpawnPoint.position;
            bikeRunner.transform.rotation = level.SpawnPoint.rotation;
        }

        // Pass both splines to initialize the runner
        bikeRunner.InitializeLevel(level.PipelineSpline, level.ShipSpline);
    }

    /// <summary>
    /// Call when the player finishes the run to progress to the next track.
    /// </summary>
    public void LoadNextLevel()
    {
        CurrentLevelIndex++;
        PlayerPrefs.SetInt(LevelPrefKey, CurrentLevelIndex);
        PlayerPrefs.Save();

        LoadCurrentLevel();
    }

    /// <summary>
    /// Call when the player falls or dies to reload the track.
    /// </summary>
    public void RestartCurrentLevel()
    {
        LoadCurrentLevel();
    }
}