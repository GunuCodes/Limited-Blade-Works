using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [SerializeField] private bool activateOnStart = false;
    [SerializeField] private GameObject checkpointVisual; // Optional visual indicator
    private bool isActivated = false;

    private void Start()
    {
        if (activateOnStart && CheckpointManager.Instance != null)
        {
            SetCheckpoint();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isActivated && collision.CompareTag("Player"))
        {
            SetCheckpoint();
        }
    }

    private void SetCheckpoint()
    {
        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        CheckpointManager.Instance?.SetCheckpoint(currentScene, transform.position);
        isActivated = true;

        // Activate visual indicator if assigned
        if (checkpointVisual != null)
        {
            checkpointVisual.SetActive(true);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
