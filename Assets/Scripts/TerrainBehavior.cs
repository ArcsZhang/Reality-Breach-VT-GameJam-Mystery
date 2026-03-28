using UnityEngine;

[RequireComponent(typeof(FoldableObject2D))]
public class TerrainBehavior : ColorManager
{
    private FoldableObject2D foldableObject;

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
}
