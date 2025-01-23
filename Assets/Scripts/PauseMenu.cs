using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PauseMenu : MonoBehaviour
{
    public static PauseMenu Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private CanvasGroup menuCanvas;

    private void Awake()
    {
        // Setup singleton pattern with DontDestroyOnLoad
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        // Hide menu initially
        if (menuCanvas != null)
        {
            menuCanvas.alpha = 0;
            menuCanvas.interactable = false;
            menuCanvas.blocksRaycasts = false;
        }
    }

    public void FadeUIIn(float fadeTime)
    {
        StopAllCoroutines();
        StartCoroutine(FadeCanvasGroup(menuCanvas, 0, 1, fadeTime));
        EnableUI(true);
    }

    public void FadeUIOut(float fadeTime)
    {
        StopAllCoroutines();
        StartCoroutine(FadeCanvasGroup(menuCanvas, 1, 0, fadeTime));
        EnableUI(false);
    }

    private void EnableUI(bool enable)
    {
        if (menuCanvas != null)
        {
            menuCanvas.interactable = enable;
            menuCanvas.blocksRaycasts = enable;
        }
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float start, float end, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(start, end, elapsed / duration);
            yield return null;
        }
        cg.alpha = end;
    }
}
