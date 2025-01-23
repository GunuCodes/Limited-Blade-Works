using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HeartController : MonoBehaviour
{
    private GameObject[] heartContainers;
    private Image[] heartFills;
    public Transform heartsParent;
    public GameObject heartContainerPrefab;
    private Coroutine[] heartFillCoroutines;
    private bool[] isHeartRegenerating;
    // Start is called before the first frame update
    void Start()
    {
        heartContainers = new GameObject[PlayerController.Instance.maxHealth];
        heartFills = new Image[PlayerController.Instance.maxHealth];

        PlayerController.Instance.onHealthChangedCallback += UpdateHeartsHUD;
        InstantiateHeartContainers();
        UpdateHeartsHUD();

        heartFillCoroutines = new Coroutine[PlayerController.Instance.maxHealth];
        isHeartRegenerating = new bool[PlayerController.Instance.maxHealth];
    }

    // Update is called once per frame
    void Update()
    {
        // Check for regenerating heart
        int regeneratingIndex = PlayerController.Instance.regeneratingHeartIndex;
        if (regeneratingIndex >= 0 && regeneratingIndex < heartFills.Length)
        {
            // Update the fill amount of the regenerating heart
            heartFills[regeneratingIndex].fillAmount = PlayerController.Instance.healProgress;
        }
        else if (regeneratingIndex == -1)
        {
            // If regeneration was interrupted, ensure any partially filled hearts are emptied
            for (int i = PlayerController.Instance.health; i < heartFills.Length; i++)
            {
                if (heartFills[i] != null)
                {
                    heartFills[i].fillAmount = 0;
                }
            }
        }
    }
    void SetHeartContainers()
    {
        for (int i = 0; i < heartContainers.Length; i++)
        {
            if (i < PlayerController.Instance.maxHealth)
            {
                heartContainers[i].SetActive(true);
            }
            else
            {
                heartContainers[i].SetActive(false);
            }
        }
    }
    void SetFilledHearts()
    {
        if (PlayerController.Instance == null) return;
        
        for (int i = 0; i < heartFills.Length; i++)
        {
            if (heartFills[i] != null)
            {
                // If this heart is currently regenerating, skip it
                if (i == PlayerController.Instance.regeneratingHeartIndex)
                    continue;
                    
                // Otherwise, set it to either full or empty
                heartFills[i].fillAmount = (i < PlayerController.Instance.health) ? 1 : 0;
            }
        }
    }
    void InstantiateHeartContainers()
    {
        for (int i = 0; i < PlayerController.Instance.maxHealth; i++)
        {
            GameObject temp = Instantiate(heartContainerPrefab);
            temp.transform.SetParent(heartsParent, false);
            heartContainers[i] = temp;
            heartFills[i] = temp.transform.Find("HeartFill").GetComponent<Image>();
        }
    }
    void UpdateHeartsHUD()
    {
        SetHeartContainers();
        SetFilledHearts();
    }
}