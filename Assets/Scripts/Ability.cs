using UnityEngine;

public abstract class Ability : MonoBehaviour
{
    protected Camera WorldCamera;

    public virtual void Initialize(Camera cam)
    {
        WorldCamera = cam;
    }

    public abstract void onUpdate();
    public abstract void onClear();
    public abstract void onAbilitySwitch();
}
