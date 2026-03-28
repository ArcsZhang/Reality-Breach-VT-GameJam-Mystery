using UnityEngine;

public class ColorManager : MonoBehaviour
{
    public enum VisualColorMode
    {
        Auto,
        SpriteRenderer,
        MeshRenderer
    }

    public enum ColorState
    {
        Red,
        Orange,
        Yellow,
        Green,
        Blue,
        Purple,
        White
    }

    [Header("Color State")]
    [SerializeField] private ColorState originalColor = ColorState.White;
    [SerializeField] private bool applyVisualColorToRenderer = true;

    [Header("Visual Color")]
    [SerializeField] private VisualColorMode visualColorMode = VisualColorMode.Auto;
    [SerializeField] private SpriteRenderer targetSpriteRenderer;
    [SerializeField] private MeshRenderer targetMeshRenderer;

    private ColorState currentColor = ColorState.White;
    private bool isInitialized;

    protected virtual void Awake()
    {
        EnsureInitialized();
    }

    protected virtual void Update()
    {
        EnsureInitialized();
        DispatchPerColorUpdate(currentColor);
        OnColorUpdate(currentColor);
    }

    public ColorState GetColor()
    {
        EnsureInitialized();
        return currentColor;
    }

    public void SetColor(ColorState newColor)
    {
        EnsureInitialized();

        if (currentColor == newColor)
        {
            return;
        }

        ColorState previousColor = currentColor;
        currentColor = newColor;

        if (applyVisualColorToRenderer)
        {
            ApplyRendererColor(currentColor);
        }

        DispatchPerColorChanged(previousColor, currentColor);
        OnColorChanged(previousColor, currentColor);
    }

    public ColorState GetOriginalColor()
    {
        return originalColor;
    }

    public void SetOriginalColor(ColorState newOriginalColor, bool alsoApplyCurrent = false)
    {
        originalColor = newOriginalColor;

        if (alsoApplyCurrent)
        {
            SetColor(newOriginalColor);
        }
    }

    public void ResetToOriginalColor()
    {
        SetColor(originalColor);
    }

    protected virtual void OnRedUpdate() { }
    protected virtual void OnOrangeUpdate() { }
    protected virtual void OnYellowUpdate() { }
    protected virtual void OnGreenUpdate() { }
    protected virtual void OnBlueUpdate() { }
    protected virtual void OnPurpleUpdate() { }
    protected virtual void OnWhiteUpdate() { }

    protected virtual void OnRedChanged(ColorState previous) { }
    protected virtual void OnOrangeChanged(ColorState previous) { }
    protected virtual void OnYellowChanged(ColorState previous) { }
    protected virtual void OnGreenChanged(ColorState previous) { }
    protected virtual void OnBlueChanged(ColorState previous) { }
    protected virtual void OnPurpleChanged(ColorState previous) { }
    protected virtual void OnWhiteChanged(ColorState previous) { }

    protected virtual void OnColorChanged(ColorState previousColor, ColorState newColor) { }
    protected virtual void OnColorUpdate(ColorState state) { }

    private void EnsureInitialized()
    {
        if (isInitialized)
        {
            return;
        }

        ResolveVisualTargets();

        currentColor = originalColor;
        if (applyVisualColorToRenderer)
        {
            ApplyRendererColor(currentColor);
        }

        isInitialized = true;
    }

    private void DispatchPerColorUpdate(ColorState state)
    {
        switch (state)
        {
            case ColorState.Red:
                OnRedUpdate();
                break;
            case ColorState.Orange:
                OnOrangeUpdate();
                break;
            case ColorState.Yellow:
                OnYellowUpdate();
                break;
            case ColorState.Green:
                OnGreenUpdate();
                break;
            case ColorState.Blue:
                OnBlueUpdate();
                break;
            case ColorState.Purple:
                OnPurpleUpdate();
                break;
            case ColorState.White:
                OnWhiteUpdate();
                break;
        }
    }

    private void DispatchPerColorChanged(ColorState previousColor, ColorState newColor)
    {
        switch (newColor)
        {
            case ColorState.Red:
                OnRedChanged(previousColor);
                break;
            case ColorState.Orange:
                OnOrangeChanged(previousColor);
                break;
            case ColorState.Yellow:
                OnYellowChanged(previousColor);
                break;
            case ColorState.Green:
                OnGreenChanged(previousColor);
                break;
            case ColorState.Blue:
                OnBlueChanged(previousColor);
                break;
            case ColorState.Purple:
                OnPurpleChanged(previousColor);
                break;
            case ColorState.White:
                OnWhiteChanged(previousColor);
                break;
        }
    }

    private void ApplyRendererColor(ColorState state)
    {
        Color unityColor = ToUnityColor(state);

        if (visualColorMode == VisualColorMode.SpriteRenderer)
        {
            if (targetSpriteRenderer != null)
            {
                targetSpriteRenderer.color = unityColor;
            }

            return;
        }

        if (visualColorMode == VisualColorMode.MeshRenderer)
        {
            if (targetMeshRenderer != null)
            {
                targetMeshRenderer.material.color = unityColor;
            }

            return;
        }

        if (targetSpriteRenderer != null)
        {
            targetSpriteRenderer.color = unityColor;
            return;
        }

        if (targetMeshRenderer != null)
        {
            targetMeshRenderer.material.color = unityColor;
        }
    }

    private void ResolveVisualTargets()
    {
        if (targetSpriteRenderer == null)
        {
            targetSpriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (targetMeshRenderer == null)
        {
            targetMeshRenderer = GetComponent<MeshRenderer>();
        }

        if (visualColorMode == VisualColorMode.SpriteRenderer)
        {
            return;
        }

        if (visualColorMode == VisualColorMode.MeshRenderer)
        {
            return;
        }

        if (targetSpriteRenderer == null && targetMeshRenderer == null)
        {
            targetMeshRenderer = GetComponent<MeshRenderer>();
        }
    }

    private static Color ToUnityColor(ColorState state)
    {
        switch (state)
        {
            case ColorState.Red:
                return Color.red;
            case ColorState.Orange:
                return new Color(1f, 0.5f, 0f);
            case ColorState.Yellow:
                return Color.yellow;
            case ColorState.Green:
                return Color.green;
            case ColorState.Blue:
                return Color.blue;
            case ColorState.Purple:
                return new Color(0.5f, 0f, 1f);
            case ColorState.White:
            default:
                return Color.white;
        }
    }
}
