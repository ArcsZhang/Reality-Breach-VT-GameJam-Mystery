using UnityEngine;

public static class ColorCollisionResolver
{
    public static void ResolveEnemyTouch(EnemyBehavior self, GameObject target)
    {
        if (self == null || target == null)
        {
            return;
        }

        EnemyBehavior touchedEnemy = target.GetComponentInParent<EnemyBehavior>();
        if (touchedEnemy != null && touchedEnemy != self)
        {
            bool selfIsRed = self.GetColor() == ColorManager.ColorState.Red;
            bool targetIsRed = touchedEnemy.GetColor() == ColorManager.ColorState.Red;

            if (selfIsRed && targetIsRed)
            {
                touchedEnemy.Die();
                self.Die();
                return;
            }
        }

        if (TryGetTouchedColor(target, out ColorManager.ColorState touchedColor) && touchedColor == ColorManager.ColorState.Red)
        {
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
