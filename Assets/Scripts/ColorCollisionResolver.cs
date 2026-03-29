using UnityEngine;

public static class ColorCollisionResolver
{
    public static void ResolveEnemyTouch(EnemyBehavior self, GameObject target)
    {
        if (self == null || target == null)
        {
            return;
        }

        ColorManager.ColorState selfColor = self.GetColor();
        switch (selfColor)
        {
            case ColorManager.ColorState.Red:
                ResolveRedTouch(self, target);
                break;
            case ColorManager.ColorState.Orange:
                ResolveOrangeTouch(self, target);
                break;
            case ColorManager.ColorState.Blue:
                ResolveBlueFragileTouch(self, target);
                break;
        }
    }

    public static void ResolveObjectTouch(ObjectBehavior self, GameObject target)
    {
        if (self == null || target == null)
        {
            return;
        }

        switch (self.GetColor())
        {
            case ColorManager.ColorState.Red:
                ResolveRedTouch(self, target);
                break;
            case ColorManager.ColorState.Orange:
                ResolveOrangeTouch(self, target);
                break;
            case ColorManager.ColorState.Blue:
                ResolveBlueFragileTouch(self, target);
                break;
            case ColorManager.ColorState.Green:
                if (TryGetTouchedColor(target, out ColorManager.ColorState touchedColor) && touchedColor == ColorManager.ColorState.Red)
                {
                    self.Die();
                }

                break;
        }
    }

    public static void ResolveTerrainTouch(TerrainBehavior self, GameObject target)
    {
        if (self == null || target == null)
        {
            return;
        }

        switch (self.GetColor())
        {
            case ColorManager.ColorState.Red:
                ResolveRedTouch(self, target);
                break;
            case ColorManager.ColorState.Orange:
                break;
            case ColorManager.ColorState.Blue:
                ResolveBlueFragileTouch(self, target);
                break;
        }
    }

    private static void ResolveRedTouch(ColorManager self, GameObject target)
    {
        EnemyBehavior touchedEnemy = target.GetComponentInParent<EnemyBehavior>();
        if (touchedEnemy != null && touchedEnemy != self)
        {
            touchedEnemy.Die();
            return;
        }

        ObjectBehavior touchedObject = target.GetComponentInParent<ObjectBehavior>();
        if (touchedObject != null && touchedObject != self && touchedObject.GetColor() == ColorManager.ColorState.Green)
        {
            touchedObject.Die();
            return;
        }

        TerrainBehavior touchedTerrain = target.GetComponentInParent<TerrainBehavior>();
        if (touchedTerrain != null && touchedTerrain != self && touchedTerrain.GetColor() == ColorManager.ColorState.Green)
        {
            touchedTerrain.Destroy();
        }
    }

    private static void ResolveOrangeTouch(ColorManager self, GameObject target)
    {
        if (!TryGetTouchedColorManager(target, out ColorManager touchedColorManager))
        {
            return;
        }

        if (touchedColorManager == self)
        {
            return;
        }

        if (touchedColorManager.GetColor() == ColorManager.ColorState.Orange)
        {
            return;
        }

        KillColorManagerTarget(touchedColorManager);
        KillColorManagerTarget(self);
    }

    private static void ResolveBlueFragileTouch(ColorManager self, GameObject target)
    {
        EnemyBehavior touchedEnemy = target.GetComponentInParent<EnemyBehavior>();
        if (touchedEnemy != null && touchedEnemy != self)
        {
            if (touchedEnemy.GetColor() == ColorManager.ColorState.Blue)
            {
                touchedEnemy.Die();
            }

            KillColorManagerTarget(self);
            return;
        }

        ObjectBehavior touchedObject = target.GetComponentInParent<ObjectBehavior>();
        if (touchedObject != null && touchedObject != self)
        {
            if (touchedObject.GetColor() == ColorManager.ColorState.Blue)
            {
                touchedObject.Die();
            }

            KillColorManagerTarget(self);
            return;
        }

        TerrainBehavior touchedTerrain = target.GetComponentInParent<TerrainBehavior>();
        if (touchedTerrain != null && touchedTerrain != self && touchedTerrain.GetColor() == ColorManager.ColorState.Red)
        {
            KillColorManagerTarget(self);
        }
    }

    private static void KillColorManagerTarget(ColorManager target)
    {
        if (target == null)
        {
            return;
        }

        EnemyBehavior enemy = target as EnemyBehavior;
        if (enemy != null)
        {
            enemy.Die();
            return;
        }

        ObjectBehavior obj = target as ObjectBehavior;
        if (obj != null)
        {
            obj.Die();
            return;
        }

        TerrainBehavior terrain = target as TerrainBehavior;
        if (terrain != null)
        {
            terrain.Destroy();
            return;
        }
    }

    private static bool TryGetTouchedColor(GameObject target, out ColorManager.ColorState color)
    {
        if (TryGetTouchedColorManager(target, out ColorManager colorManager))
        {
            color = colorManager.GetColor();
            return true;
        }

        color = ColorManager.ColorState.White;
        return false;
    }

    private static bool TryGetTouchedColorManager(GameObject target, out ColorManager colorManager)
    {
        colorManager = target != null ? target.GetComponentInParent<ColorManager>() : null;
        return colorManager != null;
    }
}
