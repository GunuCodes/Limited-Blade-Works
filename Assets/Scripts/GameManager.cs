using UnityEngine;

public class GameManager : MonoBehaviour
{
    public string transitionedFromScene;

    [Header("Pause Menu Settings")]
    [SerializeField] private PauseMenu pauseMenu;
    [SerializeField] private float fadeTime = 0.3f;
    
    public bool gameIsPaused { get; private set; }

    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    private void Update()
    {
        // Only allow pausing if we have a pause menu and aren't in the main menu
        if (pauseMenu != null && !UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Equals("MainMenu"))
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (gameIsPaused)
                {
                    UnpauseGame();
                }
                else
                {
                    PauseGame();
                }
            }
        }
    }

    public void PauseGame()
    {
        if (pauseMenu != null)
        {
            pauseMenu.FadeUIIn(fadeTime);
            Time.timeScale = 0;
            gameIsPaused = true;
        }
    }

    public void UnpauseGame()
    {
        if (pauseMenu != null)
        {
            pauseMenu.FadeUIOut(fadeTime);
            Time.timeScale = 1;
            gameIsPaused = false;
        }
    }
}
