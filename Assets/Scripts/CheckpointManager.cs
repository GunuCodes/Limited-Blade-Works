using UnityEngine;
using System.Collections.Generic;

public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance;
    
    // Dictionary to store checkpoint positions for each scene
    private Dictionary<string, Vector3> checkpoints = new Dictionary<string, Vector3>();

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

    public void SetCheckpoint(string sceneName, Vector3 position)
    {
        if (checkpoints.ContainsKey(sceneName))
        {
            checkpoints[sceneName] = position;
        }
        else
        {
            checkpoints.Add(sceneName, position);
        }
    }

    public bool HasCheckpoint(string sceneName)
    {
        return checkpoints.ContainsKey(sceneName);
    }

    public Vector3 GetCheckpoint(string sceneName)
    {
        return checkpoints.ContainsKey(sceneName) ? checkpoints[sceneName] : Vector3.zero;
    }

    public void ClearCheckpoint(string sceneName)
    {
        if (checkpoints.ContainsKey(sceneName))
        {
            checkpoints.Remove(sceneName);
        }
    }

    public void ClearAllCheckpoints()
    {
        checkpoints.Clear();
    }
}
