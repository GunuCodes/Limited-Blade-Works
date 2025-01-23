using UnityEngine;

public class DestroyFX : MonoBehaviour
{
    [SerializeField] private float additionalDelay = 0f; // Optional delay after animation

    void Start()
    {
        float animationLength = GetComponent<Animator>()?.GetCurrentAnimatorStateInfo(0).length ?? 0f;
        Destroy(gameObject, animationLength + additionalDelay);
    }
}
