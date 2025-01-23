using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    public SceneFader sceneFader;

    [SerializeField] private GameObject deathScreen;
    [SerializeField] private Button respawnButton;
    [SerializeField] private ColorBlock respawnButtonColors;

    private bool isDeathScreenActive = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
        DontDestroyOnLoad(gameObject);
        sceneFader = GetComponentInChildren<SceneFader>();
        
        SetupRespawnButton();
    }

    private void Update()
    {
        if (isDeathScreenActive)
        {
            if (Input.GetMouseButtonDown(0))
            {
                StartRespawn();
            }
        }
    }

    private void SetupRespawnButton()
    {
        if (respawnButton != null)
        {
            respawnButton.onClick.RemoveAllListeners();
            respawnButton.onClick.AddListener(StartRespawn);

            ColorBlock colors = respawnButton.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 1f);
            colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
            colors.selectedColor = new Color(1f, 1f, 1f, 1f);
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            colors.fadeDuration = 0.1f;
            respawnButton.colors = colors;
        }
    }

    public IEnumerator ActivateDeathScreen()
    {
        yield return new WaitForSeconds(1f);
        StartCoroutine(sceneFader.Fade(SceneFader.FadeDirection.In));

        yield return new WaitForSeconds(1f);
        
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var eventSystem = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), 
                typeof(UnityEngine.EventSystems.StandaloneInputModule));
        }

        deathScreen.SetActive(true);
        isDeathScreenActive = true;
        
        if (respawnButton != null)
        {
            respawnButton.interactable = true;
        }
    }

    public void StartRespawn()
    {
        if (!isDeathScreenActive) return;
        isDeathScreenActive = false;
        StartCoroutine(RespawnPlayer());
    }

    private IEnumerator RespawnPlayer()
    {
        deathScreen.SetActive(false);

        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.Health = PlayerController.Instance.maxHealth;
            PlayerController.Instance.pState.alive = true;
            PlayerController.Instance.pState.invincible = false;
            PlayerController.Instance.pState.dying = false;
        }

        if (CheckpointManager.Instance != null)
        {
            CheckpointManager.Instance.ClearAllCheckpoints();
        }

        // Load Level_1 without fade in (screen stays black)
        SceneManager.LoadScene("Level_1");
        
        // Wait a frame to ensure scene is loaded
        yield return null;
        
        // Find the start point in the new scene
        SceneTransition[] transitions = FindObjectsOfType<SceneTransition>();
        Transform startPoint = null;
        
        foreach (var transition in transitions)
        {
            if (transition.GetStartPoint() != null)
            {
                startPoint = transition.GetStartPoint();
                break;
            }
        }
        
        // Position player at start point
        if (startPoint != null && PlayerController.Instance != null)
        {
            PlayerController.Instance.transform.position = startPoint.position;
        }
        
        // Only fade out from black
        if (sceneFader != null)
        {
            yield return StartCoroutine(sceneFader.Fade(SceneFader.FadeDirection.Out));
        }
    }
}
