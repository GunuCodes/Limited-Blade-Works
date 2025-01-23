using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class MainMenu : MonoBehaviour
{
    [Header("Menu Settings")]
    [SerializeField] private string firstLevelScene = "Level1"; // The scene to load when starting game
    [SerializeField] private float fadeTime = 1f; // How long the fade transition takes

    [Header("UI References")]
    [SerializeField] private Button startButton; // Reference to the Start Game button
    [SerializeField] private Image fadeImage; // Reference to full-screen fade image
    [SerializeField] private CanvasGroup menuCanvas; // Reference to the menu's canvas group

    private void Awake()
    {
        // Setup the start button
        if (startButton != null)
        {
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(StartGame);
        }

        // Ensure fade image starts fully opaque
        if (fadeImage != null)
        {
            fadeImage.color = new Color(0, 0, 0, 1);
        }
    }

    private void Start()
    {
        // Fade in the menu when the game starts
        StartCoroutine(FadeIn());
    }

    public void StartGame()
    {
        // Disable the button to prevent multiple clicks
        if (startButton != null)
        {
            startButton.interactable = false;
        }
        
        // Start the fade out transition
        StartCoroutine(FadeOutAndLoadGame());
    }

    private IEnumerator FadeIn()
    {
        float elapsedTime = 0;
        
        // Ensure everything is visible except the fade image
        if (menuCanvas != null)
        {
            menuCanvas.alpha = 1;
        }

        // Fade from black to transparent
        while (elapsedTime < fadeTime)
        {
            elapsedTime += Time.deltaTime;
            float alpha = 1 - (elapsedTime / fadeTime);
            
            if (fadeImage != null)
            {
                fadeImage.color = new Color(0, 0, 0, alpha);
            }
            
            yield return null;
        }

        // Ensure fade image is completely transparent
        if (fadeImage != null)
        {
            fadeImage.color = new Color(0, 0, 0, 0);
        }
    }

    private IEnumerator FadeOutAndLoadGame()
    {
        float elapsedTime = 0;

        // Fade to black
        while (elapsedTime < fadeTime)
        {
            elapsedTime += Time.deltaTime;
            float alpha = elapsedTime / fadeTime;
            
            if (fadeImage != null)
            {
                fadeImage.color = new Color(0, 0, 0, alpha);
            }
            
            // Also fade out the menu elements
            if (menuCanvas != null)
            {
                menuCanvas.alpha = 1 - alpha;
            }
            
            yield return null;
        }

        // Ensure screen is fully black
        if (fadeImage != null)
        {
            fadeImage.color = new Color(0, 0, 0, 1);
        }

        // Load the first level
        SceneManager.LoadScene(firstLevelScene);
    }
}
