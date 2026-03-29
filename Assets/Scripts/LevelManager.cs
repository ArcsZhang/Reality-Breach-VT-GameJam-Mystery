using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
	public static LevelManager Instance { get; private set; }

	[Header("Level Info")]
	[SerializeField] public string thisLevelName;
	[SerializeField] public string nextLevelName;
	[SerializeField] public Portal portal;
	[SerializeField] public ObjectBehavior player;

	private PlayerInput playerInput;
	private InputAction restartAction;
	private bool isRestarting;
	[Header("Abilities per level")]
	[Tooltip("Uncheck to lock an ability for this scene. When no LevelManager is present, AbilityManager allows all abilities.")]
	[SerializeField] private bool allowFoldAbility = true;
	[SerializeField] private bool allowSnapshotAbility = true;
	[SerializeField] private bool allowGravityAbility = true;

	private void Awake()
	{
		if (Instance != null && Instance != this)
        {
            Destroy(Instance.gameObject);
        }

        Instance = this;
		ResolveSceneReferences();
	}

	private void Start()
    {
		BindRestartAction();
		ResolveSceneReferences();

		if (portal != null)
		{
			portal.nextLevelScene = nextLevelName;
		}
	}

	private void OnEnable()
	{
		BindRestartAction();
	}

	private void OnDisable()
	{
		restartAction?.Disable();
	}

	private void Update()
	{
		if (isRestarting)
		{
			return;
		}

		if (restartAction != null && restartAction.WasPressedThisFrame())
		{
			isRestarting = true;
			Instance = null;
			SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
		}
	}

	private void BindRestartAction()
	{
		if (playerInput == null)
		{
			playerInput = GetComponent<PlayerInput>();
		}

		if (playerInput == null)
		{
			return;
		}

		playerInput.defaultActionMap = "Player";
		restartAction = playerInput.actions != null
			? playerInput.actions.FindAction("Restart", throwIfNotFound: false)
			: null;

		restartAction?.Enable();
	}

	private void OnDestroy()
	{
		if (Instance == this)
		{
			Instance = null;
		}
	}

	public void RegisterEnemy()
	{
		if (TryGetPortal(out Portal resolvedPortal))
		{
			resolvedPortal.IncrementEnemiesLeft();
		}
	}

	public void UnregisterEnemy()
	{
		if (TryGetPortal(out Portal resolvedPortal))
		{
			resolvedPortal.DecrementEnemiesLeft();
		}
    }

	public bool TryGetPortal(out Portal resolvedPortal)
    {
		if (portal == null)
		{
			portal = FindAnyObjectByType<Portal>();
		}

		resolvedPortal = portal;
		if (resolvedPortal == null)
		{
			Debug.LogWarning("No Portal found in the scene. Please assign one to the LevelManager.", this);
			return false;
		}

		return true;
	}

	/// <summary>
	/// Whether the player may use this ability on this level. Fold / Snapshot / Gravity match AbilityManager indices 0–2.
	/// </summary>
	public bool IsAbilityAllowed(AbilityManager.AbilityType abilityType)
	{
		switch (abilityType)
		{
			case AbilityManager.AbilityType.Fold:
				return allowFoldAbility;
			case AbilityManager.AbilityType.Snapshot:
				return allowSnapshotAbility;
			case AbilityManager.AbilityType.Gravity:
				return allowGravityAbility;
			default:
				return true;
		}
	}

	public bool TryGetPlayer(out ObjectBehavior resolvedPlayer)
	{
		if (player == null)
		{
			ObjectBehavior[] allObjects = FindObjectsByType<ObjectBehavior>();
			for (int i = 0; i < allObjects.Length; i++)
			{
				if (allObjects[i].isPlayer)
				{
					player = allObjects[i];
					break;
				}
			}
		}

		resolvedPlayer = player;
		return resolvedPlayer != null;
	}

	private void ResolveSceneReferences()
	{
		thisLevelName = SceneManager.GetActiveScene().name;
		TryGetPortal(out _);
		TryGetPlayer(out _);
    }
}
