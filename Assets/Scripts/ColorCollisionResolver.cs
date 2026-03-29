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
                ResolveRedEnemyTouch(self, target);
                break;
            case ColorManager.ColorState.Orange:
                ResolveOrangeEnemyTouch(self, target);
                break;
        }
    }

    private static void ResolveRedEnemyTouch(EnemyBehavior self, GameObject target)
    {
        EnemyBehavior touchedEnemy = target.GetComponentInParent<EnemyBehavior>();
        if (touchedEnemy != null && touchedEnemy != self)
        {
            bool targetIsRed = touchedEnemy.GetColor() == ColorManager.ColorState.Red;
            touchedEnemy.Die();
            if (targetIsRed)
            {
                self.Die();
            }

            return;
        }

        ObjectBehavior touchedObject = target.GetComponentInParent<ObjectBehavior>();
        if (touchedObject != null && touchedObject.GetColor() == ColorManager.ColorState.Green)
        {
            touchedObject.Die();
        }
    }

    private static void ResolveOrangeEnemyTouch(EnemyBehavior self, GameObject target)
    {
        TerrainBehavior touchedTerrain = target.GetComponentInParent<TerrainBehavior>();
        if (touchedTerrain != null)
        {
            if (touchedTerrain.GetColor() == ColorManager.ColorState.Orange)
            {
                return;
            }

            touchedTerrain.Destroy();
            self.Die();
            return;
        }

        EnemyBehavior touchedEnemy = target.GetComponentInParent<EnemyBehavior>();
        if (touchedEnemy != null && touchedEnemy != self)
        {
            touchedEnemy.Die();
            self.Die();
            return;
        }

        ObjectBehavior touchedObject = target.GetComponentInParent<ObjectBehavior>();
        if (touchedObject != null)
        {
            touchedObject.Die();
            self.Die();
        }
    }

    public static void ResolveObjectTouch(ObjectBehavior self, GameObject target)
    {
        if (self == null || target == null)
        {
            return;
        }

        if (self.GetColor() != ColorManager.ColorState.Green)
        {
            return;
        }

        if (TryGetTouchedColor(target, out ColorManager.ColorState touchedColor) && touchedColor == ColorManager.ColorState.Red)
        {
            self.Die();
        }
    }

    private static bool TryGetTouchedColor(GameObject target, out ColorManager.ColorState color)
    {
        ColorManager colorManager = target.GetComponentInParent<ColorManager>();
        if (colorManager != null)
        {
            color = colorManager.GetColor();
            return true;
        }

        color = ColorManager.ColorState.White;
        return false;
    }
}
