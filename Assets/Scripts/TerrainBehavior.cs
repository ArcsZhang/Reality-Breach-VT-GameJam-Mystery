using UnityEngine;

[RequireComponent(typeof(FoldableObject2D))]
public class TerrainBehavior : ColorManager
{
    private FoldableObject2D foldableObject;
	private bool hasDestroyed = false;

    protected override void Awake()
    {
        base.Awake();
        foldableObject = GetComponent<FoldableObject2D>();
    }

    protected override void OnRedChanged(ColorState previous)
    {
    }

    protected override void OnOrangeChanged(ColorState previous)
    {
    }

    protected override void OnYellowChanged(ColorState previous)
    {
		// change its layer to FalseWall
		gameObject.layer = LayerMask.NameToLayer("FalseWall");
    }

    protected override void OnGreenChanged(ColorState previous)
    {
    }

    protected override void OnBlueChanged(ColorState previous)
    {
    }

    protected override void OnPurpleChanged(ColorState previous)
    {
    }

    protected override void OnWhiteChanged(ColorState previous)
    {
    }

	protected override void OnColorChanged(ColorState previousColor, ColorState newColor)
	{
		if (newColor != ColorState.Yellow)
		{
			// change its layer back to Default
			gameObject.layer = LayerMask.NameToLayer("Wall");
		}
	}

    protected override void OnRedUpdate()
    {
    }

    protected override void OnOrangeUpdate()
    {
    }

    protected override void OnYellowUpdate()
    {
    }

    protected override void OnGreenUpdate()
    {
    }

    protected override void OnBlueUpdate()
    {
    }

    protected override void OnPurpleUpdate()
    {
    }

    protected override void OnWhiteUpdate()
    {
    }

	public void Destroy()
	{
		if (hasDestroyed)
		{
			return;
		}
		hasDestroyed = true;
		gameObject.SetActive(false);
	}

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision == null)
        {
            return;
        }

        ColorCollisionResolver.ResolveTerrainTouch(this, collision.gameObject);
    }
}
