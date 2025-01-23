using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitEffect : MonoBehaviour
{
    [Header("Damage Flash Settings")]
    [SerializeField] private Color damageFlashColor = new Color(1f, 0f, 0f, 0.5f);
    [SerializeField] private float damageFlashDuration = 0.15f;
    [SerializeField] private int damageFlashCount = 1;

    [Header("Invincibility Flash Settings")]
    [SerializeField] private bool useInvincibilityFlash = true;
    [SerializeField] private Color invincibilityFlashColor = new Color(1f, 1f, 1f, 0.5f);
    [SerializeField] private float invincibilityFlashDuration = 0.1f;
    [SerializeField] private int invincibilityFlashCount = 5;

    [Header("Blood Effect Settings")]
    [SerializeField] private bool useBloodEffect = true;
    [SerializeField] private GameObject bloodSplatterPrefab;
    [SerializeField] private int minSplatters = 1;
    [SerializeField] private int maxSplatters = 3;

    [Header("Blood Randomization")]
    [SerializeField] private bool randomizeRotation = true;
    [SerializeField] private bool randomizeScale = true;
    [SerializeField] private float minScale = 0.8f;
    [SerializeField] private float maxScale = 1.2f;
    [SerializeField] private float rotationRange = 360f;
    [SerializeField] private Vector2 offsetRange = new Vector2(0.5f, 0.5f);

    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Coroutine flashRoutine;
    private Coroutine invincibilityFlashRoutine;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color;
    }

    public void OnHit()
    {
        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
        }
        flashRoutine = StartCoroutine(FlashRoutine(damageFlashColor, damageFlashDuration, damageFlashCount));

        if (useBloodEffect && bloodSplatterPrefab != null)
        {
            SpawnBloodEffects();
        }
    }

    public void StartInvincibilityFlash()
    {
        if (useInvincibilityFlash)
        {
            if (invincibilityFlashRoutine != null)
            {
                StopCoroutine(invincibilityFlashRoutine);
            }
            invincibilityFlashRoutine = StartCoroutine(FlashRoutine(invincibilityFlashColor, invincibilityFlashDuration, invincibilityFlashCount));
        }
    }

    private IEnumerator FlashRoutine(Color flashColor, float flashDuration, int count)
    {
        for (int i = 0; i < count; i++)
        {
            float elapsedTime = 0f;
            
            // Fade in
            while (elapsedTime < flashDuration * 0.5f)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / (flashDuration * 0.5f);
                Color blendedColor = Color.Lerp(originalColor, flashColor, t);
                spriteRenderer.color = blendedColor;
                yield return null;
            }
            
            // Fade out
            while (elapsedTime < flashDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = (elapsedTime - flashDuration * 0.5f) / (flashDuration * 0.5f);
                Color blendedColor = Color.Lerp(flashColor, originalColor, t);
                spriteRenderer.color = blendedColor;
                yield return null;
            }
            
            spriteRenderer.color = originalColor;
            
            if (i < count - 1)
            {
                yield return new WaitForSeconds(flashDuration * 0.5f);
            }
        }
    }

    private void SpawnBloodEffects()
    {
        int splatCount = UnityEngine.Random.Range(minSplatters, maxSplatters + 1);
        
        for (int i = 0; i < splatCount; i++)
        {
            // Random position offset
            Vector2 randomOffset = new Vector2(
                UnityEngine.Random.Range(-offsetRange.x, offsetRange.x),
                UnityEngine.Random.Range(-offsetRange.y, offsetRange.y)
            );

            Vector3 spawnPosition = transform.position + new Vector3(randomOffset.x, randomOffset.y, 0);
            
            // Create blood effect
            GameObject bloodEffect = Instantiate(bloodSplatterPrefab, spawnPosition, Quaternion.identity);
            
            // Random rotation
            if (randomizeRotation)
            {
                float randomRotation = UnityEngine.Random.Range(0f, rotationRange);
                bloodEffect.transform.rotation = Quaternion.Euler(0, 0, randomRotation);
            }
            
            // Random scale
            if (randomizeScale)
            {
                float randomScale = UnityEngine.Random.Range(minScale, maxScale);
                bloodEffect.transform.localScale = new Vector3(randomScale, randomScale, 1f);
            }

            // Optional: Destroy the blood effect after animation
            float destroyDelay = 2f; // Adjust based on your animation length
            Destroy(bloodEffect, destroyDelay);
        }
    }
}