using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class GlowController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer glowRenderer; // child object with same sprite, unlit material
    [SerializeField] private Color glowColor = Color.cyan;
    [SerializeField] private float glowIntensity = 2.0f;
    [SerializeField] private float fadeInDuration = 0.2f;
    [SerializeField] private float holdDuration = 0.3f;
    [SerializeField] private float fadeOutDuration = 0.4f;
    private bool isGlowActive = false;

    void Start()
    {
        if (glowRenderer != null)
            glowRenderer.enabled = false;
    }

    public void EnableGlow()
    {
        if (glowRenderer == null || isGlowActive) return;
        glowRenderer.color = glowColor * glowIntensity; // HDR value will trigger bloom
        glowRenderer.enabled = true;
        isGlowActive = true;

        StartCoroutine(GlowPulse());
    }

    private IEnumerator GlowPulse()
    {
        Color targetColor = glowColor * glowIntensity;
        float t = 0f;
        // Fade In
        while(t < 1f)
        {
            t += Time.deltaTime / fadeInDuration;
            glowRenderer.color = Color.Lerp(Color.clear, targetColor, t);
            yield return null;
        }
        // Hold
        yield return new WaitForSeconds(holdDuration);

        // Fade Out
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / fadeInDuration;
            glowRenderer.color = Color.Lerp(targetColor, Color.clear, t);
            yield return null;
        }

        DisableGlow();


    }

    public void DisableGlow()
    {
        Debug.Log("Disabling glow");
        if (glowRenderer == null || !isGlowActive) return;
        StopAllCoroutines();
        glowRenderer.enabled = false;
        isGlowActive = false;
    }
}