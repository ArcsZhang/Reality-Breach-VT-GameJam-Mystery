using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Portal : MonoBehaviour
{
    [Header("Level Progress")]
    [SerializeField] public int enemiesLeft = 0;
    [SerializeField] public string nextLevelScene = "";
	[SerializeField] private bool detectEnemiesOnStart = true;

    [Header("Activation Visuals")]
    [SerializeField] private float growDurationSeconds = 1f;
    [SerializeField] private float activeScaleMultiplier = 1.6f;
    [SerializeField] private float baseRotationSpeedDegrees = 45f;
    [SerializeField] private float grownRotationSpeedDegrees = 240f;
    [SerializeField] private float rainbowHueCyclesPerSecond = 0.5f;
    [SerializeField] private float rainbowSaturation = 1f;
    [SerializeField] private float rainbowValue = 1f;

    [Header("Player Transition")]
    [SerializeField] private float suckDurationSeconds = 2f;
    [SerializeField] private float finalPlayerScaleMultiplier = 0.05f;

    private bool isActive = false;
    private bool isLoadingLevel = false;
    private Vector3 baseScale;
    private float activationTime = -1f;
    private Renderer[] renderers;
    private SpriteRenderer[] spriteRenderers;
	private AudioSource audioSource;

    private void Awake()
    {
        baseScale = transform.localScale;
        renderers = GetComponentsInChildren<Renderer>(true);
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
    }

    private void Start()
    {
		audioSource = GetComponent<AudioSource>();
        if (detectEnemiesOnStart)
        {
            RefreshEnemyCountFromScene();
        }

        if (enemiesLeft <= 0)
        {
            ActivatePortal();
        }
    }

    public void RefreshEnemyCountFromScene()
    {
        EnemyBehavior[] enemies = FindObjectsByType<EnemyBehavior>();
        enemiesLeft = enemies != null ? enemies.Length : 0;
    }

    private void Update()
    {
        float growthT = 0f;

        if (!isActive)
        {
            transform.Rotate(0f, 0f, baseRotationSpeedDegrees * Time.deltaTime);
            return;
        }

        float elapsedSinceActivation = Mathf.Max(0f, Time.time - activationTime);
        growthT = growDurationSeconds <= 0f ? 1f : Mathf.Clamp01(elapsedSinceActivation / growDurationSeconds);
        float currentScaleMultiplier = Mathf.Lerp(1f, activeScaleMultiplier, growthT);
        transform.localScale = baseScale * currentScaleMultiplier;

        float rotationSpeed = Mathf.Lerp(baseRotationSpeedDegrees, grownRotationSpeedDegrees, growthT);
        transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);

        float hue = Mathf.Repeat(Time.time * rainbowHueCyclesPerSecond, 1f);
        Color rainbow = Color.HSVToRGB(hue, Mathf.Clamp01(rainbowSaturation), Mathf.Clamp01(rainbowValue));
        ApplyColor(rainbow);
    }

    public void IncrementEnemiesLeft()
    {
        enemiesLeft++;
    }

    public void DecrementEnemiesLeft()
    {
        enemiesLeft = Mathf.Max(0, enemiesLeft - 1);
        if (enemiesLeft == 0)
        {
            ActivatePortal();
        }
    }

    private void ActivatePortal()
    {
        if (isActive)
        {
            return;
        }
		if (audioSource != null && !audioSource.isPlaying)
		{
			audioSource.Play();
		}

        isActive = true;
        activationTime = Time.time;
    }

    private void ApplyColor(Color color)
    {
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null)
            {
                spriteRenderers[i].color = color;
            }
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null)
            {
                continue;
            }

            Material material = renderers[i].material;
            if (material != null && material.HasProperty("_Color"))
            {
                material.color = color;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        TryProceedToNextLevel(collision);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision == null)
        {
            return;
        }

        TryProceedToNextLevel(collision.collider);
    }

    private void TryProceedToNextLevel(Collider2D collider)
    {
        if (!isActive || isLoadingLevel || collider == null)
        {
            return;
        }

        ObjectBehavior objectBehavior = collider.GetComponentInParent<ObjectBehavior>();
        if (objectBehavior == null || !objectBehavior.isPlayer)
        {
            return;
        }

        isLoadingLevel = true;
        objectBehavior.SetPortalTransitionActive(true);
        StartCoroutine(SuckPlayerAndProceed(objectBehavior));
    }

    private IEnumerator SuckPlayerAndProceed(ObjectBehavior player)
    {
        if (player == null)
        {
            isLoadingLevel = false;
            yield break;
        }

        Transform playerTransform = player.transform;
        Vector3 initialPosition = playerTransform.position;
        Vector3 centerPosition = transform.position;
        Vector3 initialScale = playerTransform.localScale;
        float clampedScaleMultiplier = Mathf.Max(0f, finalPlayerScaleMultiplier);
        Vector3 targetScale = initialScale * clampedScaleMultiplier;
        float duration = Mathf.Max(0.01f, suckDurationSeconds);

        Rigidbody2D body2D = player.GetComponent<Rigidbody2D>();
        bool hadBody = body2D != null;
        bool originalSimulated = false;
        if (hadBody)
        {
            originalSimulated = body2D.simulated;
            body2D.linearVelocity = Vector2.zero;
            body2D.angularVelocity = 0f;
            body2D.simulated = false;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - (1f - t) * (1f - t);

            playerTransform.position = Vector3.Lerp(initialPosition, centerPosition, eased);
            playerTransform.localScale = Vector3.Lerp(initialScale, targetScale, t);

            yield return null;
        }

        playerTransform.position = centerPosition;
        playerTransform.localScale = targetScale;

        bool loaded = LoadNextLevel();
        if (!loaded)
        {
            if (hadBody)
            {
                body2D.simulated = originalSimulated;
            }

            playerTransform.localScale = initialScale;
            player.SetPortalTransitionActive(false);
            isLoadingLevel = false;
        }
    }

    private bool LoadNextLevel()
    {
        if (!string.IsNullOrWhiteSpace(nextLevelScene))
        {
            SceneManager.LoadScene(nextLevelScene);
            return true;
        }

        Debug.LogWarning("Portal is active but no next level is configured.", this);
        return false;
    }
}
