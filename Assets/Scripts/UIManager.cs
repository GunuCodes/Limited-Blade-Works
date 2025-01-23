using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
        }

        if (CheckpointManager.Instance != null)
        {
            CheckpointManager.Instance.ClearAllCheckpoints();
        }

        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        yield return StartCoroutine(sceneFader.FadeAndLoadScene(
            SceneFader.FadeDirection.In,
            currentScene
        ));
    }
}
