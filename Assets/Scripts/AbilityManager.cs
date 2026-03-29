using UnityEngine;
using UnityEngine.InputSystem;

public class AbilityManager : MonoBehaviour
{
    [SerializeField] private Ability[] abilities;
    [SerializeField] private int activeIndex;
    [SerializeField] private bool abilityInputEnabled = true;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private string iconName;

    private Ability active => IsValidAbilityIndex(activeIndex) ? abilities[activeIndex] : null;
	private AudioSource audioSource;

    public enum AbilityType
    {
        Fold = 0,
        Snapshot = 1,
        Gravity = 2
    }
    private string[] IconNames =
    {
        "FoldAbilityIcon",
        "SnapshotAbilityIcon",
        "GravityAbilityIcon"
    };

    private void Awake()
    {
		audioSource = GetComponent<AudioSource>();
        if (worldCamera == null)
        {
            worldCamera = Camera.main;
        }

        if (abilities == null || abilities.Length == 0)
        {
            return;
        }

        activeIndex = Mathf.Clamp(activeIndex, 0, abilities.Length - 1);

        for (int i = 0; i < abilities.Length; i++)
        {
            if (abilities[i] != null)
            {
                abilities[i].Initialize(worldCamera);
            }
        }
    }

    // Check ability every frame
    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        if (keyboard.digit1Key.wasPressedThisFrame)
        {
            switchTo((int)AbilityType.Fold);
            Debug.Log("Switched to Fold");
        }
        else if (keyboard.digit2Key.wasPressedThisFrame)
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
        else if (keyboard.digit3Key.wasPressedThisFrame)
        {
			if (!audioSource.isPlaying)
			{
				audioSource.Play();
			}
            switchTo((int)AbilityType.Gravity);

            Debug.Log("Switched to Gravity");
        }


        if (!abilityInputEnabled) return;
        if (abilities == null || abilities.Length == 0) return;
        if (abilityInputEnabled && active != null)
            active.onUpdate();
    }
    private void switchTo(int index)
    {
        if (!IsValidAbilityIndex(index))
        {
            return;
        }

        if (active != null)
        {
            iconName = IconNames[index];
            active.onAbilitySwitch();
            GameObject go = GameObject.Find(iconName);
            if (go != null)
            {
                GlowController controller = go.GetComponent<GlowController>();
                controller.EnableGlow();
            }

            activeIndex = (int)index;
        }
    }

    private bool IsValidAbilityIndex(int index)
    {
        return abilities != null && index >= 0 && index < abilities.Length && abilities[index] != null;
    }

}
