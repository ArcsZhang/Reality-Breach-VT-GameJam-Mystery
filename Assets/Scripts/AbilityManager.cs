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
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {

            switchTo((int)AbilityType.Fold);
            Debug.Log("Switched to Fold");
        }
        else if (Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            if (activeIndex == (int)AbilityType.Snapshot &&
                abilities.Length > (int)AbilityType.Snapshot &&
                abilities[(int)AbilityType.Snapshot] is SnapshotAbility snapshotAbility &&
                snapshotAbility.TryPasteSnapshotAtCursor())
            {
                // Snapshot already active: paste held capture at cursor (does not re-run ability switch).
            }
            else
            {
                switchTo((int)AbilityType.Snapshot);
                Debug.Log("Switched to Snapshot");
            }
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
