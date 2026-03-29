using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
	public static LevelManager Instance { get; private set; }

	[Header("Level Info")]
	[SerializeField] public string thisLevelName;
	[SerializeField] public string nextLevelName;
	[SerializeField] public Portal portal;
	[SerializeField] public ObjectBehavior player;

	private void Awake()
	{
		if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // enforce single instance
            return;
        }

        Instance = this;
		ResolveSceneReferences();
	}

	private void Start()
    {
		ResolveSceneReferences();

		if (portal != null)
		{
			portal.nextLevelScene = nextLevelName;
		}
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
