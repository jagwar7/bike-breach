using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Panels")]
    [SerializeField] private GameObject readyPanel;       // "Tap to Play" overlay
    [SerializeField] private GameObject gameplayPanel;    // In-game HUD (progress, reticle, etc.)
    [SerializeField] private GameObject winPanel;         // Victory screen
    [SerializeField] private GameObject failPanel;        // Defeat screen

    [Header("Text Displays")]
    [SerializeField] private TextMeshProUGUI levelTextReady; // "Level 1"
    [SerializeField] private TextMeshProUGUI levelTextWin;

    [Header("Buttons")]
    [SerializeField] private Button tapToPlayButton;
    [SerializeField] private Button nextLevelButton;
    [SerializeField] private Button retryButton;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Hook button listeners
        if (tapToPlayButton != null)
            tapToPlayButton.onClick.AddListener(OnTapToPlayClicked);

        if (nextLevelButton != null)
            nextLevelButton.onClick.AddListener(OnNextLevelClicked);

        if (retryButton != null)
            retryButton.onClick.AddListener(OnRetryClicked);
    }

    private void OnEnable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged += HandleGameStateChanged;

        if (LevelManager.Instance != null)
            LevelManager.Instance.OnLevelLoaded += HandleLevelLoaded;
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged -= HandleGameStateChanged;

        if (LevelManager.Instance != null)
            LevelManager.Instance.OnLevelLoaded -= HandleLevelLoaded;
    }

    private void Start()
    {
        // 1. Hide all panels first
        HideAllPanels();

        // 2. Safely update labels
        UpdateLevelLabels();

        // 3. Force ReadyPanel active if game is Ready or Init
        if (GameManager.Instance != null)
        {
            HandleGameStateChanged(GameManager.Instance.CurrentState);
        }
        else
        {
            if (readyPanel != null) readyPanel.SetActive(true);
        }
    }

    private void HandleGameStateChanged(GameState state)
    {
        // Hide all screens first
        HideAllPanels();

        switch (state)
        {
            case GameState.Ready:
                UpdateLevelLabels();
                if (readyPanel != null) readyPanel.SetActive(true);
                break;

            case GameState.Playing:
                if (gameplayPanel != null) gameplayPanel.SetActive(true);
                break;

            case GameState.Won:
                if (winPanel != null) winPanel.SetActive(true);
                break;

            case GameState.Failed:
                if (failPanel != null) failPanel.SetActive(true);
                break;
        }
    }

    private void HandleLevelLoaded(LevelData level)
    {
        UpdateLevelLabels();
    }

    private void UpdateLevelLabels()
    {
        int displayLevel = (LevelManager.Instance != null) ? LevelManager.Instance.CurrentLevelIndex + 1 : 1;
        string text = $"LEVEL {displayLevel}";

        if (levelTextReady != null) levelTextReady.text = text;
        if (levelTextWin != null) levelTextWin.text = text;
    }

    private void HideAllPanels()
    {
        if (readyPanel != null) readyPanel.SetActive(false);
        if (gameplayPanel != null) gameplayPanel.SetActive(false);
        if (winPanel != null) winPanel.SetActive(false);
        if (failPanel != null) failPanel.SetActive(false);
    }

    // Button Callbacks
    private void OnTapToPlayClicked()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.StartGame();
    }

    private void OnNextLevelClicked()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.NextLevel();
    }

    private void OnRetryClicked()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.RetryLevel();
    }
}