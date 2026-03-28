using UnityEngine;
using UnityEngine.InputSystem;

public class AbilityManager : MonoBehaviour
{
    [SerializeField] private Ability[] abilities;
    [SerializeField] private int activeIndex;
    [SerializeField] private bool abilityInputEnabled = true;

    private Ability active => abilities[activeIndex];

    public enum AbilityType
    {
        Fold = 0,
        Snapshot = 1
    }

    // Check ability every frame
    private void Update()
    {
        //if (Keyboard.current.anyKey.wasPressedThisFrame)
        //{
        //    Debug.Log("Key detected");
        //}

        if (Keyboard.current.aKey.wasPressedThisFrame)
        {

            switchTo((int)AbilityType.Fold);
            Debug.Log("Switched to Fold");
        }
        else if (Keyboard.current.bKey.wasPressedThisFrame)
        {
            switchTo((int)AbilityType.Snapshot);
            Debug.Log("Switched to Snapshot");
        }


        if (!abilityInputEnabled) return;
        if (abilities == null || abilities.Length == 0) return;

        if (abilityInputEnabled)
            active.onUpdate();
    }
    private void switchTo(int index)
    {
        active.onAbilitySwitch();
        activeIndex = (int)index;
    }

}
