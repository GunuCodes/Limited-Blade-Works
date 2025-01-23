using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.SceneManagement;

public class SceneTransition : MonoBehaviour
{
    [SerializeField] private string transitionTo; //Represents the scene to transition to

    [SerializeField] private Transform startPoint; //Defines the player's entry point in the scene

    [SerializeField] private Vector2 exitDirection; //Specifies the direction for the player's exit

    [SerializeField] private float exitTime; //Determines the time it takes for the player to exit the scene transition

    [Header("Checkpoint Settings")]
    [SerializeField] private bool isCheckpoint = false;  // Is this transition a checkpoint?
    [SerializeField] private Transform checkpointPosition;  // Optional custom checkpoint position

    // Start is called before the first frame update
    private void Start()
    {
        StartCoroutine(SafeInitializeScene());
    }

    private IEnumerator SafeInitializeScene()
    {
        // Wait for next frame to ensure all objects are initialized
        yield return null;

        string currentScene = SceneManager.GetActiveScene().name;
        
        // Check if we're coming from the scene we're transitioning to
        if (GameManager.Instance?.transitionedFromScene == transitionTo)
        {
            // Wait for PlayerController initialization
            yield return StartCoroutine(WaitForPlayerController());

            // Position the player
            if (PlayerController.Instance != null)
            {
                // Check if there's a checkpoint for this scene
                if (CheckpointManager.Instance != null && CheckpointManager.Instance.HasCheckpoint(currentScene))
                {
                    PlayerController.Instance.transform.position = CheckpointManager.Instance.GetCheckpoint(currentScene);
                }
                else if (startPoint != null)
                {
                    PlayerController.Instance.transform.position = startPoint.position;
                }

                try
                {
                    StartCoroutine(PlayerController.Instance.WalkIntoNewScene(exitDirection, exitTime));
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error in WalkIntoNewScene: {e.Message}");
                }
            }
        }

        // Fade out
        if (UIManager.Instance?.sceneFader != null)
        {
            StartCoroutine(UIManager.Instance.sceneFader.Fade(SceneFader.FadeDirection.Out));
        }
    }

    private IEnumerator WaitForPlayerController()
    {
        int maxAttempts = 10;
        int attempts = 0;
        while (PlayerController.Instance == null || PlayerController.Instance.pState == null)
        {
            yield return new WaitForSeconds(0.1f);
            attempts++;
            if (attempts >= maxAttempts)
            {
                Debug.LogError("Failed to find initialized PlayerController!");
                yield break;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D _other)
    {
        if (!_other.CompareTag("Player")) return;

        // Set checkpoint if this is a checkpoint transition
        if (isCheckpoint)
        {
            string currentScene = SceneManager.GetActiveScene().name;
            Vector3 checkpointPos = checkpointPosition != null ? 
                                  checkpointPosition.position : 
                                  _other.transform.position;
            
            CheckpointManager.Instance?.SetCheckpoint(currentScene, checkpointPos);
        }

        StartCoroutine(SafeSceneTransition());
    }

    private IEnumerator SafeSceneTransition()
    {
        if (string.IsNullOrEmpty(transitionTo))
        {
            Debug.LogError("Transition To scene name not set!");
            yield break;
        }

        // Store current scene name
        if (GameManager.Instance != null)
        {
            GameManager.Instance.transitionedFromScene = SceneManager.GetActiveScene().name;
        }

        // Set player state
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.pState.cutscene = true;
            PlayerController.Instance.pState.invincible = true;
        }

        // Perform scene transition
        if (UIManager.Instance?.sceneFader != null)
        {
            yield return StartCoroutine(UIManager.Instance.sceneFader.FadeAndLoadScene(
                SceneFader.FadeDirection.In,
                transitionTo
            ));
        }
        else
        {
            SceneManager.LoadScene(transitionTo);
        }
    }
}