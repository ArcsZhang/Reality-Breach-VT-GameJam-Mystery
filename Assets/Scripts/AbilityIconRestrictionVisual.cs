using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Dims this icon when <see cref="AbilityManager.IsAbilityAllowed"/> is false for the configured ability.
/// Add to each ability icon (Fold / Snapshot / Gravity); assign <see cref="abilityType"/> and optionally the render targets.
/// </summary>
public class AbilityIconRestrictionVisual : MonoBehaviour
{
    [SerializeField] private AbilityManager.AbilityType abilityType;

    [Tooltip("If unset, uses SpriteRenderer on this object.")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Tooltip("If unset, uses Graphic (e.g. Image) on this object.")]
    [SerializeField] private Graphic uiGraphic;

    [Tooltip("Optional: dims the whole slot (children included) when locked.")]
    [SerializeField] private CanvasGroup canvasGroup;

    [SerializeField] private Color enabledTint = Color.white;
    [SerializeField] private Color disabledTint = new Color(0.38f, 0.38f, 0.38f, 0.85f);

    [SerializeField] private float enabledCanvasAlpha = 1f;
    [SerializeField] private float disabledCanvasAlpha = 0.45f;

    private void Start()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (uiGraphic == null)
        {
            uiGraphic = GetComponent<Graphic>();
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        AbilityManager manager = FindAnyObjectByType<AbilityManager>();
        bool allowed = manager == null || manager.IsAbilityAllowed((int)abilityType);

        Apply(allowed);
    }

    private void Apply(bool allowed)
    {
        Color c = allowed ? enabledTint : disabledTint;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = c;
        }

        if (uiGraphic != null)
        {
            uiGraphic.color = c;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = allowed ? enabledCanvasAlpha : disabledCanvasAlpha;
            canvasGroup.interactable = allowed;
        }
    }
}
